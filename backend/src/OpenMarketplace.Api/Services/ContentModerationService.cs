using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OpenMarketplace.Domain.Moderation;
using OpenMarketplace.Infrastructure.Persistence;

namespace OpenMarketplace.Api.Services;

public sealed record ModerationCheckResult(
    bool IsSafe,
    bool IsRejected,
    bool NeedsReview,
    string Status,
    string Reason,
    string Categories,
    decimal MaxScore,
    string RawResponse);

public interface IContentModerationService
{
    Task<ModerationCheckResult> CheckTextAsync(string title, string description, CancellationToken ct);
    Task<ModerationCheckResult> CheckImageAsync(byte[] imageBytes, string contentType, CancellationToken ct);
}

public sealed class ContentModerationService(AppDbContext db, IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<ContentModerationService> logger) : IContentModerationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ModerationCheckResult> CheckTextAsync(string title, string description, CancellationToken ct)
    {
        var text = string.Join("\n\n", new[] { title, description }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
        if (string.IsNullOrWhiteSpace(text)) return Safe("Empty content");

        var local = await CheckBlockedWordsAsync(text, ct);
        if (!local.IsSafe) return local;

        if (!await IsEnabledAsync("moderation.ai_enabled", true, ct)) return Safe("AI moderation disabled");
        return await CallOpenAiModerationAsync(new object[] { new { type = "text", text } }, "Text", ct);
    }

    public async Task<ModerationCheckResult> CheckImageAsync(byte[] imageBytes, string contentType, CancellationToken ct)
    {
        if (imageBytes.Length == 0) return Safe("Empty image");
        if (!await IsEnabledAsync("moderation.ai_enabled", true, ct)) return Safe("AI moderation disabled");

        var base64 = Convert.ToBase64String(imageBytes);
        var dataUrl = $"data:{contentType};base64,{base64}";
        return await CallOpenAiModerationAsync(new object[] { new { type = "image_url", image_url = new { url = dataUrl } } }, "Image", ct);
    }

    private async Task<ModerationCheckResult> CheckBlockedWordsAsync(string text, CancellationToken ct)
    {
        var normalized = Normalize(text);
        var words = await db.BlockedWords.AsNoTracking()
            .Where(x => x.IsActive && !x.IsDeleted)
            .OrderByDescending(x => x.Severity)
            .ToListAsync(ct);

        foreach (var item in words)
        {
            var needle = string.IsNullOrWhiteSpace(item.NormalizedWord) ? Normalize(item.Word) : item.NormalizedWord;
            if (string.IsNullOrWhiteSpace(needle)) continue;
            var matchType = (item.MatchType ?? "Contains").Trim();
            var matched = matchType.Equals("Exact", StringComparison.OrdinalIgnoreCase)
                ? Regex.IsMatch(normalized, $@"(^|\W){Regex.Escape(needle)}($|\W)", RegexOptions.IgnoreCase)
                : matchType.Equals("Regex", StringComparison.OrdinalIgnoreCase)
                    ? Regex.IsMatch(normalized, item.Word, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                    : normalized.Contains(needle, StringComparison.OrdinalIgnoreCase);

            if (!matched) continue;

            var high = item.Severity.Equals("High", StringComparison.OrdinalIgnoreCase);
            return new ModerationCheckResult(false, high, !high, high ? "Rejected" : "PendingReview", $"Blocked word matched: {item.Word}", item.Category, 1m, "LocalBlockedWords");
        }

        return Safe("No blocked words");
    }

    private async Task<ModerationCheckResult> CallOpenAiModerationAsync(object[] input, string targetType, CancellationToken ct)
    {
        var apiKey = configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey)) return Safe("OPENAI_API_KEY is not configured");

        var model = configuration["OpenAI:ModerationModel"] ?? Environment.GetEnvironmentVariable("OPENAI_MODERATION_MODEL") ?? "omni-moderation-latest";
        var client = httpClientFactory.CreateClient("openai-moderation");
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/moderations");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(new { model, input }, JsonOptions), Encoding.UTF8, "application/json");

        try
        {
            using var response = await client.SendAsync(request, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("OpenAI moderation failed for {TargetType}: {Status} {Body}", targetType, response.StatusCode, raw);
                return new ModerationCheckResult(true, false, false, "Safe", "OpenAI moderation unavailable; allowed by fail-open setting.", "", 0, raw);
            }
            var rejectThreshold = await GetDecimalAsync("moderation.reject_threshold", 0.85m, ct);
            var reviewThreshold = await GetDecimalAsync("moderation.review_threshold", 0.45m, ct);
            return ParseOpenAi(raw, reviewThreshold, rejectThreshold);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "OpenAI moderation request failed for {TargetType}", targetType);
            return new ModerationCheckResult(true, false, false, "Safe", "OpenAI moderation unavailable; allowed by fail-open setting.", "", 0, ex.Message);
        }
    }

    private ModerationCheckResult ParseOpenAi(string raw, decimal reviewThreshold, decimal rejectThreshold)
    {
        using var doc = JsonDocument.Parse(raw);
        var result = doc.RootElement.GetProperty("results")[0];
        var flagged = result.TryGetProperty("flagged", out var flaggedElement) && flaggedElement.GetBoolean();
        var categories = new List<string>();
        decimal maxScore = 0;

        if (result.TryGetProperty("category_scores", out var scores))
        {
            foreach (var score in scores.EnumerateObject())
            {
                var value = score.Value.ValueKind == JsonValueKind.Number ? score.Value.GetDecimal() : 0;
                if (value > maxScore) maxScore = value;
                if (value >= 0.5m) categories.Add(score.Name);
            }
        }
        if (result.TryGetProperty("categories", out var cats))
        {
            foreach (var cat in cats.EnumerateObject())
            {
                if (cat.Value.ValueKind == JsonValueKind.True && !categories.Contains(cat.Name)) categories.Add(cat.Name);
            }
        }

        var rejected = flagged && maxScore >= rejectThreshold;
        var review = flagged || maxScore >= reviewThreshold;
        var status = rejected ? "Rejected" : review ? "PendingReview" : "Safe";
        var reason = status == "Safe" ? "OpenAI moderation passed." : $"OpenAI moderation flagged content: {string.Join(", ", categories.DefaultIfEmpty("policy"))}.";
        return new ModerationCheckResult(status == "Safe", rejected, review && !rejected, status, reason, string.Join(",", categories), maxScore, raw);
    }

    private async Task<bool> IsEnabledAsync(string key, bool fallback, CancellationToken ct)
    {
        var value = await GetSettingValueAsync(key, ct);
        if (bool.TryParse(value, out var parsed)) return parsed;
        return fallback;
    }

    private async Task<decimal> GetDecimalAsync(string key, decimal fallback, CancellationToken ct)
    {
        var value = await GetSettingValueAsync(key, ct);
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    }

    private async Task<string?> GetSettingValueAsync(string key, CancellationToken ct)
    {
        var envKey = key.ToUpperInvariant().Replace('.', '_');
        var configured = configuration[key] ?? configuration[ToConfigurationKey(key)] ?? Environment.GetEnvironmentVariable(envKey);
        try
        {
            var dbValue = await db.AppSettings.AsNoTracking()
                .Where(x => x.Key == key && !x.IsDeleted)
                .Select(x => x.Value)
                .FirstOrDefaultAsync(ct);
            return string.IsNullOrWhiteSpace(dbValue) ? configured : dbValue;
        }
        catch
        {
            return configured;
        }
    }

    private static string ToConfigurationKey(string key) => key switch
    {
        "moderation.ai_enabled" => "Moderation:AiEnabled",
        "moderation.auto_approve_safe" => "Moderation:AutoApproveSafe",
        "moderation.review_threshold" => "Moderation:ReviewThreshold",
        "moderation.reject_threshold" => "Moderation:RejectThreshold",
        _ => key.Replace('.', ':')
    };

    private static ModerationCheckResult Safe(string reason) => new(true, false, false, "Safe", reason, "", 0, "");

    public static string Normalize(string value)
    {
        var lower = (value ?? string.Empty).ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(lower.Length);
        foreach (var ch in lower)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
        }
        return Regex.Replace(sb.ToString().Normalize(NormalizationForm.FormC), @"\s+", " ").Trim();
    }
}
