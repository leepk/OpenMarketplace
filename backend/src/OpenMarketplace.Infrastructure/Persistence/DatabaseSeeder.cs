using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using OpenMarketplace.Domain.Advertising;
using OpenMarketplace.Domain.Categories;
using OpenMarketplace.Domain.Cms;
using OpenMarketplace.Domain.Commerce;
using OpenMarketplace.Domain.Communication;
using OpenMarketplace.Domain.Listings;
using OpenMarketplace.Domain.Media;
using OpenMarketplace.Domain.Settings;
using OpenMarketplace.Domain.Users;

namespace OpenMarketplace.Infrastructure.Persistence;

public sealed class DatabaseSeeder(AppDbContext db, ILogger<DatabaseSeeder> logger)
{
    private static readonly Guid CustomerId = Guid.Parse("01990000-0000-7000-8000-000000000001");
    private static readonly Guid SellerJohnId = Guid.Parse("01990000-0000-7000-8000-000000000002");
    private static readonly Guid SellerSarahId = Guid.Parse("01990000-0000-7000-8000-000000000003");
    private static readonly Guid AdminId = Guid.Parse("01990000-0000-7000-8000-000000000010");

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedCategoriesAsync(ct);
        await SeedPackagesAsync(ct);
        await SeedPaymentProvidersAsync(ct);
        await SeedAdvertisementsAsync(ct);
        await SeedUsersAsync(ct);
        await SeedListingsAsync(ct);
        await SeedCommerceAsync(ct);
        await SeedMessagingAsync(ct);
        await SeedSystemDataAsync(ct);

        logger.LogInformation("Seed completed with sample marketplace data");
    }

    private async Task SeedCategoriesAsync(CancellationToken ct)
    {
        var samples = new[]
        {
            new { Code = "vehicles", IconKey = "vehicle", ParentCode = (string?)null, Sort = 10 },
            new { Code = "property_rentals", IconKey = "rental", ParentCode = (string?)null, Sort = 20 },
            new { Code = "for_sale", IconKey = "sale", ParentCode = (string?)null, Sort = 30 },
            new { Code = "jobs", IconKey = "jobs", ParentCode = (string?)null, Sort = 40 },
            new { Code = "services", IconKey = "services", ParentCode = (string?)null, Sort = 50 },
            new { Code = "electronics", IconKey = "electronics", ParentCode = (string?)null, Sort = 60 },
            new { Code = "home_garden", IconKey = "garden", ParentCode = (string?)null, Sort = 70 },
            new { Code = "community", IconKey = "community", ParentCode = (string?)null, Sort = 80 },
            new { Code = "pets", IconKey = "pets", ParentCode = (string?)null, Sort = 90 },
            new { Code = "sports_outdoors", IconKey = "sports", ParentCode = (string?)null, Sort = 100 }
        };

        foreach (var item in samples)
        {
            var legacySlug = item.Code.Replace('_', '-');
            var category = await db.Categories.FirstOrDefaultAsync(x => x.Code == item.Code || x.Slug == legacySlug, ct);
            if (category is null)
            {
                category = new Category { Code = item.Code };
                db.Categories.Add(category);
            }

            category.Code = item.Code;
            category.IconKey = item.IconKey;
            category.ParentCode = item.ParentCode;
            category.Slug = legacySlug;
            category.Name = item.Code; // display is localized in FE by code
            category.SortOrder = item.Sort;
            category.IsActive = true;
            category.IsDeleted = false;
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task SeedPackagesAsync(CancellationToken ct)
    {
        var packages = new[]
        {
            new { Code = "FREE", Name = "FREE", Price = 0m, Days = 30 },
            new { Code = "BASIC", Name = "BASIC", Price = 4.99m, Days = 30 },
            new { Code = "FEATURED", Name = "FEATURED", Price = 9.99m, Days = 7 },
            new { Code = "URGENT", Name = "URGENT", Price = 4.99m, Days = 7 },
            new { Code = "PREMIUM", Name = "PREMIUM", Price = 19.99m, Days = 30 },
            new { Code = "CREDITS100", Name = "CREDITS100", Price = 50m, Days = 365 }
        };
        foreach (var item in packages)
        {
            var package = await db.Packages.FirstOrDefaultAsync(x => x.Code == item.Code, ct);
            if (package is null) db.Packages.Add(new Package { Code = item.Code, Name = item.Name, Price = item.Price, DurationDays = item.Days, IsActive = true });
            else { package.Name = item.Name; package.Price = item.Price; package.DurationDays = item.Days; package.IsActive = true; }
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task SeedPaymentProvidersAsync(CancellationToken ct)
    {
        var providers = new[]
        {
            new
            {
                Code = "TEST",
                Name = "TEST",
                Type = "TEST",
                DisplayName = "TEST",
                IsEnabled = true,
                IsTestMode = true,
                Currency = "USD",
                SortOrder = 10,
                ConfigJson = "{\"mode\":\"test\",\"allowSuccess\":true,\"allowFailure\":true}",
                PublicConfigJson = "{\"label\":\"Developer test payment\",\"supportsInstantApproval\":true}"
            },
            new
            {
                Code = "STRIPE",
                Name = "STRIPE",
                Type = "STRIPE",
                DisplayName = "STRIPE",
                IsEnabled = true,
                IsTestMode = true,
                Currency = "USD",
                SortOrder = 20,
                ConfigJson = "{\"publishableKey\":\"pk_test_replace_me\",\"secretKey\":\"sk_test_replace_me\",\"webhookSecret\":\"whsec_replace_me\"}",
                PublicConfigJson = "{\"label\":\"Credit or debit card\",\"supportsCards\":true,\"supportsApplePay\":true,\"supportsGooglePay\":true}"
            },
            new
            {
                Code = "PAYPAL",
                Name = "PAYPAL",
                Type = "PAYPAL",
                DisplayName = "PAYPAL",
                IsEnabled = true,
                IsTestMode = true,
                Currency = "USD",
                SortOrder = 30,
                ConfigJson = "{\"clientId\":\"paypal_sandbox_client_id_replace_me\",\"clientSecret\":\"paypal_sandbox_secret_replace_me\"}",
                PublicConfigJson = "{\"label\":\"Pay with PayPal\",\"supportsRedirect\":true}"
            }
        };

        foreach (var item in providers)
        {
            var provider = await db.PaymentProviders.FirstOrDefaultAsync(x => x.Code == item.Code, ct);
            if (provider is null)
            {
                provider = new PaymentProvider { Code = item.Code };
                db.PaymentProviders.Add(provider);
            }

            provider.Name = item.Name;
            provider.Type = item.Type;
            provider.DisplayName = item.DisplayName;
            provider.IsEnabled = item.IsEnabled;
            provider.IsTestMode = item.IsTestMode;
            provider.Currency = item.Currency;
            provider.SortOrder = item.SortOrder;
            provider.ConfigJson = item.ConfigJson;
            provider.PublicConfigJson = item.PublicConfigJson;
        }

        await db.SaveChangesAsync(ct);
    }


    private async Task SeedAdvertisementsAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var campaigns = new[]
        {
            new { Name = "Summer Marketplace Deals 2026", Start = now.AddDays(-14), End = now.AddDays(90), Priority = 120, Budget = (decimal?)2500m },
            new { Name = "South Bay Local Services", Start = now.AddDays(-7), End = now.AddDays(75), Priority = 105, Budget = (decimal?)1800m },
            new { Name = "Real Estate Weekend", Start = now.AddDays(-3), End = now.AddDays(45), Priority = 95, Budget = (decimal?)1500m },
            new { Name = "Auto Buyer Week", Start = now.AddDays(-5), End = now.AddDays(50), Priority = 90, Budget = (decimal?)1200m },
            new { Name = "OpenMarketplace Seller Growth", Start = now.AddDays(-30), End = now.AddDays(180), Priority = 130, Budget = (decimal?)3000m },
            new { Name = "Safety Trust Campaign", Start = now.AddDays(-10), End = now.AddDays(120), Priority = 80, Budget = (decimal?)800m }
        };

        var campaignIds = new Dictionary<string, Guid>();
        foreach (var item in campaigns)
        {
            var campaign = await db.AdCampaigns.FirstOrDefaultAsync(x => x.Name == item.Name, ct);
            if (campaign is null)
            {
                campaign = new AdCampaign { Name = item.Name };
                db.AdCampaigns.Add(campaign);
            }

            campaign.Status = "Active";
            campaign.StartDate = item.Start;
            campaign.EndDate = item.End;
            campaign.Priority = item.Priority;
            campaign.Budget = item.Budget;
            campaign.IsDeleted = false;
            campaignIds[item.Name] = campaign.Id;
        }
        await db.SaveChangesAsync(ct);

        var creatives = new[]
        {
            // Home hero slider
            new AdSeed("Summer Marketplace Deals 2026", "HOME_HERO", "Prime local deals", "Fresh offers from trusted sellers around you.", "/media/ads/home-hero-prime.svg", "/media/ads/home-hero-prime-mobile.svg", "/search?promoted=true", 10),
            new AdSeed("Auto Buyer Week", "HOME_HERO", "Drive your next deal", "Compare cars, bikes, and parts from verified local sellers.", "/media/ads/home-hero-auto.svg", "/media/ads/home-hero-auto-mobile.svg", "/search?category=vehicles", 20),
            new AdSeed("Real Estate Weekend", "HOME_HERO", "Find your next place", "Rooms, apartments, and rentals updated daily.", "/media/ads/home-hero-rentals.svg", "/media/ads/home-hero-rentals-mobile.svg", "/search?category=property_rentals", 30),
            new AdSeed("OpenMarketplace Seller Growth", "HOME_HERO", "Sell faster with featured placement", "Reach more buyers with premium listing tools.", "/media/ads/home-hero-seller.svg", "/media/ads/home-hero-seller-mobile.svg", "/billing", 40),
            new AdSeed("South Bay Local Services", "HOME_HERO", "Trusted local services", "Book movers, repair, cleaning, and more near you.", "/media/ads/home-hero-services.svg", "/media/ads/home-hero-services-mobile.svg", "/search?category=services", 50),

            // Featured area directly under featured listings

            // Home feed inline ads
            new AdSeed("South Bay Local Services", "HOME_FEED", "Moving help this weekend", "Book local movers and delivery helpers.", "/media/ads/feed-moving.svg", "/media/ads/feed-moving-mobile.svg", "/search?category=services", 10),
            new AdSeed("Auto Buyer Week", "HOME_FEED", "Auto finance offers", "Find financing options before you buy.", "/media/ads/feed-auto-finance.svg", "/media/ads/feed-auto-finance-mobile.svg", "/search?category=vehicles", 20),
            new AdSeed("Real Estate Weekend", "HOME_FEED", "Room rentals refreshed", "New South Bay rentals added today.", "/media/ads/feed-rentals.svg", "/media/ads/feed-rentals-mobile.svg", "/search?category=property_rentals", 30),
            new AdSeed("Summer Marketplace Deals 2026", "HOME_FEED", "Electronics sale", "Phones, laptops, tablets, and accessories.", "/media/ads/feed-electronics.svg", "/media/ads/feed-electronics-mobile.svg", "/search?category=electronics", 40),
            new AdSeed("OpenMarketplace Seller Growth", "HOME_FEED", "Boost your listing", "Get more saves, messages, and views.", "/media/ads/feed-boost.svg", "/media/ads/feed-boost-mobile.svg", "/billing", 50),
            new AdSeed("South Bay Local Services", "HOME_FEED", "Home cleaning pros", "Find trusted cleaners and home helpers.", "/media/ads/feed-cleaning.svg", "/media/ads/feed-cleaning-mobile.svg", "/search?category=services", 60),
            new AdSeed("Safety Trust Campaign", "HOME_FEED", "Trade safely", "Meet in public places and verify seller profiles.", "/media/ads/feed-safety.svg", "/media/ads/feed-safety-mobile.svg", "/safety", 70),
            new AdSeed("Summer Marketplace Deals 2026", "HOME_FEED", "Local fashion finds", "Shoes, bags, clothes, and accessories nearby.", "/media/ads/feed-fashion.svg", "/media/ads/feed-fashion-mobile.svg", "/search?category=for_sale", 80),


            // Listing detail slider
            new AdSeed("Auto Buyer Week", "LISTING_DETAIL", "Protect your purchase", "Inspection and warranty options for auto listings.", "/media/ads/detail-warranty.svg", "/media/ads/detail-warranty-mobile.svg", "/search?category=services", 10),
            new AdSeed("South Bay Local Services", "LISTING_DETAIL", "Need delivery?", "Find local pickup and delivery help.", "/media/ads/detail-delivery.svg", "/media/ads/detail-delivery-mobile.svg", "/search?category=services", 20),
            new AdSeed("Safety Trust Campaign", "LISTING_DETAIL", "Safety reminder", "Never send payment before confirming the item.", "/media/ads/detail-safety.svg", "/media/ads/detail-safety-mobile.svg", "/safety", 30),
            new AdSeed("OpenMarketplace Seller Growth", "LISTING_DETAIL", "Sell a similar item", "Post your own listing in minutes.", "/media/ads/detail-sell.svg", "/media/ads/detail-sell-mobile.svg", "/post", 40),
            new AdSeed("Real Estate Weekend", "LISTING_DETAIL", "Rental insurance", "Simple protection for renters and landlords.", "/media/ads/detail-rental-insurance.svg", "/media/ads/detail-rental-insurance-mobile.svg", "/search?category=property_rentals", 50),

            // Sidebar slider
            new AdSeed("Summer Marketplace Deals 2026", "SIDEBAR", "Costco-style local deals", "Bulk savings and local bargains.", "/media/ads/sidebar-costco.svg", "/media/ads/sidebar-costco-mobile.svg", "/search?promoted=true", 10),
            new AdSeed("Summer Marketplace Deals 2026", "SIDEBAR", "Best electronics picks", "Phones, laptops, and accessories near you.", "/media/ads/sidebar-electronics.svg", "/media/ads/sidebar-electronics-mobile.svg", "/search?category=electronics", 20),
            new AdSeed("South Bay Local Services", "SIDEBAR", "Home service pros", "Cleaners, movers, repair, and more.", "/media/ads/sidebar-services.svg", "/media/ads/sidebar-services-mobile.svg", "/search?category=services", 30),
            new AdSeed("Real Estate Weekend", "SIDEBAR", "Open house weekend", "Tour rentals and rooms available now.", "/media/ads/sidebar-rentals.svg", "/media/ads/sidebar-rentals-mobile.svg", "/search?category=property_rentals", 40),
            new AdSeed("Auto Buyer Week", "SIDEBAR", "Auto week", "Cars, bikes, trucks, and parts.", "/media/ads/sidebar-auto.svg", "/media/ads/sidebar-auto-mobile.svg", "/search?category=vehicles", 50),
            new AdSeed("OpenMarketplace Seller Growth", "SIDEBAR", "Featured seller tools", "Get badges, boosts, and more reach.", "/media/ads/sidebar-seller.svg", "/media/ads/sidebar-seller-mobile.svg", "/billing", 60),
            new AdSeed("Safety Trust Campaign", "SIDEBAR", "Marketplace safety", "Verify profiles and meet in public.", "/media/ads/sidebar-safety.svg", "/media/ads/sidebar-safety-mobile.svg", "/safety", 70),
            new AdSeed("Summer Marketplace Deals 2026", "SIDEBAR", "Weekend picks", "Popular local listings updated daily.", "/media/ads/sidebar-weekend.svg", "/media/ads/sidebar-weekend-mobile.svg", "/search", 80),


            new AdSeed("OpenMarketplace Seller Growth", "SELLER_PROFILE", "Featured seller spotlight", "Highlight your profile to get more followers.", "/media/ads/seller-profile-featured.svg", "/media/ads/seller-profile-featured-mobile.svg", "/billing", 10),
            new AdSeed("OpenMarketplace Seller Growth", "SELLER_PROFILE", "Business verification", "Verified businesses earn buyer confidence.", "/media/ads/seller-profile-verify.svg", "/media/ads/seller-profile-verify-mobile.svg", "/profile", 20)
        };

        foreach (var item in creatives)
        {
            var creative = await db.AdCreatives.FirstOrDefaultAsync(x => x.Placement == item.Placement && x.Title == item.Title, ct);
            if (creative is null)
            {
                creative = new AdCreative();
                db.AdCreatives.Add(creative);
            }

            creative.CampaignId = campaignIds[item.CampaignName];
            creative.Placement = item.Placement;
            creative.Title = item.Title;
            creative.Description = item.Description;
            creative.DesktopImageUrl = item.DesktopImageUrl;
            creative.MobileImageUrl = item.MobileImageUrl;
            creative.TargetUrl = item.TargetUrl;
            creative.OpenInNewTab = item.TargetUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase);
            creative.SortOrder = item.SortOrder;
            creative.Status = "Active";
            creative.MaxImpressions = 0;
            creative.MaxClicks = 0;
            creative.IsDeleted = false;
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task SeedUsersAsync(CancellationToken ct)
    {
        await UpsertUser(CustomerId, "David Wilson", "david@example.com", "408-555-0101", "San Jose, CA", "Customer", true, true, false, false, 78, 4.6m, 8, ct);
        await UpsertUser(SellerJohnId, "John Doe", "john@example.com", "408-123-4567", "San Jose, CA", "Seller", true, true, true, false, 96, 4.9m, 108, ct);
        await UpsertUser(SellerSarahId, "Sarah Smith", "sarah@example.com", "408-555-0202", "Santa Clara, CA", "Seller", true, true, true, true, 91, 4.8m, 42, ct);
        await UpsertUser(AdminId, "Admin", "admin@example.com", "408-555-9999", "Santa Clara, CA", "Admin", true, true, true, true, 100, 5m, 0, ct);
        await db.SaveChangesAsync(ct);
    }

    private async Task UpsertUser(Guid id, string name, string email, string phone, string location, string role, bool emailVerified, bool phoneVerified, bool idVerified, bool businessVerified, int trustScore, decimal rating, int reviewCount, CancellationToken ct)
    {
        var user = await db.UserProfiles.FirstOrDefaultAsync(x => x.Id == id || x.Email == email, ct);
        if (user is null)
        {
            user = new UserProfile { Id = id, Email = email };
            db.UserProfiles.Add(user);
        }
        user.Name = name;
        user.Phone = phone;
        user.Location = location;
        user.Role = role;
        user.EmailVerified = emailVerified;
        user.PhoneVerified = phoneVerified;
        user.IdVerified = idVerified;
        user.BusinessVerified = businessVerified;
        user.TrustScore = trustScore;
        user.Rating = rating;
        user.ReviewCount = reviewCount;
        user.Status = "Active";
        if (string.IsNullOrWhiteSpace(user.PasswordHash)) user.PasswordHash = HashPassword("Password123!");
        if (string.IsNullOrWhiteSpace(user.AvatarUrl)) user.AvatarUrl = $"/avatars/avatar-{Math.Abs(email.GetHashCode()) % 12 + 1}.svg";
    }

    private async Task SeedListingsAsync(CancellationToken ct)
    {
        var cats = await db.Categories.ToDictionaryAsync(x => x.Code, x => x.Id, ct);
        var samples = new[]
        {
            new ListingSeed("2024 BMW X5 xDrive40i — Low Miles", "2024-bmw-x5-xdrive40i-low-miles", "Excellent condition, low miles, one owner. Clean title. Meet in a public place for test drive.", 49900m, "San Jose, CA", "vehicles", SellerJohnId, true, false, true, "https://images.unsplash.com/photo-1555215695-3004980ad54e?auto=format&fit=crop&w=900&q=80", 2450, 28, 12),
            new ListingSeed("iPhone 15 Pro Max 256GB — Like New", "iphone-15-pro-max-256gb-like-new", "Battery 98%. Original box and charger. Always kept in case with screen protector.", 850m, "Santa Clara, CA", "electronics", SellerJohnId, true, true, false, "https://images.unsplash.com/photo-1695048133142-1a20484d2569?auto=format&fit=crop&w=900&q=80", 1856, 23, 9),
            new ListingSeed("Modern Gray Sofa — Excellent Condition", "modern-gray-sofa-excellent-condition", "Very comfortable sectional sofa. Smoke-free home. Pickup in Campbell.", 350m, "Campbell, CA", "home_garden", SellerSarahId, true, false, false, "https://images.unsplash.com/photo-1555041469-a586c61ea9bc?auto=format&fit=crop&w=900&q=80", 632, 11, 4),
            new ListingSeed("Trek Marlin 6 Mountain Bike", "trek-marlin-6-mountain-bike", "Great condition. Size M/L. Perfect for trails and commuting.", 400m, "Sunnyvale, CA", "sports_outdoors", SellerSarahId, false, false, false, "https://images.unsplash.com/photo-1485965120184-e220f721d03e?auto=format&fit=crop&w=900&q=80", 412, 9, 2),
            new ListingSeed("MacBook Air M2 13 inch — Like New", "macbook-air-m2-13-like-new", "Used for 3 months only. Comes with charger and sleeve.", 750m, "Sunnyvale, CA", "electronics", SellerJohnId, false, true, false, "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?auto=format&fit=crop&w=900&q=80", 320, 8, 2),
            new ListingSeed("2BR Apartment for Rent", "2br-apartment-for-rent", "Bright 2BR apartment near transit. Available next month.", 2200m, "San Jose, CA", "property_rentals", SellerSarahId, true, false, false, "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=900&q=80", 1240, 18, 5),
            new ListingSeed("Office Desk Set", "office-desk-set", "Desk, chair, and small drawer. Clean and ready for pickup.", 350m, "San Jose, CA", "for_sale", SellerSarahId, false, false, false, "https://images.unsplash.com/photo-1518455027359-f3f8164ba6bd?auto=format&fit=crop&w=900&q=80", 288, 7, 1),
            new ListingSeed("Local Moving Help", "local-moving-help", "Two-person moving help available on weekends around South Bay.", 80m, "Milpitas, CA", "services", SellerJohnId, false, false, false, "https://images.unsplash.com/photo-1600518464441-9306b2dcbb39?auto=format&fit=crop&w=900&q=80", 190, 4, 1)
        };

        foreach (var s in samples)
        {
            var listing = await db.Listings.FirstOrDefaultAsync(x => x.Slug == s.Slug, ct);
            if (listing is null)
            {
                listing = new Listing { Slug = s.Slug, CreatedAt = DateTimeOffset.UtcNow.AddHours(-Random.Shared.Next(2, 120)) };
                db.Listings.Add(listing);
            }
            listing.SellerId = s.SellerId;
            listing.CategoryId = cats.TryGetValue(s.CategorySlug, out var catId) ? catId : cats.Values.First();
            listing.Title = s.Title;
            listing.Description = s.Description;
            listing.Price = s.Price;
            listing.Currency = "USD";
            listing.Location = s.Location;
            listing.Status = "Published";
            listing.ModerationStatus = "Approved";
            listing.IsFeatured = s.Featured;
            listing.IsUrgent = s.Urgent;
            listing.IsPinned = s.Premium;
            listing.PackageCode = s.Premium ? "PREMIUM" : s.Urgent ? "URGENT" : s.Featured ? "FEATURED" : "FREE";
            listing.PackageStatus = "Active";
            listing.PackageStartsAt = DateTimeOffset.UtcNow.AddDays(-Random.Shared.Next(1, 10));
            listing.PackageEndsAt = DateTimeOffset.UtcNow.AddDays(s.Premium ? 30 : s.Urgent || s.Featured ? 7 : 30);
            listing.ViewCount = s.Views;
            listing.FavoriteCount = s.Favorites;
            listing.LikeCount = s.Favorites;
            listing.CommentCount = s.Comments;
            listing.PublishedAt = DateTimeOffset.UtcNow.AddDays(-Random.Shared.Next(1, 12));
            listing.ExpiresAt = DateTimeOffset.UtcNow.AddDays(30);
            await db.SaveChangesAsync(ct);

            if (!await db.MediaAssets.AnyAsync(x => x.ListingId == listing.Id, ct))
            {
                db.MediaAssets.Add(new MediaAsset
                {
                    OwnerId = listing.SellerId,
                    ListingId = listing.Id,
                    FileName = $"{s.Slug}.jpg",
                    ContentType = "image/jpeg",
                    SizeBytes = 100000,
                    Url = s.ImageUrl,
                    StorageProvider = "Seed"
                });
            }
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task SeedCommerceAsync(CancellationToken ct)
    {
        if (!await db.Orders.AnyAsync(ct))
        {
            db.Orders.AddRange(
                new Order { UserId = CustomerId, OrderNumber = "ORD-10001", Total = 19.99m, Status = "Paid", Provider = "StripeDemo", ProviderReference = "seed-premium" },
                new Order { UserId = CustomerId, OrderNumber = "ORD-10002", Total = 9.99m, Status = "Paid", Provider = "StripeDemo", ProviderReference = "seed-featured" },
                new Order { UserId = CustomerId, OrderNumber = "ORD-10003", Total = 50m, Status = "Paid", Provider = "StripeDemo", ProviderReference = "seed-credits" }
            );
            await db.SaveChangesAsync(ct);
            var orders = await db.Orders.Where(x => x.UserId == CustomerId).ToListAsync(ct);
            foreach (var order in orders)
            {
                if (!await db.Payments.AnyAsync(x => x.OrderId == order.Id, ct)) db.Payments.Add(new Payment { OrderId = order.Id, Amount = order.Total, Status = "Succeeded", Provider = "StripeDemo", ProviderReference = order.ProviderReference });
                if (!await db.Invoices.AnyAsync(x => x.OrderId == order.Id, ct)) db.Invoices.Add(new Invoice { OrderId = order.Id, InvoiceNumber = $"INV-{order.OrderNumber.Replace("ORD-", "")}", Amount = order.Total, Status = "Issued", PdfUrl = $"/api/v1/billing/invoices/{order.Id}/pdf" });
            }
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task SeedMessagingAsync(CancellationToken ct)
    {
        var listing = await db.Listings.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(ct);
        if (listing is not null && !await db.Conversations.AnyAsync(ct))
        {
            var conversation = new Conversation { ListingId = listing.Id, BuyerId = CustomerId, SellerId = SellerJohnId, Subject = listing.Title, LastMessageAt = DateTimeOffset.UtcNow.AddMinutes(-20) };
            db.Conversations.Add(conversation);
            await db.SaveChangesAsync(ct);
            db.Messages.AddRange(
                new Message { ConversationId = conversation.Id, SenderId = CustomerId, ReceiverId = SellerJohnId, Body = "Hi, is this still available?", CreatedAt = DateTimeOffset.UtcNow.AddHours(-2) },
                new Message { ConversationId = conversation.Id, SenderId = SellerJohnId, ReceiverId = CustomerId, Body = "Yes, it is available.", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-90) },
                new Message { ConversationId = conversation.Id, SenderId = CustomerId, ReceiverId = SellerJohnId, Body = "Can you do $870?", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-45) }
            );
        }

        if (!await db.Notifications.AnyAsync(x => x.UserId == CustomerId, ct))
        {
            db.Notifications.AddRange(
                new Notification { UserId = CustomerId, Type = "Message", Title = "New message", Body = "John replied to your conversation.", Url = "/messages" },
                new Notification { UserId = CustomerId, Type = "Listing", Title = "Listing approved", Body = "Your iPhone listing is live.", Url = "/my-listings" },
                new Notification { UserId = CustomerId, Type = "BILLING", Title = "Payment successful", Body = "Your featured listing payment was processed.", Url = "/billing", IsRead = true }
            );
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task SeedSystemDataAsync(CancellationToken ct)
    {
        var adPlacements = new[]
        {
            new { Code = "HOME_HERO", Name = "HOME_HERO", InsertEvery = 0 },
            new { Code = "HOME_FEED", Name = "HOME_FEED", InsertEvery = 6 },
            new { Code = "LISTING_DETAIL", Name = "LISTING_DETAIL", InsertEvery = 0 },
            new { Code = "SIDEBAR", Name = "SIDEBAR", InsertEvery = 0 },
            new { Code = "SELLER_PROFILE", Name = "SELLER_PROFILE", InsertEvery = 0 }
        };

        foreach (var item in adPlacements)
        {
            var placement = await db.AdPlacements.FirstOrDefaultAsync(x => x.Code == item.Code, ct);
            if (placement is null)
            {
                placement = new AdPlacement { Code = item.Code };
                db.AdPlacements.Add(placement);
            }

            placement.Name = item.Name;
            placement.InsertEvery = item.InsertEvery;
            placement.IsActive = true;
            placement.IsDeleted = false;
        }

        if (!await db.CmsPages.AnyAsync(ct))
        {
            db.CmsPages.AddRange(
                new CmsPage { Slug = "terms", Title = "Terms", ContentMd = "# Terms", Status = "Published" },
                new CmsPage { Slug = "privacy", Title = "Privacy", ContentMd = "# Privacy", Status = "Published" },
                new CmsPage { Slug = "safety", Title = "Safety Tips", ContentMd = "Meet in public places and report suspicious activity.", Status = "Published" }
            );
        }

        if (!await db.AppSettings.AnyAsync(ct))
        {
            db.AppSettings.AddRange(
                new AppSetting { Key = "commerce.default_currency", Value = "USD", IsPublic = true },
                new AppSetting { Key = "listing.default_expiration_days", Value = "30", ValueType = "Int" },
                new AppSetting { Key = "market.default_location", Value = "San Jose, CA", IsPublic = true }
            );
        }

        await db.SaveChangesAsync(ct);
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);
        return $"v1.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private sealed record AdSeed(string CampaignName, string Placement, string Title, string Description, string DesktopImageUrl, string MobileImageUrl, string TargetUrl, int SortOrder);

    private sealed record ListingSeed(string Title, string Slug, string Description, decimal Price, string Location, string CategorySlug, Guid SellerId, bool Featured, bool Urgent, bool Premium, string ImageUrl, int Views, int Favorites, int Comments);
}
