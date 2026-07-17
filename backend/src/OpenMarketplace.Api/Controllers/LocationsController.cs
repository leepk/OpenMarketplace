using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenMarketplace.Domain.Locations;
using OpenMarketplace.Infrastructure.Persistence;
using OpenMarketplace.Shared.Api;

namespace OpenMarketplace.Api.Controllers;

[ApiController]
[Route("api/v1/locations")]
public sealed class LocationsController(AppDbContext db) : ControllerBase
{
    [HttpGet("cities")]
    [AllowAnonymous]
    public async Task<IActionResult> Cities([FromQuery] string state = "CA", [FromQuery] string? q = null, [FromQuery] int limit = 500, CancellationToken ct = default)
    {
        var query = db.Localities.AsNoTracking().Where(x => !x.IsDeleted && x.IsActive && x.StateCode == state.ToUpper());
        if (!string.IsNullOrWhiteSpace(q)) { var term=q.Trim().ToLower(); query=query.Where(x=>x.Name.ToLower().Contains(term)); }
        var items = await query.OrderByDescending(x => x.SortOrder).ThenByDescending(x => x.SelectionCount).ThenBy(x => x.Name)
            .Take(Math.Clamp(limit,1,1000)).Select(x => new { x.Id, x.Name, x.StateCode, x.CountryCode, x.Latitude, x.Longitude, x.SortOrder, x.SelectionCount }).ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(items));
    }

    [HttpPost("cities/{id:guid}/selected")]
    [AllowAnonymous]
    public async Task<IActionResult> Selected(Guid id, CancellationToken ct)
    {
        var city=await db.Localities.FirstOrDefaultAsync(x=>x.Id==id && !x.IsDeleted && x.IsActive,ct);
        if(city is null) return NotFound();
        city.SelectionCount++; city.UpdatedAt=DateTimeOffset.UtcNow; await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { city.Id, city.SelectionCount }));
    }
}

[ApiController]
[Route("api/v1/admin/localities")]
[Authorize(Roles = "Admin") ]
public sealed class AdminLocalitiesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? q=null,[FromQuery] int page=1,[FromQuery] int pageSize=50,CancellationToken ct=default)
    {
        var query=db.Localities.AsNoTracking().Where(x=>!x.IsDeleted);
        if(!string.IsNullOrWhiteSpace(q)){var term=q.Trim().ToLower();query=query.Where(x=>x.Name.ToLower().Contains(term)||x.StateCode.ToLower().Contains(term));}
        var total=await query.CountAsync(ct);
        var items=await query.OrderByDescending(x=>x.SortOrder).ThenByDescending(x=>x.SelectionCount).ThenBy(x=>x.Name).Skip((Math.Max(page,1)-1)*Math.Clamp(pageSize,1,200)).Take(Math.Clamp(pageSize,1,200)).ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(new {items,totalItems=total,page,pageSize}));
    }
    [HttpPost]
    public async Task<IActionResult> Create(LocalityRequest r,CancellationToken ct){var x=new Locality{Name=r.Name.Trim(),StateCode=(r.StateCode??"CA").Trim().ToUpper(),CountryCode=(r.CountryCode??"US").Trim().ToUpper(),GeoId=r.GeoId?.Trim()??"",Latitude=r.Latitude,Longitude=r.Longitude,SortOrder=r.SortOrder??0,IsActive=r.IsActive??true};db.Localities.Add(x);await db.SaveChangesAsync(ct);return Ok(ApiResponse<object>.Ok(x));}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id,LocalityRequest r,CancellationToken ct){var x=await db.Localities.FirstOrDefaultAsync(v=>v.Id==id&&!v.IsDeleted,ct);if(x is null)return NotFound();x.Name=r.Name.Trim();x.StateCode=(r.StateCode??x.StateCode).Trim().ToUpper();x.CountryCode=(r.CountryCode??x.CountryCode).Trim().ToUpper();x.GeoId=r.GeoId?.Trim()??x.GeoId;x.Latitude=r.Latitude;x.Longitude=r.Longitude;x.SortOrder=r.SortOrder??x.SortOrder;x.IsActive=r.IsActive??x.IsActive;x.UpdatedAt=DateTimeOffset.UtcNow;await db.SaveChangesAsync(ct);return Ok(ApiResponse<object>.Ok(x));}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id,CancellationToken ct){var x=await db.Localities.FirstOrDefaultAsync(v=>v.Id==id&&!v.IsDeleted,ct);if(x is null)return NotFound();x.IsDeleted=true;x.DeletedAt=DateTimeOffset.UtcNow;await db.SaveChangesAsync(ct);return Ok(ApiResponse<object>.Ok(new{id}));}
}
public sealed record LocalityRequest(string Name,string? StateCode,string? CountryCode,string? GeoId,decimal? Latitude,decimal? Longitude,int? SortOrder,bool? IsActive);
