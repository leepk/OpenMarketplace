using Microsoft.EntityFrameworkCore;
using OpenMarketplace.Infrastructure.Persistence;

namespace OpenMarketplace.Api.Services;

public sealed record ExternalProviderExecution(
    string Code,
    string Name,
    int Priority,
    bool Enabled,
    bool Queried,
    int ItemCount,
    string? Message);

public sealed record ExternalMarketplaceSearchResult(
    bool Enabled,
    bool Queried,
    int LocalResultCount,
    int MinimumLocalResults,
    IReadOnlyList<EbayListingDto> Items,
    IReadOnlyList<ExternalProviderExecution> Providers,
    string? Message);

public interface IExternalMarketplaceService
{
    Task<ExternalMarketplaceSearchResult> SearchAsync(
        string? query,
        string? categoryId,
        string? postalCode,
        int limit,
        bool force,
        int localResultCount,
        CancellationToken ct);
}

public sealed class ExternalMarketplaceService(
    AppDbContext db,
    IEbayBrowseService ebay,
    ILogger<ExternalMarketplaceService> logger) : IExternalMarketplaceService
{
    private static readonly (string Code, string Name, int DefaultPriority)[] KnownProviders =
    [
        ("ebay", "eBay", 1),
        ("amazon", "Amazon", 2),
        ("walmart", "Walmart", 3),
        ("aliexpress", "AliExpress", 4)
    ];

    public async Task<ExternalMarketplaceSearchResult> SearchAsync(
        string? query,
        string? categoryId,
        string? postalCode,
        int limit,
        bool force,
        int localResultCount,
        CancellationToken ct)
    {
        var settings = await db.AppSettings.AsNoTracking()
            .Where(x => !x.IsDeleted && x.Key.StartsWith("external."))
            .ToDictionaryAsync(x => x.Key, x => x.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase, ct);

        var minimumLocal = ParseInt(settings.GetValueOrDefault("external.minimum_local_results"), 10, 0, 1000);
        var maximumResults = ParseInt(settings.GetValueOrDefault("external.maximum_results"), 100, 1, 100);
        limit = Math.Clamp(limit <= 0 ? maximumResults : Math.Min(limit, maximumResults), 1, 100);

        var providers = KnownProviders
            .Select(x => new ProviderConfig(
                x.Code,
                x.Name,
                ParseBool(settings.GetValueOrDefault($"external.{x.Code}.enabled")),
                ParseInt(settings.GetValueOrDefault($"external.{x.Code}.priority"), x.DefaultPriority, 1, 999)))
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!providers.Any(x => x.Enabled))
            return new(false, false, localResultCount, minimumLocal, [],
                providers.Select(x => ToExecution(x, false, 0, "Disabled")).ToList(),
                "All external marketplace providers are disabled.");

        if (!force && localResultCount >= minimumLocal)
            return new(true, false, localResultCount, minimumLocal, [],
                providers.Select(x => ToExecution(x, false, 0, x.Enabled ? "Not needed; enough Vunoca listings were found." : "Disabled")).ToList(),
                "Enough Vunoca listings were found, so external providers were not queried.");

        var items = new List<EbayListingDto>(limit);
        var executions = new List<ExternalProviderExecution>();

        foreach (var provider in providers)
        {
            if (!provider.Enabled)
            {
                executions.Add(ToExecution(provider, false, 0, "Disabled"));
                continue;
            }

            if (items.Count >= limit)
            {
                executions.Add(ToExecution(provider, false, 0, "Skipped because the external result limit was reached."));
                continue;
            }

            var remaining = limit - items.Count;
            switch (provider.Code)
            {
                case "ebay":
                {
                    var result = await ebay.SearchAsync(query, categoryId, postalCode, remaining, true, localResultCount, ct);
                    items.AddRange(result.Items.Take(remaining));
                    executions.Add(ToExecution(provider, result.Queried, result.Items.Count, result.Message));
                    break;
                }
                default:
                    // The provider is fully represented in settings and priority ordering now.
                    // Its live API adapter can be added later without changing Customer or ListingsController.
                    logger.LogInformation("External provider {Provider} is enabled but no live adapter is registered yet.", provider.Code);
                    executions.Add(ToExecution(provider, false, 0, "Provider adapter is not configured yet."));
                    break;
            }
        }

        return new(true, executions.Any(x => x.Queried), localResultCount, minimumLocal,
            items.Take(limit).ToList(), executions,
            items.Count == 0 ? "No external provider returned listings." : null);
    }

    private static ExternalProviderExecution ToExecution(ProviderConfig provider, bool queried, int itemCount, string? message) =>
        new(provider.Code, provider.Name, provider.Priority, provider.Enabled, queried, itemCount, message);

    private sealed record ProviderConfig(string Code, string Name, bool Enabled, int Priority);
    private static bool ParseBool(string? value) => (value ?? "").Trim().ToLowerInvariant() is "true" or "1" or "yes" or "on" or "enabled";
    private static int ParseInt(string? value, int fallback, int min, int max) => int.TryParse(value, out var parsed) ? Math.Clamp(parsed, min, max) : fallback;
}
