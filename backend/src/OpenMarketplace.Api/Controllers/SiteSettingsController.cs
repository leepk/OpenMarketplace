using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenMarketplace.Domain.Moderation;
using OpenMarketplace.Domain.Settings;
using OpenMarketplace.Infrastructure.Persistence;
using OpenMarketplace.Shared.Api;

namespace OpenMarketplace.Api.Controllers;

[ApiController]
public sealed class SiteSettingsController(AppDbContext db) : ControllerBase
{
    private static readonly Dictionary<string, string> DefaultTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["site.name"] = "String",
        ["site.logo_url"] = "ImageUrl",
        ["site.favicon_url"] = "ImageUrl",
        ["site.primary_color"] = "Color",
        ["site.secondary_color"] = "Color",
        ["social.facebook_url"] = "Url",
        ["social.youtube_url"] = "Url",
        ["social.instagram_url"] = "Url",
        ["contact.email"] = "Email",
        ["contact.phone"] = "String",
        ["contact.address"] = "String",
        ["footer.text"] = "String",
        ["seo.title"] = "String",
        ["seo.description"] = "Text",
        ["moderation.ai_enabled"] = "Boolean",
        ["moderation.auto_approve_safe"] = "Boolean",
        ["moderation.review_threshold"] = "String",
        ["moderation.reject_threshold"] = "String",
        ["auth.email_enabled"] = "Boolean",
        ["auth.google_enabled"] = "Boolean",
        ["auth.google_client_id"] = "String",
        ["auth.google_client_secret"] = "Secret",
        ["auth.facebook_enabled"] = "Boolean",
        ["auth.facebook_app_id"] = "String",
        ["auth.facebook_app_secret"] = "Secret",
        ["auth.auto_create_user"] = "Boolean",
        ["payment.default_provider"] = "String",
        ["payment.currency"] = "String",
        ["payment.stripe_enabled"] = "Boolean",
        ["payment.stripe_publishable_key"] = "String",
        ["payment.stripe_secret_key"] = "Secret",
        ["payment.stripe_webhook_secret"] = "Secret",
        ["payment.paypal_enabled"] = "Boolean",
        ["payment.paypal_client_id"] = "String",
        ["payment.paypal_secret"] = "Secret",
        ["payment.paypal_mode"] = "String",
        ["payment.manual_enabled"] = "Boolean",
        ["payment.manual_instructions"] = "Text",
        ["email.enabled"] = "Boolean",
        ["email.provider"] = "String",
        ["email.from_name"] = "String",
        ["email.from_address"] = "Email",
        ["email.smtp_host"] = "String",
        ["email.smtp_port"] = "String",
        ["email.smtp_username"] = "String",
        ["email.smtp_password"] = "Secret",
        ["email.smtp_use_ssl"] = "Boolean",
        ["sms.enabled"] = "Boolean",
        ["sms.provider"] = "String",
        ["sms.account_sid"] = "String",
        ["sms.auth_token"] = "Secret",
        ["sms.from_number"] = "String",
        ["template.email_welcome_subject"] = "String",
        ["template.email_welcome_body"] = "Text",
        ["template.email_verify_subject"] = "String",
        ["template.email_verify_body"] = "Text",
        ["template.email_password_reset_subject"] = "String",
        ["template.email_password_reset_body"] = "Text",
        ["template.email_payment_subject"] = "String",
        ["template.email_payment_body"] = "Text",
        ["template.sms_verification"] = "Text",
        ["template.sms_payment"] = "Text"
    };

    [HttpGet("api/v1/site-settings")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> Public(CancellationToken ct)
    {
        await EnsureDefaultsAsync(ct);
        var rows = await db.AppSettings.AsNoTracking()
            .Where(x => x.IsPublic && !x.IsDeleted && x.Key.StartsWith("site.") || x.IsPublic && !x.IsDeleted && x.Key.StartsWith("social.") || x.IsPublic && !x.IsDeleted && x.Key.StartsWith("contact.") || x.IsPublic && !x.IsDeleted && x.Key.StartsWith("footer.") || x.IsPublic && !x.IsDeleted && x.Key.StartsWith("seo."))
            .OrderBy(x => x.Key)
            .Select(x => new { x.Key, x.Value, x.ValueType })
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Ok(ToResponse(rows), HttpContext.TraceIdentifier));
    }

    [HttpGet("api/v1/admin/site-settings")]
    [Authorize(Roles = "Admin,SuperAdmin,System,Moderator,Support")]
    public async Task<ActionResult<ApiResponse<object>>> Admin(CancellationToken ct)
    {
        await EnsureDefaultsAsync(ct);
        var entities = await db.AppSettings.AsNoTracking()
            .Where(x => !x.IsDeleted && (x.Key.StartsWith("site.") || x.Key.StartsWith("social.") || x.Key.StartsWith("contact.") || x.Key.StartsWith("footer.") || x.Key.StartsWith("seo.") || x.Key.StartsWith("moderation.") || x.Key.StartsWith("auth.") || x.Key.StartsWith("payment.") || x.Key.StartsWith("email.") || x.Key.StartsWith("sms.") || x.Key.StartsWith("template.")))
            .OrderBy(x => x.Key)
            .ToListAsync(ct);

        // Never send a stored secret to the browser. The empty value lets the UI show a
        // password placeholder without accidentally posting a mask back as the new secret.
        var rows = entities.Select(x => new
        {
            x.Id,
            x.Key,
            Value = IsSecretType(x.ValueType) ? string.Empty : x.Value,
            x.ValueType,
            SecretConfigured = IsSecretType(x.ValueType) && !string.IsNullOrWhiteSpace(x.Value),
            x.IsPublic,
            x.CreatedAt,
            x.UpdatedAt
        }).ToList();

        return Ok(ApiResponse<object>.Ok(new { items = rows, settings = ToDictionary(rows), branding = ToBranding(rows) }, HttpContext.TraceIdentifier));
    }

    [HttpPost("api/v1/admin/site-settings")]
    [Authorize(Roles = "Admin,SuperAdmin,System,Moderator,Support")]
    public async Task<ActionResult<ApiResponse<object>>> Save(SiteSettingsSaveRequest request, CancellationToken ct)
    {
        var values = request.Settings ?? new Dictionary<string, string?>();
        foreach (var item in values)
        {
            var key = NormalizeKey(item.Key);
            if (string.IsNullOrWhiteSpace(key)) continue;
            if (!IsAllowedKey(key)) continue;

            var setting = await db.AppSettings.FirstOrDefaultAsync(x => x.Key == key, ct);
            if (setting is null)
            {
                setting = new AppSetting { Key = key, ValueType = DefaultTypes.GetValueOrDefault(key, "String"), IsPublic = !key.StartsWith("moderation.") };
                db.AppSettings.Add(setting);
            }
            var incoming = item.Value?.Trim() ?? string.Empty;
            if (IsSecretKey(key) && (string.IsNullOrWhiteSpace(incoming) || IsMaskedSecret(incoming))) continue;
            setting.Value = incoming;
            setting.ValueType = DefaultTypes.GetValueOrDefault(key, setting.ValueType);
            setting.IsPublic = IsPublicKey(key);
            setting.UpdatedAt = DateTimeOffset.UtcNow;
        }
        db.AuditLogs.Add(new AuditLog { ActorId = request.AdminId, Action = "Site settings updated", EntityType = "SiteSettings", EntityId = Guid.Empty });
        await db.SaveChangesAsync(ct);
        return await Admin(ct);
    }

    [HttpPost("api/v1/admin/site-settings/{key}/update")]
    [Authorize(Roles = "Admin,SuperAdmin,System,Moderator,Support")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateOne(string key, SiteSettingUpdateRequest request, CancellationToken ct)
    {
        key = NormalizeKey(key);
        if (!IsAllowedKey(key)) return BadRequest(ApiResponse<object>.Fail("Validation", "Unsupported site setting key", HttpContext.TraceIdentifier));
        var setting = await db.AppSettings.FirstOrDefaultAsync(x => x.Key == key, ct);
        if (setting is null)
        {
            setting = new AppSetting { Key = key, ValueType = DefaultTypes.GetValueOrDefault(key, "String"), IsPublic = !key.StartsWith("moderation.") };
            db.AppSettings.Add(setting);
        }
        var incoming = request.Value?.Trim() ?? string.Empty;
        if (IsSecretKey(key) && (string.IsNullOrWhiteSpace(incoming) || IsMaskedSecret(incoming)))
        {
            var preserved = new
            {
                setting.Id,
                setting.Key,
                Value = string.Empty,
                setting.ValueType,
                SecretConfigured = !string.IsNullOrWhiteSpace(setting.Value),
                setting.IsPublic,
                setting.CreatedAt,
                setting.UpdatedAt
            };
            return Ok(ApiResponse<object>.Ok(new { setting = preserved }, HttpContext.TraceIdentifier));
        }
        setting.Value = incoming;
        setting.ValueType = DefaultTypes.GetValueOrDefault(key, setting.ValueType);
        setting.IsPublic = IsPublicKey(key);
        setting.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { ActorId = request.AdminId, Action = $"Site setting updated: {key}", EntityType = "SiteSettings", EntityId = setting.Id });
        await db.SaveChangesAsync(ct);
        var responseSetting = new
        {
            setting.Id,
            setting.Key,
            Value = IsSecretKey(key) ? string.Empty : setting.Value,
            setting.ValueType,
            SecretConfigured = IsSecretKey(key) && !string.IsNullOrWhiteSpace(setting.Value),
            setting.IsPublic,
            setting.CreatedAt,
            setting.UpdatedAt
        };
        return Ok(ApiResponse<object>.Ok(new { setting = responseSetting }, HttpContext.TraceIdentifier));
    }

    private async Task EnsureDefaultsAsync(CancellationToken ct)
    {
        var defaults = GetDefaultSettings();
        var keys = defaults.Select(x => x.Key).ToList();
        var existing = await db.AppSettings.Where(x => keys.Contains(x.Key)).Select(x => x.Key).ToListAsync(ct);
        foreach (var row in defaults.Where(x => !existing.Contains(x.Key)))
        {
            db.AppSettings.Add(new AppSetting { Key = row.Key, Value = row.Value, ValueType = row.ValueType, IsPublic = IsPublicKey(row.Key) });
        }
        if (db.ChangeTracker.HasChanges()) await db.SaveChangesAsync(ct);
    }

    private static List<(string Key, string Value, string ValueType)> GetDefaultSettings() =>
    [
        ("site.name", "OpenMarketplace", "String"),
        ("site.logo_url", "/site/logo-openmarketplace.svg", "ImageUrl"),
        ("site.favicon_url", "/site/favicon-openmarketplace.svg", "ImageUrl"),
        ("site.primary_color", "#2563eb", "Color"),
        ("site.secondary_color", "#f59e0b", "Color"),
        ("social.facebook_url", "https://facebook.com/", "Url"),
        ("social.youtube_url", "", "Url"),
        ("social.instagram_url", "", "Url"),
        ("contact.email", "support@openmarketplace.local", "Email"),
        ("contact.phone", "", "String"),
        ("contact.address", "Santa Clara, CA", "String"),
        ("footer.text", "© OpenMarketplace. All rights reserved.", "String"),
        ("seo.title", "OpenMarketplace - Local Classifieds", "String"),
        ("seo.description", "Buy, sell and discover local listings near you.", "Text"),
        ("moderation.ai_enabled", "true", "Boolean"),
        ("moderation.auto_approve_safe", "true", "Boolean"),
        ("moderation.review_threshold", "0.45", "String"),
        ("moderation.reject_threshold", "0.85", "String"),
        ("auth.email_enabled", "true", "Boolean"),
        ("auth.google_enabled", "false", "Boolean"),
        ("auth.google_client_id", "", "String"),
        ("auth.google_client_secret", "", "Secret"),
        ("auth.facebook_enabled", "false", "Boolean"),
        ("auth.facebook_app_id", "", "String"),
        ("auth.facebook_app_secret", "", "Secret"),
        ("auth.auto_create_user", "true", "Boolean"),
        ("payment.default_provider", "MANUAL", "String"),
        ("payment.currency", "USD", "String"),
        ("payment.stripe_enabled", "false", "Boolean"),
        ("payment.stripe_publishable_key", "", "String"),
        ("payment.stripe_secret_key", "", "Secret"),
        ("payment.stripe_webhook_secret", "", "Secret"),
        ("payment.paypal_enabled", "false", "Boolean"),
        ("payment.paypal_client_id", "", "String"),
        ("payment.paypal_secret", "", "Secret"),
        ("payment.paypal_mode", "Sandbox", "String"),
        ("payment.manual_enabled", "true", "Boolean"),
        ("payment.manual_instructions", "Contact support to complete payment.", "Text"),
        ("email.enabled", "false", "Boolean"),
        ("email.provider", "SMTP", "String"),
        ("email.from_name", "Vunoca", "String"),
        ("email.from_address", "no-reply@vunoca.com", "Email"),
        ("email.smtp_host", "", "String"),
        ("email.smtp_port", "587", "String"),
        ("email.smtp_username", "", "String"),
        ("email.smtp_password", "", "Secret"),
        ("email.smtp_use_ssl", "true", "Boolean"),
        ("sms.enabled", "false", "Boolean"),
        ("sms.provider", "Twilio", "String"),
        ("sms.account_sid", "", "String"),
        ("sms.auth_token", "", "Secret"),
        ("sms.from_number", "", "String"),
        ("template.email_welcome_subject", "Welcome to {{siteName}}", "String"),
        ("template.email_welcome_body", "Hello {{userName}}, welcome to {{siteName}}.", "Text"),
        ("template.email_verify_subject", "Verify your {{siteName}} account", "String"),
        ("template.email_verify_body", "Hello {{userName}}, verify your email here: {{verificationUrl}}", "Text"),
        ("template.email_password_reset_subject", "Reset your {{siteName}} password", "String"),
        ("template.email_password_reset_body", "Hello {{userName}}, reset your password here: {{resetUrl}}. This link expires in {{expiresMinutes}} minutes.", "Text"),
        ("template.email_payment_subject", "Payment received - {{orderNumber}}", "String"),
        ("template.email_payment_body", "We received {{amount}} for order {{orderNumber}}.", "Text"),
        ("template.sms_verification", "{{siteName}} verification code: {{code}}", "Text"),
        ("template.sms_payment", "Payment {{amount}} received for {{orderNumber}}.", "Text")
    ];

    private static bool IsSecretType(string? valueType) => string.Equals(valueType, "Secret", StringComparison.OrdinalIgnoreCase);
    private static bool IsSecretKey(string key) => string.Equals(DefaultTypes.GetValueOrDefault(key), "Secret", StringComparison.OrdinalIgnoreCase);

    private static bool IsMaskedSecret(string value)
    {
        var compact = new string((value ?? string.Empty).Where(c => !char.IsWhiteSpace(c)).ToArray());
        if (compact.Length < 4) return false;
        return compact.All(c => c is '*' or '•' or '●' or '·' or 'x' or 'X');
    }

    private static string NormalizeKey(string key) => (key ?? string.Empty).Trim().ToLowerInvariant().Replace('-', '_');
    private static bool IsAllowedKey(string key) => DefaultTypes.ContainsKey(key);
    private static bool IsPublicKey(string key) => key.StartsWith("site.") || key.StartsWith("social.") || key.StartsWith("contact.") || key.StartsWith("footer.") || key.StartsWith("seo.") || key is "auth.email_enabled" or "auth.google_enabled" or "auth.google_client_id" or "auth.facebook_enabled" or "auth.facebook_app_id" or "auth.auto_create_user" or "payment.default_provider" or "payment.currency" or "payment.stripe_enabled" or "payment.stripe_publishable_key" or "payment.paypal_enabled" or "payment.paypal_client_id" or "payment.paypal_mode" or "payment.manual_enabled" or "payment.manual_instructions";
    private static Dictionary<string, string> ToDictionary(IEnumerable<dynamic> rows) => rows.ToDictionary(x => (string)x.Key, x => (string)(x.Value ?? string.Empty));
    private static object ToResponse(IEnumerable<dynamic> rows) => new { settings = ToDictionary(rows), branding = ToBranding(rows) };
    private static object ToBranding(IEnumerable<dynamic> rows)
    {
        var values = ToDictionary(rows);
        string V(string key) => values.TryGetValue(key, out var value) ? value : string.Empty;
        return new
        {
            siteName = V("site.name"),
            logoUrl = V("site.logo_url"),
            faviconUrl = V("site.favicon_url"),
            primaryColor = V("site.primary_color"),
            secondaryColor = V("site.secondary_color"),
            facebookUrl = V("social.facebook_url"),
            youtubeUrl = V("social.youtube_url"),
            instagramUrl = V("social.instagram_url"),
            contactEmail = V("contact.email"),
            contactPhone = V("contact.phone"),
            contactAddress = V("contact.address"),
            footerText = V("footer.text"),
            seoTitle = V("seo.title"),
            seoDescription = V("seo.description")
        };
    }
}

public sealed record SiteSettingsSaveRequest(Dictionary<string, string?>? Settings, Guid? AdminId);
public sealed record SiteSettingUpdateRequest(string? Value, Guid? AdminId);
