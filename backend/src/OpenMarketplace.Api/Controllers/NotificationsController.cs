using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenMarketplace.Infrastructure.Persistence;
using OpenMarketplace.Shared.Api;

namespace OpenMarketplace.Api.Controllers;
[ApiController]
[Route("api/v1/notifications")]
public sealed class NotificationsController(AppDbContext db):ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] Guid? userId, CancellationToken ct)
    {
        var uid = userId ?? DemoIds.Customer;
        var items = await db.Notifications.AsNoTracking().Where(x=>x.UserId==uid).OrderByDescending(x=>x.CreatedAt).Take(50).ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(new{items,unread=items.Count(x=>!x.IsRead)},HttpContext.TraceIdentifier));
    }
    [HttpPost("{id:guid}/read")]
    public async Task<ActionResult<ApiResponse<object>>> Read(Guid id, CancellationToken ct)
    { var n=await db.Notifications.FindAsync([id],ct); if(n is not null){n.IsRead=true;await db.SaveChangesAsync(ct);} return Ok(ApiResponse<object>.Ok(new{read=true},HttpContext.TraceIdentifier)); }
}
