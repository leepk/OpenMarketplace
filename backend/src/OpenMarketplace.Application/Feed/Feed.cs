using Microsoft.EntityFrameworkCore;
using OpenMarketplace.Application.Common.Interfaces;

namespace OpenMarketplace.Application.Feed;

public sealed record FeedItemDto(string Type, object Data);
public sealed record HomeFeedResponse(IReadOnlyList<object> Listings, IReadOnlyList<object> FeaturedListings, IReadOnlyList<object> RecentListings, IReadOnlyList<object> Categories, IReadOnlyList<FeedItemDto> Items, int Page, int PageSize, int TotalItems, int TotalPages);
public interface IFeedService { Task<HomeFeedResponse> HomeAsync(int page, int pageSize, CancellationToken ct); }

public sealed class FeedService(IAppDbContext db) : IFeedService
{
    public async Task<HomeFeedResponse> HomeAsync(int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Listings.AsNoTracking()
            .Where(x => x.Status == "Published" && !x.IsDeleted)
            .OrderByDescending(x => x.IsPinned)
            .ThenByDescending(x => x.IsFeatured)
            .ThenByDescending(x => x.CreatedAt);

        var total = await query.CountAsync(ct);
        var listings = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new
            {
                id = x.Id,
                title = x.Title,
                price = x.Price,
                currency = x.Currency,
                location = x.Location,
                description = x.Description,
                status = x.Status,
                isFeatured = x.IsFeatured,
                isUrgent = x.IsUrgent,
                isPinned = x.IsPinned,
                packageCode = x.PackageCode,
                packageStatus = x.PackageStatus,
                packageStartsAt = x.PackageStartsAt,
                packageEndsAt = x.PackageEndsAt,
                viewCount = x.ViewCount,
                favoriteCount = x.FavoriteCount,
                likeCount = x.LikeCount,
                commentCount = x.CommentCount,
                createdAt = x.CreatedAt,
                categoryId = x.CategoryId,
                categoryCode = db.Categories.Where(c => c.Id == x.CategoryId).Select(c => c.Code).FirstOrDefault(),
                categoryName = db.Categories.Where(c => c.Id == x.CategoryId).Select(c => c.Code).FirstOrDefault(),
                categoryIconKey = db.Categories.Where(c => c.Id == x.CategoryId).Select(c => c.IconKey).FirstOrDefault(),
                imageUrl = db.MediaAssets.Where(m => m.ListingId == x.Id).OrderBy(m => m.CreatedAt).Select(m => m.Url).FirstOrDefault()
            }).Cast<object>().ToListAsync(ct);

        var featured = await db.Listings.AsNoTracking()
            .Where(x => x.Status == "Published" && (x.IsFeatured || x.IsPinned) && !x.IsDeleted)
            .OrderByDescending(x => x.IsPinned).ThenByDescending(x => x.CreatedAt).Take(8)
            .Select(x => new
            {
                id = x.Id,
                title = x.Title,
                price = x.Price,
                currency = x.Currency,
                location = x.Location,
                description = x.Description,
                status = x.Status,
                isFeatured = x.IsFeatured,
                isUrgent = x.IsUrgent,
                isPinned = x.IsPinned,
                packageCode = x.PackageCode,
                packageStatus = x.PackageStatus,
                packageStartsAt = x.PackageStartsAt,
                packageEndsAt = x.PackageEndsAt,
                viewCount = x.ViewCount,
                favoriteCount = x.FavoriteCount,
                likeCount = x.LikeCount,
                commentCount = x.CommentCount,
                createdAt = x.CreatedAt,
                categoryId = x.CategoryId,
                categoryCode = db.Categories.Where(c => c.Id == x.CategoryId).Select(c => c.Code).FirstOrDefault(),
                categoryName = db.Categories.Where(c => c.Id == x.CategoryId).Select(c => c.Code).FirstOrDefault(),
                categoryIconKey = db.Categories.Where(c => c.Id == x.CategoryId).Select(c => c.IconKey).FirstOrDefault(),
                imageUrl = db.MediaAssets.Where(m => m.ListingId == x.Id).OrderBy(m => m.CreatedAt).Select(m => m.Url).FirstOrDefault()
            }).Cast<object>().ToListAsync(ct);

        var categories = await db.Categories.AsNoTracking()
            .Where(x => x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .Select(x => new
            {
                id = x.Id,
                code = x.Code,
                iconKey = x.IconKey,
                parentCode = x.ParentCode,
                name = x.Code,
                slug = x.Slug,
                description = x.Code,
                count = db.Listings.Count(l => l.CategoryId == x.Id && l.Status == "Published" && !l.IsDeleted)
            }).Cast<object>().ToListAsync(ct);

        var items = listings.Select(x => new FeedItemDto("listing", x)).ToList();

        return new HomeFeedResponse(listings, featured.Count > 0 ? featured : listings, listings, categories, items, page, pageSize, total, (int)Math.Ceiling(total / (double)pageSize));
    }
}
