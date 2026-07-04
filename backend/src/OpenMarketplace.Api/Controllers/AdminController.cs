using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenMarketplace.Domain.Moderation;
using OpenMarketplace.Infrastructure.Persistence;
using OpenMarketplace.Shared.Api;

namespace OpenMarketplace.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
public sealed class AdminController(AppDbContext db) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<object>>> Dashboard(CancellationToken ct)
    {
        var totalListings = await db.Listings.CountAsync(ct);
        var pendingListings = await db.Listings.CountAsync(x=>x.Status=="Pending" || x.ModerationStatus=="Pending",ct);
        var users = await db.UserProfiles.CountAsync(ct);
        var reports = await db.Reports.CountAsync(x=>x.Status=="Open",ct);
        var orders = await db.Orders.CountAsync(ct);
        var revenue = await db.Payments.Where(x=>x.Status=="Succeeded").SumAsync(x=>(decimal?)x.Amount,ct) ?? 0;
        var recentListings = await db.Listings.AsNoTracking().OrderByDescending(x=>x.CreatedAt).Take(10).ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(new{stats=new{totalListings,pendingListings,users,reports,orders,revenue},recentListings},HttpContext.TraceIdentifier));
    }

    [HttpGet("listings")]
    public async Task<ActionResult<ApiResponse<object>>> Listings([FromQuery]string? status, CancellationToken ct)
    {
        var q = db.Listings.AsNoTracking().AsQueryable();
        if(!string.IsNullOrWhiteSpace(status)) q=q.Where(x=>x.Status==status || x.ModerationStatus==status);
        var items = await q.OrderByDescending(x=>x.CreatedAt).Take(100).ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(new{items},HttpContext.TraceIdentifier));
    }

    [HttpPost("listings/{id:guid}/moderate")]
    public async Task<ActionResult<ApiResponse<object>>> Moderate(Guid id, ModerateRequest request, CancellationToken ct)
    {
        var listing = await db.Listings.FindAsync([id],ct);
        if(listing is null) return NotFound(ApiResponse<object>.Fail("NotFound","Listing not found",HttpContext.TraceIdentifier));
        listing.ModerationStatus = request.Decision;
        listing.ModerationReason = request.Reason ?? "";
        listing.Status = request.Decision == "Approved" ? "Published" : request.Decision == "Rejected" ? "Rejected" : "Pending";
        listing.PublishedAt = listing.Status=="Published" ? DateTimeOffset.UtcNow : listing.PublishedAt;
        db.AuditLogs.Add(new AuditLog{ActorId=request.AdminId,Action=$"Listing {request.Decision}",EntityType="Listing",EntityId=id,Details=request.Reason??""});
        db.Notifications.Add(new OpenMarketplace.Domain.Communication.Notification{UserId=listing.SellerId,Type="Moderation",Title=$"Listing {request.Decision}",Body=listing.ModerationReason,Url="/my-listings"});
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new{listing},HttpContext.TraceIdentifier));
    }

    [HttpGet("users")]
    public async Task<ActionResult<ApiResponse<object>>> Users(CancellationToken ct) => Ok(ApiResponse<object>.Ok(new{items=await db.UserProfiles.AsNoTracking().OrderByDescending(x=>x.CreatedAt).Take(100).ToListAsync(ct)},HttpContext.TraceIdentifier));

    [HttpPost("users/{id:guid}/badge")]
    public async Task<ActionResult<ApiResponse<object>>> Badge(Guid id, BadgeRequest request, CancellationToken ct)
    {
        var user = await db.UserProfiles.FindAsync([id],ct);
        if(user is null) return NotFound(ApiResponse<object>.Fail("NotFound","User not found",HttpContext.TraceIdentifier));
        user.EmailVerified=request.EmailVerified; user.PhoneVerified=request.PhoneVerified; user.IdVerified=request.IdVerified; user.BusinessVerified=request.BusinessVerified; user.TrustScore=request.TrustScore;
        db.AuditLogs.Add(new AuditLog{ActorId=request.AdminId,Action="Trust badges updated",EntityType="UserProfile",EntityId=id});
        await db.SaveChangesAsync(ct); return Ok(ApiResponse<object>.Ok(new{user},HttpContext.TraceIdentifier));
    }

    [HttpGet("reports")]
    public async Task<ActionResult<ApiResponse<object>>> Reports(CancellationToken ct) => Ok(ApiResponse<object>.Ok(new{items=await db.Reports.AsNoTracking().OrderByDescending(x=>x.CreatedAt).Take(100).ToListAsync(ct)},HttpContext.TraceIdentifier));

    [HttpPost("reports/{id:guid}/resolve")]
    public async Task<ActionResult<ApiResponse<object>>> ResolveReport(Guid id, ResolveReportRequest request, CancellationToken ct)
    { var report=await db.Reports.FindAsync([id],ct); if(report is not null){report.Status=request.Status; db.AuditLogs.Add(new AuditLog{ActorId=request.AdminId,Action="Report resolved",EntityType="Report",EntityId=id,Details=request.Status}); await db.SaveChangesAsync(ct);} return Ok(ApiResponse<object>.Ok(new{report},HttpContext.TraceIdentifier)); }

    [HttpGet("orders")]
    public async Task<ActionResult<ApiResponse<object>>> Orders(CancellationToken ct) => Ok(ApiResponse<object>.Ok(new{items=await db.Orders.AsNoTracking().OrderByDescending(x=>x.CreatedAt).Take(100).ToListAsync(ct)},HttpContext.TraceIdentifier));
    [HttpGet("payments")]
    public async Task<ActionResult<ApiResponse<object>>> Payments(CancellationToken ct) => Ok(ApiResponse<object>.Ok(new{items=await db.Payments.AsNoTracking().OrderByDescending(x=>x.CreatedAt).Take(100).ToListAsync(ct)},HttpContext.TraceIdentifier));
    [HttpGet("invoices")]
    public async Task<ActionResult<ApiResponse<object>>> Invoices(CancellationToken ct) => Ok(ApiResponse<object>.Ok(new{items=await db.Invoices.AsNoTracking().OrderByDescending(x=>x.CreatedAt).Take(100).ToListAsync(ct)},HttpContext.TraceIdentifier));
    [HttpGet("messages")]
    public async Task<ActionResult<ApiResponse<object>>> Messages(CancellationToken ct) => Ok(ApiResponse<object>.Ok(new{items=await db.Messages.AsNoTracking().OrderByDescending(x=>x.CreatedAt).Take(100).ToListAsync(ct)},HttpContext.TraceIdentifier));
    [HttpGet("comments")]
    public async Task<ActionResult<ApiResponse<object>>> Comments(CancellationToken ct) => Ok(ApiResponse<object>.Ok(new{items=await db.ListingComments.AsNoTracking().OrderByDescending(x=>x.CreatedAt).Take(100).ToListAsync(ct)},HttpContext.TraceIdentifier));
    [HttpGet("reviews")]
    public async Task<ActionResult<ApiResponse<object>>> Reviews(CancellationToken ct) => Ok(ApiResponse<object>.Ok(new{items=await db.ListingReviews.AsNoTracking().OrderByDescending(x=>x.CreatedAt).Take(100).ToListAsync(ct)},HttpContext.TraceIdentifier));
    [HttpGet("audit-logs")]
    public async Task<ActionResult<ApiResponse<object>>> Audit(CancellationToken ct) => Ok(ApiResponse<object>.Ok(new{items=await db.AuditLogs.AsNoTracking().OrderByDescending(x=>x.CreatedAt).Take(100).ToListAsync(ct)},HttpContext.TraceIdentifier));
    [HttpGet("health")]
    public ActionResult<ApiResponse<object>> Health() => Ok(ApiResponse<object>.Ok(new{api="ok",time=DateTimeOffset.UtcNow,swagger="/swagger/index.html"},HttpContext.TraceIdentifier));
}
public sealed record ModerateRequest(Guid? AdminId, string Decision, string? Reason);
public sealed record BadgeRequest(Guid? AdminId, bool EmailVerified, bool PhoneVerified, bool IdVerified, bool BusinessVerified, int TrustScore);
public sealed record ResolveReportRequest(Guid? AdminId, string Status);
