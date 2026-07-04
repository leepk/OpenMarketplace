using Microsoft.EntityFrameworkCore;
using OpenMarketplace.Domain.Advertising;
using OpenMarketplace.Domain.Categories;
using OpenMarketplace.Domain.Cms;
using OpenMarketplace.Domain.Commerce;
using OpenMarketplace.Domain.Communication;
using OpenMarketplace.Domain.Engagement;
using OpenMarketplace.Domain.Listings;
using OpenMarketplace.Domain.Media;
using OpenMarketplace.Domain.Moderation;
using OpenMarketplace.Domain.Settings;
using OpenMarketplace.Domain.Users;

namespace OpenMarketplace.Application.Common.Interfaces;
public interface IAppDbContext
{
    DbSet<Category> Categories { get; }
    DbSet<Package> Packages { get; }
    DbSet<Listing> Listings { get; }
    DbSet<AdPlacement> AdPlacements { get; }
    DbSet<CmsPage> CmsPages { get; }
    DbSet<AppSetting> AppSettings { get; }
    DbSet<UserProfile> UserProfiles { get; }
    DbSet<MediaAsset> MediaAssets { get; }
    DbSet<Favorite> Favorites { get; }
    DbSet<ListingLike> ListingLikes { get; }
    DbSet<ListingComment> ListingComments { get; }
    DbSet<ListingReview> ListingReviews { get; }
    DbSet<Conversation> Conversations { get; }
    DbSet<Message> Messages { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<Report> Reports { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Order> Orders { get; }
    DbSet<Payment> Payments { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<Promotion> Promotions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
