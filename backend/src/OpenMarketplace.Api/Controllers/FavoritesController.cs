using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenMarketplace.Infrastructure.Persistence;
using OpenMarketplace.Shared.Api;

namespace OpenMarketplace.Api.Controllers;

[ApiController]
[Route("api/v1/favorites")]
public sealed class FavoritesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] Guid userId, CancellationToken ct)
    {
        if (userId == Guid.Empty)
        {
            return Ok(ApiResponse<object>.Ok(new { items = Array.Empty<object>(), totalItems = 0 }, HttpContext.TraceIdentifier));
        }

        var query =
            from favorite in db.Favorites.AsNoTracking()
            join listing in db.Listings.AsNoTracking() on favorite.ListingId equals listing.Id
            join category in db.Categories.AsNoTracking() on listing.CategoryId equals category.Id into categoryJoin
            from category in categoryJoin.DefaultIfEmpty()
            where favorite.UserId == userId && !listing.IsDeleted
            orderby favorite.CreatedAt descending
            select new
            {
                listing.Id,
                listing.Title,
                listing.Price,
                listing.Currency,
                listing.Location,
                listing.Status,
                listing.ModerationStatus,
                listing.IsFeatured,
                listing.IsUrgent,
                listing.IsPinned,
                listing.Description,
                listing.CreatedAt,
                listing.ViewCount,
                listing.FavoriteCount,
                listing.LikeCount,
                listing.CommentCount,
                CategoryId = listing.CategoryId,
                CategoryCode = category != null ? category.Code : null,
                CategoryName = category != null ? category.Code : null,
                CategoryIconKey = category != null ? category.IconKey : null,
                ImageUrl = db.MediaAssets
                    .Where(media => media.ListingId == listing.Id)
                    .OrderBy(media => media.CreatedAt)
                    .Select(media => media.Url)
                    .FirstOrDefault()
            };

        var items = await query.Take(50).ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { items, totalItems = items.Count() }, HttpContext.TraceIdentifier));
    }
}
