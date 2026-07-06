using Microsoft.EntityFrameworkCore;
using OpenMarketplace.Application.Common.Interfaces;
namespace OpenMarketplace.Application.Commerce;
public sealed record PackageDto(Guid Id,string Code,string Name,decimal Price,string Currency,int DurationDays,int SortOrder);
public interface IPackageReadService { Task<IReadOnlyList<PackageDto>> GetAsync(CancellationToken ct); }
public sealed class PackageReadService(IAppDbContext db) : IPackageReadService
{
    public async Task<IReadOnlyList<PackageDto>> GetAsync(CancellationToken ct) =>
        await db.Packages.AsNoTracking().Where(x=>x.IsActive&&!x.IsDeleted).OrderBy(x=>x.SortOrder).ThenBy(x=>x.Price).ThenBy(x=>x.Name).Select(x=>new PackageDto(x.Id,x.Code,x.Name,x.Price,x.Currency,x.DurationDays,x.SortOrder)).ToListAsync(ct);
}
