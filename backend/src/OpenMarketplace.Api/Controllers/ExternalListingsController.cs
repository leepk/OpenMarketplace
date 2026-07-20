using Microsoft.AspNetCore.Mvc;
using OpenMarketplace.Api.Services;
using OpenMarketplace.Shared.Api;

namespace OpenMarketplace.Api.Controllers;

[ApiController]
[Route("api/v1/external-listings")]
public sealed class ExternalListingsController(IEbayBrowseService ebay) : ControllerBase
{
    [HttpGet("ebay/search")]
    public async Task<ActionResult<ApiResponse<EbaySearchResult>>> SearchEbay(
        [FromQuery] string? q = null,
        [FromQuery] string? categoryId = null,
        [FromQuery] string? postalCode = null,
        [FromQuery] int limit = 100,
        [FromQuery] bool force = false,
        CancellationToken ct = default)
    {
        Response.Headers.CacheControl = "no-store";
        var result = await ebay.SearchAsync(q, categoryId, postalCode, limit, force, null, ct);
        return Ok(ApiResponse<EbaySearchResult>.Ok(result, HttpContext.TraceIdentifier));
    }
}
