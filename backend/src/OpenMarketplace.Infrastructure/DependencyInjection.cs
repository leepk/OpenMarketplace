using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenMarketplace.Application.Common.Interfaces;
using OpenMarketplace.Infrastructure.Persistence;
namespace OpenMarketplace.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services,IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("Default")).ConfigureWarnings(w=>w.Ignore(RelationalEventId.PendingModelChangesWarning)));
        services.AddScoped<IAppDbContext>(p=>p.GetRequiredService<AppDbContext>());
        services.AddScoped<DatabaseSeeder>();
        return services;
    }
}
