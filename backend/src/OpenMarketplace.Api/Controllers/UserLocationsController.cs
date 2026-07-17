using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenMarketplace.Domain.Locations;
using OpenMarketplace.Infrastructure.Persistence;
using OpenMarketplace.Shared.Api;

namespace OpenMarketplace.Api.Controllers;

[ApiController]
[Route("api/v1/user-locations")]
public sealed class UserLocationsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] Guid userId, [FromQuery] decimal? latitude, [FromQuery] decimal? longitude, CancellationToken ct)
    {
        if (userId == Guid.Empty) return BadRequest(ApiResponse<object>.Fail("Validation", "UserId is required", HttpContext.TraceIdentifier));
        var rows = await db.UserLocations.AsNoTracking().Where(x => x.UserId == userId && !x.IsDeleted).ToListAsync(ct);
        var items = rows.Select(x => new
        {
            x.Id, x.Label, x.AddressLine, x.City, x.State, x.PostalCode, x.Country, x.Latitude, x.Longitude, x.UseCount, x.LastUsedAt, x.IsDefault,
            distanceMiles = latitude.HasValue && longitude.HasValue && x.Latitude.HasValue && x.Longitude.HasValue
                ? DistanceMiles((double)latitude.Value, (double)longitude.Value, (double)x.Latitude.Value, (double)x.Longitude.Value)
                : (double?)null
        })
        .OrderBy(x => x.distanceMiles.HasValue ? 0 : 1)
        .ThenBy(x => x.distanceMiles)
        .ThenByDescending(x => x.IsDefault)
        .ThenByDescending(x => x.LastUsedAt)
        .ThenByDescending(x => x.UseCount)
        .ToList();
        return Ok(ApiResponse<object>.Ok(items, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserLocation>>> Save(UserLocationRequest request, CancellationToken ct)
    {
        if (request.UserId == Guid.Empty || string.IsNullOrWhiteSpace(request.City) || string.IsNullOrWhiteSpace(request.AddressLine))
            return BadRequest(ApiResponse<UserLocation>.Fail("Validation", "UserId, city, and address or pickup area are required", HttpContext.TraceIdentifier));
        var item = await UpsertAsync(request, ct);
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<UserLocation>.Ok(item, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/used")]
    public async Task<ActionResult<ApiResponse<object>>> MarkUsed(Guid id, [FromBody] UserLocationUsedRequest request, CancellationToken ct)
    {
        var item = await db.UserLocations.FirstOrDefaultAsync(x => x.Id == id && x.UserId == request.UserId && !x.IsDeleted, ct);
        if (item is null) return NotFound(ApiResponse<object>.Fail("NotFound", "Saved location not found", HttpContext.TraceIdentifier));
        item.UseCount++; item.LastUsedAt = DateTimeOffset.UtcNow; item.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { item.Id, item.UseCount, item.LastUsedAt }, HttpContext.TraceIdentifier));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, [FromQuery] Guid userId, CancellationToken ct)
    {
        var item = await db.UserLocations.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && !x.IsDeleted, ct);
        if (item is null) return NotFound(ApiResponse<object>.Fail("NotFound", "Saved location not found", HttpContext.TraceIdentifier));
        item.IsDeleted = true; item.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { id }, HttpContext.TraceIdentifier));
    }

    internal async Task<UserLocation> UpsertAsync(UserLocationRequest request, CancellationToken ct)
    {
        var city = request.City.Trim(); var address = request.AddressLine.Trim(); var state = (request.State ?? "").Trim();
        var normalizedCity = city.ToLower(); var normalizedAddress = address.ToLower();
        var item = await db.UserLocations.FirstOrDefaultAsync(x => x.UserId == request.UserId && !x.IsDeleted && x.City.ToLower() == normalizedCity && x.AddressLine.ToLower() == normalizedAddress, ct);
        if (item is null) { item = new UserLocation { UserId=request.UserId, CreatedBy=request.UserId }; db.UserLocations.Add(item); }
        item.Label = string.IsNullOrWhiteSpace(request.Label) ? $"{address}, {city}" : request.Label.Trim();
        item.AddressLine=address; item.City=city; item.State=state; item.PostalCode=(request.PostalCode??"").Trim(); item.Country=string.IsNullOrWhiteSpace(request.Country)?"US":request.Country.Trim();
        item.Latitude=request.Latitude; item.Longitude=request.Longitude; item.UseCount++; item.LastUsedAt=DateTimeOffset.UtcNow; item.UpdatedAt=DateTimeOffset.UtcNow; item.UpdatedBy=request.UserId;
        return item;
    }

    private static double DistanceMiles(double lat1,double lon1,double lat2,double lon2)
    {
        const double r=3958.7613; static double Rad(double v)=>v*Math.PI/180;
        var dLat=Rad(lat2-lat1); var dLon=Rad(lon2-lon1);
        var a=Math.Sin(dLat/2)*Math.Sin(dLat/2)+Math.Cos(Rad(lat1))*Math.Cos(Rad(lat2))*Math.Sin(dLon/2)*Math.Sin(dLon/2);
        return r*2*Math.Atan2(Math.Sqrt(a),Math.Sqrt(1-a));
    }
}

public sealed record UserLocationRequest(Guid UserId,string? Label,string AddressLine,string City,string? State,string? PostalCode,string? Country,decimal? Latitude,decimal? Longitude);
public sealed record UserLocationUsedRequest(Guid UserId);
