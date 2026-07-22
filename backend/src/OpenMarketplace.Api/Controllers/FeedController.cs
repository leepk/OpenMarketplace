using Microsoft.AspNetCore.Mvc;
using OpenMarketplace.Application.Feed;
using OpenMarketplace.Api.Services;
using OpenMarketplace.Shared.Api;

namespace OpenMarketplace.Api.Controllers;

[ApiController]
[Route("api/v1/feed")]
public sealed class FeedController(IFeedService service, IExternalMarketplaceService externalMarketplaces) : ControllerBase
{
    [HttpGet("home")]
    public async Task<ActionResult<ApiResponse<object>>> Home(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        [FromQuery] double? latitude = null,
        [FromQuery] double? longitude = null,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var feed = await service.HomeAsync(page, pageSize, latitude, longitude, ct);

        // The homepage has no user-entered query. Use a broad marketplace query so
        // external providers can supplement a sparse local feed. Provider settings,
        // priority, threshold and limits are still fully enforced by the backend.
        var external = await externalMarketplaces.SearchAsync(
            query: "popular deals",
            categoryId: null,
            postalCode: null,
            limit: 100,
            force: false,
            localResultCount: feed.TotalItems,
            ct: ct);

        return Ok(ApiResponse<object>.Ok(new
        {
            feed.Listings,
            feed.FeaturedListings,
            feed.RecentListings,
            feed.Categories,
            feed.Items,
            feed.Page,
            feed.PageSize,
            feed.TotalItems,
            feed.TotalPages,
            external
        }, HttpContext.TraceIdentifier));
    }
}
