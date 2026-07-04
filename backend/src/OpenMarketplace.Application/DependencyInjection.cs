using Microsoft.Extensions.DependencyInjection;
using OpenMarketplace.Application.Categories;
using OpenMarketplace.Application.Commerce;
using OpenMarketplace.Application.Feed;
namespace OpenMarketplace.Application;
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICategoryReadService,CategoryReadService>();
        services.AddScoped<IPackageReadService,PackageReadService>();
        services.AddScoped<IFeedService,FeedService>();
        return services;
    }
}
