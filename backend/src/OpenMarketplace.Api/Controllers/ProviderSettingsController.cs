using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenMarketplace.Infrastructure.Persistence;
using OpenMarketplace.Shared.Api;

namespace OpenMarketplace.Api.Controllers;

[ApiController]
public sealed class ProviderSettingsController(AppDbContext db) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("api/v1/auth/providers")]
    public async Task<ActionResult<ApiResponse<object>>> AuthProviders(CancellationToken ct)
    {
        var keys = new[] { "auth.email_enabled", "auth.google_enabled", "auth.google_client_id", "auth.facebook_enabled", "auth.facebook_app_id", "auth.auto_create_user" };
        var values = await db.AppSettings.AsNoTracking().Where(x => keys.Contains(x.Key) && !x.IsDeleted).ToDictionaryAsync(x => x.Key, x => x.Value, ct);
        bool B(string key, bool fallback = false)
        {
            if (!values.TryGetValue(key, out var value)) return fallback;
            value = value?.Trim() ?? string.Empty;
            return bool.TryParse(value, out var parsed) ? parsed : value is "1" or "yes" or "on" or "enabled";
        }
        string V(string key) => values.TryGetValue(key, out var value) ? value?.Trim() ?? string.Empty : string.Empty;
        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        var googleConfigured = !string.IsNullOrWhiteSpace(V("auth.google_client_id"));
        var facebookConfigured = !string.IsNullOrWhiteSpace(V("auth.facebook_app_id"));
        return Ok(ApiResponse<object>.Ok(new
        {
            email = new { enabled = B("auth.email_enabled", true) },
            google = new { enabled = B("auth.google_enabled") && googleConfigured, configured = googleConfigured, clientId = V("auth.google_client_id") },
            facebook = new { enabled = B("auth.facebook_enabled") && facebookConfigured, configured = facebookConfigured, appId = V("auth.facebook_app_id") },
            autoCreateUser = B("auth.auto_create_user", true)
        }, HttpContext.TraceIdentifier));
    }

    [AllowAnonymous]
    [HttpGet("api/v1/payment/settings")]
    public async Task<ActionResult<ApiResponse<object>>> PaymentSettings(CancellationToken ct)
    {
        var paymentRows = await db.AppSettings.AsNoTracking()
            .Where(x => !x.IsDeleted && EF.Functions.ILike(x.Key, "payment.%"))
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .ToListAsync(ct);
        var rows = paymentRows
            .GroupBy(x => x.Key.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Value, StringComparer.OrdinalIgnoreCase);
        static bool ParseBool(string? value, bool fallback = false)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            return value.Trim().ToLowerInvariant() is "true" or "1" or "yes" or "on" or "enabled";
        }
        bool B(string key, bool fallback = false) => rows.TryGetValue(key, out var value) ? ParseBool(value, fallback) : fallback;
        string V(string key, string fallback = "") => rows.TryGetValue(key, out var value) ? value?.Trim() ?? fallback : fallback;
        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        var stripeConfigured = !string.IsNullOrWhiteSpace(V("payment.stripe_publishable_key"));
        var paypalConfigured = !string.IsNullOrWhiteSpace(V("payment.paypal_client_id"));
        return Ok(ApiResponse<object>.Ok(new
        {
            defaultProvider = V("payment.default_provider", "MANUAL"),
            currency = V("payment.currency", "USD"),
            stripe = new { enabled = B("payment.stripe_enabled") && stripeConfigured, configured = stripeConfigured, publishableKey = V("payment.stripe_publishable_key") },
            paypal = new { enabled = B("payment.paypal_enabled") && paypalConfigured, configured = paypalConfigured, clientId = V("payment.paypal_client_id"), mode = V("payment.paypal_mode", "Sandbox") },
            manual = new { enabled = B("payment.manual_enabled", true), instructions = V("payment.manual_instructions") }
        }, HttpContext.TraceIdentifier));
    }
}
