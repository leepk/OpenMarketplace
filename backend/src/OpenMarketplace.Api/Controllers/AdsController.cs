using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenMarketplace.Domain.Advertising;
using OpenMarketplace.Infrastructure.Persistence;
using OpenMarketplace.Shared.Api;

namespace OpenMarketplace.Api.Controllers;

[ApiController]
[Route("api/v1/ads")]
public sealed class AdsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] string placement, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(placement))
            return BadRequest(ApiResponse<object>.Fail("PlacementRequired", "placement is required.", HttpContext.TraceIdentifier));

        var normalizedPlacement = NormalizePlacement(placement);
        var now = DateTimeOffset.UtcNow;
        var items = await db.AdCreatives.AsNoTracking()
            .Join(db.AdCampaigns.AsNoTracking(), creative => creative.CampaignId, campaign => campaign.Id, (creative, campaign) => new { creative, campaign })
            .Where(x => !x.creative.IsDeleted
                && !x.campaign.IsDeleted
                && x.creative.Status == "Active"
                && x.campaign.Status == "Active"
                && x.creative.Placement == normalizedPlacement
                && x.campaign.StartDate <= now
                && x.campaign.EndDate >= now
                && (x.creative.MaxImpressions <= 0 || x.creative.CurrentImpressions < x.creative.MaxImpressions)
                && (x.creative.MaxClicks <= 0 || x.creative.CurrentClicks < x.creative.MaxClicks))
            .OrderByDescending(x => x.campaign.Priority)
            .ThenBy(x => x.creative.SortOrder)
            .ThenByDescending(x => x.creative.CreatedAt)
            .Select(x => new
            {
                x.creative.Id,
                CampaignId = x.campaign.Id,
                CampaignName = x.campaign.Name,
                x.creative.Placement,
                x.creative.Title,
                x.creative.Description,
                x.creative.DesktopImageUrl,
                x.creative.MobileImageUrl,
                x.creative.TargetUrl,
                x.creative.OpenInNewTab,
                x.creative.SortOrder
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Ok(new { placement = normalizedPlacement, items }, HttpContext.TraceIdentifier));
    }

    [HttpGet("{placement}")]
    public Task<ActionResult<ApiResponse<object>>> GetByRoute(string placement, CancellationToken ct) => Get(placement, ct);

    private static string NormalizePlacement(string placement)
    {
        var value = placement.Trim().Replace("-", "_").Replace(" ", "_");
        return value.ToUpperInvariant() switch
        {
            "HOMEHERO" or "HOME_HERO" or "HEROSEARCH" or "HERO_SEARCH" or "SEARCHHERO" or "SEARCH_HERO" or "HOMESEARCH" or "HOME_SEARCH" or "SEARCHFEED" or "SEARCH_FEED" => "HOME_HERO",
            "HOMEFEED" or "HOME_FEED" => "HOME_FEED",
            "LISTINGDETAIL" or "LISTING_DETAIL" or "DETAIL" => "LISTING_DETAIL",
            "SIDEBAR" => "SIDEBAR",
            "SELLERPROFILE" or "SELLER_PROFILE" => "SELLER_PROFILE",
            _ => value.ToUpperInvariant()
        };
    }

    [HttpPost("{creativeId:guid}/impression")]
    public async Task<ActionResult<ApiResponse<object>>> Impression(Guid creativeId, CancellationToken ct)
    {
        await TrackAsync(creativeId, isClick: false, ct);
        return Ok(ApiResponse<object>.Ok(new { ok = true }, HttpContext.TraceIdentifier));
    }

    [HttpPost("{creativeId:guid}/click")]
    public async Task<ActionResult<ApiResponse<object>>> Click(Guid creativeId, CancellationToken ct)
    {
        await TrackAsync(creativeId, isClick: true, ct);
        return Ok(ApiResponse<object>.Ok(new { ok = true }, HttpContext.TraceIdentifier));
    }

    private async Task TrackAsync(Guid creativeId, bool isClick, CancellationToken ct)
    {
        var creative = await db.AdCreatives.FirstOrDefaultAsync(x => x.Id == creativeId, ct);
        if (creative is null) return;

        if (isClick) creative.CurrentClicks += 1;
        else creative.CurrentImpressions += 1;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var stat = await db.AdStatistics.FirstOrDefaultAsync(x => x.CreativeId == creativeId && x.Date == today, ct);
        if (stat is null)
        {
            stat = new AdStatistic { CreativeId = creativeId, Date = today };
            db.AdStatistics.Add(stat);
        }
        if (isClick) stat.Clicks += 1;
        else stat.Impressions += 1;
        await db.SaveChangesAsync(ct);
    }
}
