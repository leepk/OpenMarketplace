using Microsoft.AspNetCore.Mvc;
using OpenMarketplace.Application.Feed;
using OpenMarketplace.Shared.Api;

namespace OpenMarketplace.Api.Controllers;

[ApiController]
[Route("api/v1/feed")]
public sealed class FeedController(IFeedService service) : ControllerBase
{
    [HttpGet("home")]
    public async Task<ActionResult<ApiResponse<HomeFeedResponse>>> Home(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] double? latitude = null,
        [FromQuery] double? longitude = null,
        CancellationToken ct = default)
        => Ok(ApiResponse<HomeFeedResponse>.Ok(
            await service.HomeAsync(page, pageSize, latitude, longitude, ct),
            HttpContext.TraceIdentifier));
}
