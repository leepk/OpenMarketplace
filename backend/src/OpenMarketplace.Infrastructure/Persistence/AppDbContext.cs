using Microsoft.EntityFrameworkCore;
using OpenMarketplace.Application.Common.Interfaces;
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

namespace OpenMarketplace.Infrastructure.Persistence;
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<AdPlacement> AdPlacements => Set<AdPlacement>();
    public DbSet<AdCampaign> AdCampaigns => Set<AdCampaign>();
    public DbSet<AdCreative> AdCreatives => Set<AdCreative>();
    public DbSet<AdStatistic> AdStatistics => Set<AdStatistic>();
    public DbSet<CmsPage> CmsPages => Set<CmsPage>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<ListingLike> ListingLikes => Set<ListingLike>();
    public DbSet<ListingComment> ListingComments => Set<ListingComment>();
    public DbSet<ListingReview> ListingReviews => Set<ListingReview>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<PaymentProvider> PaymentProviders => Set<PaymentProvider>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Category>(e=>{e.ToTable("categories");e.HasKey(x=>x.Id);e.HasIndex(x=>x.Code).IsUnique();e.HasIndex(x=>x.Slug).IsUnique();});
        builder.Entity<Package>(e=>{e.ToTable("packages");e.HasKey(x=>x.Id);e.HasIndex(x=>x.Code).IsUnique();});
        builder.Entity<Listing>(e=>{e.ToTable("listings");e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.Status,x.CategoryId,x.CreatedAt});e.HasIndex(x=>x.Slug).IsUnique(false);});
        builder.Entity<AdPlacement>(e=>{e.ToTable("ad_placements");e.HasKey(x=>x.Id);e.HasIndex(x=>x.Code).IsUnique();});
        builder.Entity<AdCampaign>(e=>{e.ToTable("ad_campaigns");e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.Status,x.StartDate,x.EndDate});});
        builder.Entity<AdCreative>(e=>{e.ToTable("ad_creatives");e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.Placement,x.Status,x.SortOrder});});
        builder.Entity<AdStatistic>(e=>{e.ToTable("ad_statistics");e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.CreativeId,x.Date}).IsUnique(false);});
        builder.Entity<CmsPage>(e=>{e.ToTable("cms_pages");e.HasKey(x=>x.Id);e.HasIndex(x=>x.Slug).IsUnique();});
        builder.Entity<AppSetting>(e=>{e.ToTable("app_settings");e.HasKey(x=>x.Id);e.HasIndex(x=>x.Key).IsUnique();});
        builder.Entity<UserProfile>(e=>{e.ToTable("user_profiles");e.HasKey(x=>x.Id);e.HasIndex(x=>x.Email).IsUnique();});
        builder.Entity<MediaAsset>(e=>{e.ToTable("media_assets");e.HasKey(x=>x.Id);e.HasIndex(x=>x.ListingId);});
        builder.Entity<Favorite>(e=>{e.ToTable("favorites");e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.UserId,x.ListingId}).IsUnique();});
        builder.Entity<ListingLike>(e=>{e.ToTable("listing_likes");e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.UserId,x.ListingId}).IsUnique();});
        builder.Entity<ListingComment>(e=>{e.ToTable("listing_comments");e.HasKey(x=>x.Id);e.HasIndex(x=>x.ListingId);});
        builder.Entity<ListingReview>(e=>{e.ToTable("listing_reviews");e.HasKey(x=>x.Id);e.HasIndex(x=>x.SellerId);});
        builder.Entity<Conversation>(e=>{e.ToTable("conversations");e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.ListingId,x.BuyerId,x.SellerId});e.HasIndex(x=>x.LastMessageAt);});
        builder.Entity<Message>(e=>{e.ToTable("messages");e.HasKey(x=>x.Id);e.HasIndex(x=>x.ConversationId);e.HasIndex(x=>new{x.ConversationId,x.CreatedAt});e.HasIndex(x=>x.ReceiverId);});
        builder.Entity<Notification>(e=>{e.ToTable("notifications");e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.UserId,x.IsRead,x.CreatedAt});});
        builder.Entity<Report>(e=>{e.ToTable("reports");e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.TargetType,x.TargetId,x.Status});});
        builder.Entity<AuditLog>(e=>{e.ToTable("audit_logs");e.HasKey(x=>x.Id);e.HasIndex(x=>x.CreatedAt);});
        builder.Entity<Order>(e=>{e.ToTable("orders");e.HasKey(x=>x.Id);e.HasIndex(x=>x.OrderNumber).IsUnique();});
        builder.Entity<Payment>(e=>{e.ToTable("payments");e.HasKey(x=>x.Id);e.HasIndex(x=>x.OrderId);});
        builder.Entity<Invoice>(e=>{e.ToTable("invoices");e.HasKey(x=>x.Id);e.HasIndex(x=>x.InvoiceNumber).IsUnique();});
        builder.Entity<Promotion>(e=>{e.ToTable("promotions");e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.ListingId,x.Status});});
        builder.Entity<PaymentProvider>(e=>{e.ToTable("payment_providers");e.HasKey(x=>x.Id);e.HasIndex(x=>x.Code).IsUnique();});
    }
}
