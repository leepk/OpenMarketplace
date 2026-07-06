using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenMarketplace.Infrastructure.Persistence;
using OpenMarketplace.Shared.Api;

namespace OpenMarketplace.Api.Controllers;

[ApiController]
[Route("api/v1/notifications")]
public sealed class NotificationsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get(
        [FromQuery] Guid? userId,
        [FromQuery] string? type,
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var uid = userId ?? DemoIds.Customer;
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Notifications.AsNoTracking().Where(x => !x.IsDeleted && x.UserId == uid);
        if (unreadOnly) query = query.Where(x => !x.IsRead);
        if (!string.IsNullOrWhiteSpace(type) && !string.Equals(type, "All", StringComparison.OrdinalIgnoreCase))
        {
            var normalizedType = type.Trim();
            query = query.Where(x => x.Type == normalizedType);
        }

        var unread = await db.Notifications.AsNoTracking().CountAsync(x => !x.IsDeleted && x.UserId == uid && !x.IsRead, ct);
        var totalItems = await query.CountAsync(ct);
        var items = await query
            .OrderBy(x => x.IsRead)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.Type,
                x.Title,
                x.Body,
                x.Url,
                x.EntityType,
                x.EntityId,
                x.ImageUrl,
                x.IsRead,
                x.ReadAt,
                x.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Ok(new { items, unread, totalItems, page, pageSize }, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/read")]
    public async Task<ActionResult<ApiResponse<object>>> Read(Guid id, [FromQuery] Guid? userId, CancellationToken ct)
    {
        var uid = userId ?? DemoIds.Customer;
        var notification = await db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid && !x.IsDeleted, ct);
        if (notification is not null && !notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTimeOffset.UtcNow;
            notification.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return Ok(ApiResponse<object>.Ok(new { read = notification is not null }, HttpContext.TraceIdentifier));
    }

    [HttpPost("read-all")]
    public async Task<ActionResult<ApiResponse<object>>> ReadAll([FromBody] NotificationReadAllRequest request, CancellationToken ct)
    {
        var uid = request.UserId == Guid.Empty ? DemoIds.Customer : request.UserId;
        var now = DateTimeOffset.UtcNow;
        var notifications = await db.Notifications
            .Where(x => !x.IsDeleted && x.UserId == uid && !x.IsRead)
            .ToListAsync(ct);

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
            notification.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { read = notifications.Count }, HttpContext.TraceIdentifier));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, [FromQuery] Guid? userId, CancellationToken ct)
    {
        var uid = userId ?? DemoIds.Customer;
        var notification = await db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid && !x.IsDeleted, ct);
        if (notification is not null)
        {
            notification.IsDeleted = true;
            notification.DeletedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return Ok(ApiResponse<object>.Ok(new { deleted = notification is not null }, HttpContext.TraceIdentifier));
    }
}

public sealed record NotificationReadAllRequest(Guid UserId);
