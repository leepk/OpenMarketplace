using Microsoft.EntityFrameworkCore;
using OpenMarketplace.Application.Common.Interfaces;
namespace OpenMarketplace.Application.Commerce;
public sealed record PackageDto(Guid Id,string Code,string Name,decimal Price,string Currency,int DurationDays);
public interface IPackageReadService { Task<IReadOnlyList<PackageDto>> GetAsync(CancellationToken ct); }
public sealed class PackageReadService(IAppDbContext db) : IPackageReadService
{
    public async Task<IReadOnlyList<PackageDto>> GetAsync(CancellationToken ct) =>
        await db.Packages.AsNoTracking().Where(x=>x.IsActive&&!x.IsDeleted).OrderBy(x=>x.Price).Select(x=>new PackageDto(x.Id,x.Code,x.Name,x.Price,x.Currency,x.DurationDays)).ToListAsync(ct);
}
