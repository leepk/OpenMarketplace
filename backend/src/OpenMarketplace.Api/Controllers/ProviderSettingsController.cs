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
        bool B(string key, bool fallback = false) => values.TryGetValue(key, out var value) ? bool.TryParse(value, out var result) && result : fallback;
        string V(string key) => values.TryGetValue(key, out var value) ? value : string.Empty;
        return Ok(ApiResponse<object>.Ok(new { email = new { enabled = B("auth.email_enabled", true) }, google = new { enabled = B("auth.google_enabled") && !string.IsNullOrWhiteSpace(V("auth.google_client_id")), clientId = V("auth.google_client_id") }, facebook = new { enabled = B("auth.facebook_enabled") && !string.IsNullOrWhiteSpace(V("auth.facebook_app_id")), appId = V("auth.facebook_app_id") }, autoCreateUser = B("auth.auto_create_user", true) }, HttpContext.TraceIdentifier));
    }

    [AllowAnonymous]
    [HttpGet("api/v1/payment/settings")]
    public async Task<ActionResult<ApiResponse<object>>> PaymentSettings(CancellationToken ct)
    {
        var rows = await db.AppSettings.AsNoTracking().Where(x => x.Key.StartsWith("payment.") && x.IsPublic && !x.IsDeleted).ToDictionaryAsync(x => x.Key, x => x.Value, ct);
        bool B(string key, bool fallback = false) => rows.TryGetValue(key, out var value) ? bool.TryParse(value, out var result) && result : fallback;
        string V(string key, string fallback = "") => rows.TryGetValue(key, out var value) ? value : fallback;
        return Ok(ApiResponse<object>.Ok(new { defaultProvider = V("payment.default_provider", "MANUAL"), currency = V("payment.currency", "USD"), stripe = new { enabled = B("payment.stripe_enabled"), publishableKey = V("payment.stripe_publishable_key") }, paypal = new { enabled = B("payment.paypal_enabled"), clientId = V("payment.paypal_client_id"), mode = V("payment.paypal_mode", "Sandbox") }, manual = new { enabled = B("payment.manual_enabled", true), instructions = V("payment.manual_instructions") } }, HttpContext.TraceIdentifier));
    }
}
