using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OpenMarketplace.Infrastructure.Persistence;

namespace OpenMarketplace.Api.Services;

public sealed record EbayListingDto(
    string ExternalId,
    string Title,
    decimal? Price,
    string Currency,
    string ImageUrl,
    string ItemUrl,
    string Condition,
    string Location,
    string Seller,
    DateTimeOffset? ItemEndDate,
    string Source,
    bool IsExternal);

public sealed record EbaySearchResult(
    bool Enabled,
    bool Queried,
    int LocalResultCount,
    int MinimumLocalResults,
    IReadOnlyList<EbayListingDto> Items,
    string? Message);

public interface IEbayBrowseService
{
    Task<EbaySearchResult> SearchAsync(
        string? query,
        string? categoryId,
        string? postalCode,
        int limit,
        bool force,
        int? localResultCount,
        CancellationToken ct);
}

public sealed class EbayBrowseService(
    AppDbContext db,
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    ILogger<EbayBrowseService> logger) : IEbayBrowseService
{
    
    public async Task<EbaySearchResult> SearchAsync(
        string? query,
        string? categoryId,
        string? postalCode,
        int limit,
        bool force,
        int? localResultCount,
        CancellationToken ct)
    {
        query = (query ?? string.Empty).Trim();
        categoryId = (categoryId ?? string.Empty).Trim();
        if (query.Length > 0 && query.Length < 2)
            return new(false, false, 0, 0, [], "Search query must contain at least 2 characters.");
        if (query.Length == 0 && categoryId.Length == 0)
            return new(false, false, 0, 0, [], "Provide either a search query or an eBay category ID.");

        var settings = await LoadSettingsAsync(ct);
        var enabled = ParseBool(settings.GetValueOrDefault("external.ebay.enabled"));
        var minLocal = ParseInt(settings.GetValueOrDefault("external.ebay.minimum_local_results"), 10, 0, 1000);
        var configuredLimit = ParseInt(settings.GetValueOrDefault("external.ebay.maximum_results"), 100, 1, 100);
        limit = Math.Clamp(limit <= 0 ? configuredLimit : Math.Min(limit, configuredLimit), 1, 100);

        var localCount = localResultCount ?? 0;
        if (!localResultCount.HasValue && query.Length > 0)
        {
            var now = DateTimeOffset.UtcNow;
            var localPattern = $"%{query}%";
            localCount = await db.Listings.AsNoTracking().CountAsync(x =>
                !x.IsDeleted && x.Status == "Published" && (!x.ExpiresAt.HasValue || x.ExpiresAt >= now) &&
                (EF.Functions.ILike(x.Title, localPattern) || EF.Functions.ILike(x.Description, localPattern)), ct);
        }

        if (!enabled)
            return new(false, false, localCount, minLocal, [], "eBay external listings are disabled.");

        if (!force && query.Length > 0 && localCount >= minLocal)
            return new(true, false, localCount, minLocal, [], "Enough Vunoca listings were found, so eBay was not queried.");

        var clientId = settings.GetValueOrDefault("external.ebay.client_id")?.Trim();
        var clientSecret = settings.GetValueOrDefault("external.ebay.client_secret")?.Trim();
        var campaignId = settings.GetValueOrDefault("external.ebay.campaign_id")?.Trim();
        var marketplaceId = settings.GetValueOrDefault("external.ebay.marketplace_id")?.Trim();
        if (string.IsNullOrWhiteSpace(marketplaceId)) marketplaceId = "EBAY_US";

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            return new(true, false, localCount, minLocal, [], "eBay Client ID or Client Secret is not configured.");

        var cacheMinutes = ParseInt(settings.GetValueOrDefault("external.ebay.cache_minutes"), 30, 1, 1440);
        var resultCacheKey = $"external:ebay:search:{marketplaceId}:{query.ToLowerInvariant()}:{categoryId}:{postalCode}:{limit}:{campaignId}";
        if (cache.TryGetValue(resultCacheKey, out IReadOnlyList<EbayListingDto>? cachedItems) && cachedItems is not null)
            return new(true, true, localCount, minLocal, cachedItems, null);

        try
        {
            var accessToken = await GetAccessTokenAsync(clientId, clientSecret, ct);
            var parameters = new List<string> { $"limit={limit}" };
            if (query.Length > 0) parameters.Add($"q={Uri.EscapeDataString(query)}");
            if (categoryId.Length > 0) parameters.Add($"category_ids={Uri.EscapeDataString(categoryId)}");
            var requestUrl = $"https://api.ebay.com/buy/browse/v1/item_summary/search?{string.Join("&", parameters)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add("X-EBAY-C-MARKETPLACE-ID", marketplaceId);
            var endUserContext = BuildEndUserContext(campaignId, postalCode);
            if (!string.IsNullOrWhiteSpace(endUserContext))
                request.Headers.TryAddWithoutValidation("X-EBAY-C-ENDUSERCTX", endUserContext);

            var client = httpClientFactory.CreateClient("ebay-browse");
            using var response = await client.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("eBay Browse search failed. Status={StatusCode}, Body={Body}", response.StatusCode, body);
                return new(true, true, localCount, minLocal, [], $"eBay Browse API failed ({(int)response.StatusCode}).");
            }

            var items = ParseItems(body);
            cache.Set(resultCacheKey, items, TimeSpan.FromMinutes(cacheMinutes));
            return new(true, true, localCount, minLocal, items, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "eBay search failed for query {Query}, category {CategoryId}", query, categoryId);
            return new(true, true, localCount, minLocal, [], "eBay search is temporarily unavailable.");
        }
    }

    private async Task<string> GetAccessTokenAsync(string clientId, string clientSecret, CancellationToken ct)
    {
        var tokenCacheKey = $"external:ebay:oauth-token:{clientId}";
        if (cache.TryGetValue(tokenCacheKey, out string? token) && !string.IsNullOrWhiteSpace(token)) return token;

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.ebay.com/identity/v1/oauth2/token");
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["scope"] = "https://api.ebay.com/oauth/api_scope"
        });

        var client = httpClientFactory.CreateClient("ebay-browse");
        using var response = await client.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("eBay OAuth failed. Status={StatusCode}, Body={Body}", response.StatusCode, body);
            throw new InvalidOperationException($"eBay OAuth failed ({(int)response.StatusCode}).");
        }

        using var doc = JsonDocument.Parse(body);
        token = doc.RootElement.GetProperty("access_token").GetString();
        var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var expires) ? expires.GetInt32() : 7200;
        if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException("eBay OAuth returned an empty access token.");
        cache.Set(tokenCacheKey, token, TimeSpan.FromSeconds(Math.Max(60, expiresIn - 120)));
        return token;
    }

    private async Task<Dictionary<string, string>> LoadSettingsAsync(CancellationToken ct)
    {
        var rows = await db.AppSettings.AsNoTracking()
            .Where(x => !x.IsDeleted && x.Key.StartsWith("external.ebay."))
            .Select(x => new { x.Key, x.Value })
            .ToListAsync(ct);
        return rows.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static string BuildEndUserContext(string? campaignId, string? postalCode)
    {
        var values = new List<string>();
        if (!string.IsNullOrWhiteSpace(campaignId))
            values.Add($"affiliateCampaignId={campaignId.Trim()}");
        if (!string.IsNullOrWhiteSpace(postalCode))
        {
            values.Add($"contextualLocation=country=US,zip={postalCode.Trim()}");
        }
        return string.Join(',', values);
    }

    private static IReadOnlyList<EbayListingDto> ParseItems(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("itemSummaries", out var summaries) || summaries.ValueKind != JsonValueKind.Array)
            return [];

        var result = new List<EbayListingDto>();
        foreach (var item in summaries.EnumerateArray())
        {
            var id = S(item, "itemId");
            var title = S(item, "title");
            var image = item.TryGetProperty("image", out var imageNode) ? S(imageNode, "imageUrl") : "";
            var url = S(item, "itemAffiliateWebUrl");
            if (string.IsNullOrWhiteSpace(url)) url = S(item, "itemWebUrl");
            var condition = S(item, "condition");
            decimal? price = null;
            var currency = "USD";
            if (item.TryGetProperty("price", out var priceNode))
            {
                currency = S(priceNode, "currency");
                if (decimal.TryParse(S(priceNode, "value"), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var parsed)) price = parsed;
            }
            var location = "";
            if (item.TryGetProperty("itemLocation", out var locationNode))
                location = string.Join(", ", new[] { S(locationNode, "city"), S(locationNode, "stateOrProvince"), S(locationNode, "country") }.Where(x => !string.IsNullOrWhiteSpace(x)));
            var seller = item.TryGetProperty("seller", out var sellerNode) ? S(sellerNode, "username") : "";
            DateTimeOffset? endDate = DateTimeOffset.TryParse(S(item, "itemEndDate"), out var parsedEnd) ? parsedEnd : null;
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(url)) continue;
            result.Add(new(id, title, price, currency, image, url, condition, location, seller, endDate, "eBay", true));
        }
        return result;
    }

    private static string S(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";

    private static bool ParseBool(string? value) => (value ?? "").Trim().ToLowerInvariant() is "true" or "1" or "yes" or "on" or "enabled";
    private static int ParseInt(string? value, int fallback, int min, int max) => int.TryParse(value, out var parsed) ? Math.Clamp(parsed, min, max) : fallback;
}
