using Microsoft.EntityFrameworkCore;
using OpenMarketplace.Application.Common.Interfaces;

namespace OpenMarketplace.Application.Categories;

public sealed record CategoryDto(Guid Id, string Code, string IconKey, string? ParentCode, int SortOrder, string Name, string Slug, int Count);
public interface ICategoryReadService { Task<IReadOnlyList<CategoryDto>> GetAsync(CancellationToken ct); }
public sealed class CategoryReadService(IAppDbContext db) : ICategoryReadService
{
    public async Task<IReadOnlyList<CategoryDto>> GetAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        return await db.Categories.AsNoTracking()
            .Where(x => x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .Select(x => new CategoryDto(
                x.Id,
                x.Code,
                x.IconKey,
                x.ParentCode,
                x.SortOrder,
                x.Name,
                x.Slug,
                db.Listings.Count(l => l.CategoryId == x.Id && l.Status == "Published" && !l.IsDeleted && (!l.ExpiresAt.HasValue || l.ExpiresAt >= now))))
            .ToListAsync(ct);
    }
}
