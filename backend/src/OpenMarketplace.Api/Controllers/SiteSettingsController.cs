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
        ["moderation.reject_threshold"] = "String"
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
        var rows = await db.AppSettings.AsNoTracking()
            .Where(x => !x.IsDeleted && (x.Key.StartsWith("site.") || x.Key.StartsWith("social.") || x.Key.StartsWith("contact.") || x.Key.StartsWith("footer.") || x.Key.StartsWith("seo.") || x.Key.StartsWith("moderation.")))
            .OrderBy(x => x.Key)
            .Select(x => new { x.Id, x.Key, x.Value, x.ValueType, x.IsPublic, x.CreatedAt, x.UpdatedAt })
            .ToListAsync(ct);
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
            setting.Value = item.Value?.Trim() ?? string.Empty;
            setting.ValueType = DefaultTypes.GetValueOrDefault(key, setting.ValueType);
            setting.IsPublic = !key.StartsWith("moderation.");
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
        setting.Value = request.Value?.Trim() ?? string.Empty;
        setting.ValueType = DefaultTypes.GetValueOrDefault(key, setting.ValueType);
        setting.IsPublic = !key.StartsWith("moderation.");
        setting.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { ActorId = request.AdminId, Action = $"Site setting updated: {key}", EntityType = "SiteSettings", EntityId = setting.Id });
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { setting }, HttpContext.TraceIdentifier));
    }

    private async Task EnsureDefaultsAsync(CancellationToken ct)
    {
        var defaults = GetDefaultSettings();
        var keys = defaults.Select(x => x.Key).ToList();
        var existing = await db.AppSettings.Where(x => keys.Contains(x.Key)).Select(x => x.Key).ToListAsync(ct);
        foreach (var row in defaults.Where(x => !existing.Contains(x.Key)))
        {
            db.AppSettings.Add(new AppSetting { Key = row.Key, Value = row.Value, ValueType = row.ValueType, IsPublic = !row.Key.StartsWith("moderation.") });
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
        ("moderation.reject_threshold", "0.85", "String")
    ];

    private static string NormalizeKey(string key) => (key ?? string.Empty).Trim().ToLowerInvariant().Replace('-', '_');
    private static bool IsAllowedKey(string key) => DefaultTypes.ContainsKey(key);
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
