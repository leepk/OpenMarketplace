using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenMarketplace.Domain.Engagement;
using OpenMarketplace.Domain.Users;
using OpenMarketplace.Infrastructure.Persistence;
using OpenMarketplace.Shared.Api;

namespace OpenMarketplace.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
public sealed class UsersController(AppDbContext db) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<object>>> Me([FromQuery] Guid? userId, CancellationToken ct)
    {
        if (!userId.HasValue || userId.Value == Guid.Empty)
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized", "Please login first.", HttpContext.TraceIdentifier));

        var user = await db.UserProfiles.FirstOrDefaultAsync(x => x.Id == userId.Value, ct);
        if (user is null)
            return NotFound(ApiResponse<object>.Fail("NotFound", "User not found.", HttpContext.TraceIdentifier));

        if (string.IsNullOrWhiteSpace(user.AvatarUrl))
        {
            user.AvatarUrl = DefaultAvatar(user.Email);
            await db.SaveChangesAsync(ct);
        }

        return Ok(ApiResponse<object>.Ok(ToSafeUser(user), HttpContext.TraceIdentifier));
    }

    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<object>>> Save([FromQuery] Guid? userId, ProfileRequest request, CancellationToken ct)
    {
        if (!userId.HasValue || userId.Value == Guid.Empty)
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized", "Please login first.", HttpContext.TraceIdentifier));

        var user = await db.UserProfiles.FirstOrDefaultAsync(x => x.Id == userId.Value, ct);
        if (user is null)
            return NotFound(ApiResponse<object>.Fail("NotFound", "User not found.", HttpContext.TraceIdentifier));

        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(ApiResponse<object>.Fail("Validation", "Full name is required.", HttpContext.TraceIdentifier));

        user.Name = name;
        var newPhone = request.Phone?.Trim() ?? string.Empty;
        if (!string.Equals(user.Phone, newPhone, StringComparison.Ordinal))
        {
            user.Phone = newPhone;
            user.PhoneVerified = false;
            user.PhoneVerificationCodeHash = string.Empty;
            user.PhoneVerificationExpiresAt = null;
            user.PhoneVerificationSentAt = null;
            user.PhoneVerificationAttempts = 0;
        }
        user.Location = request.Location?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(request.AvatarUrl)) user.AvatarUrl = request.AvatarUrl.Trim();
        if (string.IsNullOrWhiteSpace(user.AvatarUrl)) user.AvatarUrl = DefaultAvatar(user.Email);
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(ToSafeUser(user), HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}/seller-profile")]
    public async Task<ActionResult<ApiResponse<object>>> Seller(Guid id, CancellationToken ct)
    {
        var user = await db.UserProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (user is null) return NotFound(ApiResponse<object>.Fail("NotFound", "Seller not found", HttpContext.TraceIdentifier));
        var listings = await db.Listings.AsNoTracking().Where(x => x.SellerId == id && x.Status == "Published").Take(12).ToListAsync(ct);
        var reviews = await db.ListingReviews.AsNoTracking().Where(x => x.SellerId == id && x.Status == "Published").OrderByDescending(x => x.CreatedAt).Take(10).ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { user = ToSafeUser(user), listings, reviews, badges = new { user.EmailVerified, user.PhoneVerified, user.IdVerified, user.BusinessVerified } }, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/reviews")]
    public async Task<ActionResult<ApiResponse<ListingReview>>> Review(Guid id, ReviewRequest request, CancellationToken ct)
    {
        var review = new ListingReview { SellerId = id, ReviewerId = request.ReviewerId == Guid.Empty ? DemoIds.Customer : request.ReviewerId, ListingId = request.ListingId, Rating = Math.Clamp(request.Rating, 1, 5), Body = request.Body };
        db.ListingReviews.Add(review);
        var seller = await db.UserProfiles.FindAsync([id], ct);
        if (seller is not null)
        {
            seller.ReviewCount++;
            seller.Rating = seller.ReviewCount == 1 ? review.Rating : Math.Round(((seller.Rating * (seller.ReviewCount - 1)) + review.Rating) / seller.ReviewCount, 2);
        }
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<ListingReview>.Ok(review, HttpContext.TraceIdentifier));
    }

    private static object ToSafeUser(UserProfile user) => new
    {
        user.Id,
        user.Name,
        user.Email,
        user.Phone,
        user.Location,
        user.AvatarUrl,
        user.Role,
        user.EmailVerified,
        user.PhoneVerified,
        user.IdVerified,
        user.BusinessVerified,
        user.Rating,
        user.ReviewCount,
        user.TrustScore,
        user.Status
    };

    private static string DefaultAvatar(string email)
    {
        var seed = Math.Abs(email.GetHashCode()) % 12 + 1;
        return $"/avatars/avatar-{seed}.svg";
    }
}

public sealed record ProfileRequest(string Name, string? Phone, string? Location, string? AvatarUrl);
public sealed record ReviewRequest(Guid ReviewerId, Guid? ListingId, int Rating, string Body);
