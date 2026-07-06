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
            new { Code = "FREE", Name = "FREE", Price = 0m, Days = 30, SortOrder = 10 },
            new { Code = "BASIC", Name = "BASIC", Price = 4.99m, Days = 30, SortOrder = 20 },
            new { Code = "URGENT", Name = "URGENT", Price = 4.99m, Days = 7, SortOrder = 30 },
            new { Code = "FEATURED", Name = "FEATURED", Price = 9.99m, Days = 7, SortOrder = 40 },
            new { Code = "PREMIUM", Name = "PREMIUM", Price = 19.99m, Days = 30, SortOrder = 50 },
            new { Code = "CREDITS100", Name = "CREDITS100", Price = 50m, Days = 365, SortOrder = 60 }
        };
        foreach (var item in packages)
        {
            var package = await db.Packages.FirstOrDefaultAsync(x => x.Code == item.Code, ct);
            if (package is null) db.Packages.Add(new Package { Code = item.Code, Name = item.Name, Price = item.Price, DurationDays = item.Days, SortOrder = item.SortOrder, IsActive = true });
            else { package.Name = item.Name; package.Price = item.Price; package.DurationDays = item.Days; package.SortOrder = item.SortOrder; package.IsActive = true; }
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

        // Keep ad seed idempotent and clean. Older seed versions created many ads per placement
        // and sometimes left duplicate AdPlacements. Collapse duplicate placements by Code, then
        // replace demo creatives with exactly 2 ads per placement.
        var placements = new[]
        {
            new { Code = "HOME_HERO", Name = "Homepage Hero", InsertEvery = 1 },
            new { Code = "HOME_FEED", Name = "Homepage Feed Bottom", InsertEvery = 6 },
            new { Code = "SIDEBAR", Name = "SIDEBAR", InsertEvery = 1 }
        };
        var allowedPlacementCodes = placements.Select(x => x.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var existingPlacements = await db.AdPlacements.OrderBy(x => x.CreatedAt).ToListAsync(ct);
        foreach (var existing in existingPlacements)
        {
            if (existing.Code.Equals("Sidebar", StringComparison.OrdinalIgnoreCase))
            {
                existing.Code = "SIDEBAR";
                existing.Name = "SIDEBAR";
                existing.IsActive = true;
                existing.IsDeleted = false;
                existing.UpdatedAt = now;
            }
        }

        var sidebarCreatives = await db.AdCreatives.Where(x => x.Placement == "Sidebar" || x.Placement == "sidebar").ToListAsync(ct);
        foreach (var creative in sidebarCreatives)
        {
            creative.Placement = "SIDEBAR";
            creative.IsDeleted = false;
            creative.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);

        existingPlacements = await db.AdPlacements.OrderBy(x => x.CreatedAt).ToListAsync(ct);
        var duplicatePlacements = existingPlacements
            .GroupBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .SelectMany(g => g.Skip(1))
            .ToList();
        var unusedPlacements = existingPlacements
            .Where(x => !allowedPlacementCodes.Contains(x.Code))
            .ToList();
        db.AdPlacements.RemoveRange(duplicatePlacements.Concat(unusedPlacements).DistinctBy(x => x.Id));

        foreach (var item in placements)
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
            placement.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);

        var campaign = await db.AdCampaigns.FirstOrDefaultAsync(x => x.Name == "OpenMarketplace Demo Ads", ct);
        if (campaign is null)
        {
            campaign = new AdCampaign { Name = "OpenMarketplace Demo Ads" };
            db.AdCampaigns.Add(campaign);
        }

        campaign.Status = "Active";
        campaign.StartDate = now.AddDays(-30);
        campaign.EndDate = now.AddDays(365);
        campaign.Priority = 100;
        campaign.Budget = 0;
        campaign.IsDeleted = false;
        campaign.UpdatedAt = now;
        await db.SaveChangesAsync(ct);

        var creatives = new[]
        {
            new AdSeed(campaign.Name, "HOME_HERO", "Local Marketplace Deals", "Fresh offers from trusted sellers around you.", "https://images.unsplash.com/photo-1556742049-0cfed4f6a45d?auto=format&fit=crop&w=1600&q=80", "/search?promoted=true", 10),
            new AdSeed(campaign.Name, "HOME_HERO", "Trusted Local Services", "Book movers, repair, cleaning, and more near you.", "https://images.unsplash.com/photo-1521791136064-7986c2920216?auto=format&fit=crop&w=1600&q=80", "/search?category=services", 20),

            new AdSeed(campaign.Name, "HOME_FEED", "Weekend Moving Help", "Book local movers and delivery helpers.", "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?auto=format&fit=crop&w=1400&q=80", "/search?category=services", 10),
            new AdSeed(campaign.Name, "HOME_FEED", "Electronics Sale", "Phones, laptops, tablets, and accessories.", "https://images.unsplash.com/photo-1516321318423-f06f85e504b3?auto=format&fit=crop&w=1400&q=80", "/search?category=electronics", 20),

            new AdSeed(campaign.Name, "SIDEBAR", "Best Electronics Picks", "Phones, laptops, and accessories near you.", "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?auto=format&fit=crop&w=900&q=80", "/search?category=electronics", 10),
            new AdSeed(campaign.Name, "SIDEBAR", "Home Service Pros", "Cleaners, movers, repair, and more.", "https://images.unsplash.com/photo-1581578731548-c64695cc6952?auto=format&fit=crop&w=900&q=80", "/search?category=services", 20)
        };

        var seedTitles = creatives.Select(x => x.Title).Distinct().ToList();
        var oldCampaignNames = new[]
        {
            "Summer Marketplace Deals 2026",
            "South Bay Local Services",
            "Real Estate Weekend",
            "Auto Buyer Week",
            "OpenMarketplace Seller Growth",
            "Safety Trust Campaign",
            "OpenMarketplace Demo Ads"
        };
        var oldSeedCampaignIds = await db.AdCampaigns
            .Where(x => oldCampaignNames.Contains(x.Name))
            .Select(x => x.Id)
            .ToListAsync(ct);
        var oldSeedAds = await db.AdCreatives
            .Where(x => (oldSeedCampaignIds.Contains(x.CampaignId) || allowedPlacementCodes.Contains(x.Placement)) && !seedTitles.Contains(x.Title))
            .ToListAsync(ct);
        db.AdCreatives.RemoveRange(oldSeedAds);

        foreach (var item in creatives)
        {
            var duplicateAds = await db.AdCreatives
                .Where(x => x.Placement == item.Placement && x.Title == item.Title)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync(ct);

            var creative = duplicateAds.FirstOrDefault();
            if (creative is null)
            {
                creative = new AdCreative();
                db.AdCreatives.Add(creative);
            }

            if (duplicateAds.Count > 1)
                db.AdCreatives.RemoveRange(duplicateAds.Skip(1));

            creative.CampaignId = campaign.Id;
            creative.Placement = item.Placement;
            creative.Title = item.Title;
            creative.Description = item.Description;
            creative.DesktopImageUrl = item.ImageUrl;
            creative.MobileImageUrl = item.ImageUrl;
            creative.TargetUrl = item.TargetUrl;
            creative.OpenInNewTab = item.TargetUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase);
            creative.SortOrder = item.SortOrder;
            creative.Status = "Active";
            creative.IsDeleted = false;
            creative.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);
    }


    private async Task SeedUsersAsync(CancellationToken ct)
    {
        await UpsertUser(CustomerId, "David Wilson", "david@example.com", "408-555-0101", "San Jose, CA", "Customer", true, true, false, false, 78, 4.6m, 8, ct);
        await UpsertUser(SellerJohnId, "John Doe", "john@example.com", "408-123-4567", "San Jose, CA", "Seller", true, true, true, false, 96, 4.9m, 108, ct);
        await UpsertUser(SellerSarahId, "Sarah Smith", "sarah@example.com", "408-555-0202", "Santa Clara, CA", "Seller", true, true, true, true, 91, 4.8m, 42, ct);
        await UpsertUser(AdminId, "Admin", "admin@example.com", "408-555-9999", "Santa Clara, CA", "SuperAdmin", true, true, true, true, 100, 5m, 0, ct);
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
        user.Source = role is "Admin" or "SuperAdmin" or "System" ? "AdminCreated" : role == "Seller" ? "WebCustomer" : user.Source;
        if (string.IsNullOrWhiteSpace(user.Source)) user.Source = "WebCustomer";
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
        // Ad placement data is owned by SeedAdvertisementsAsync so old seed versions cannot
        // re-add disabled/unused placement rows here.

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

    private sealed record AdSeed(string CampaignName, string Placement, string Title, string Description, string ImageUrl, string TargetUrl, int SortOrder);

    private sealed record ListingSeed(string Title, string Slug, string Description, decimal Price, string Location, string CategorySlug, Guid SellerId, bool Featured, bool Urgent, bool Premium, string ImageUrl, int Views, int Favorites, int Comments);
}
