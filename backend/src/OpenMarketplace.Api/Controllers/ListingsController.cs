using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenMarketplace.Domain.Communication;
using OpenMarketplace.Domain.Engagement;
using OpenMarketplace.Domain.Listings;
using OpenMarketplace.Domain.Moderation;
using OpenMarketplace.Infrastructure.Persistence;
using OpenMarketplace.Api.Services;
using OpenMarketplace.Shared.Api;

namespace OpenMarketplace.Api.Controllers;

[ApiController]
[Route("api/v1/listings")]
public sealed class ListingsController(AppDbContext db, IContentModerationService moderation, IExternalMarketplaceService externalMarketplaces) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get(
        [FromQuery] string? q,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? category,
        [FromQuery] string? status,
        [FromQuery] Guid? sellerId,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var now = DateTimeOffset.UtcNow;

        var query =
            from listing in db.Listings.AsNoTracking()
            join categoryEntity in db.Categories.AsNoTracking()
                on listing.CategoryId equals categoryEntity.Id into categoryJoin
            from categoryEntity in categoryJoin.DefaultIfEmpty()
            join sellerEntity in db.UserProfiles.AsNoTracking()
                on listing.SellerId equals sellerEntity.Id into sellerJoin
            from sellerEntity in sellerJoin.DefaultIfEmpty()
            where !listing.IsDeleted
            select new
            {
                Listing = listing,
                Category = categoryEntity,
                Seller = sellerEntity
            };

        if (sellerId.HasValue)
            query = query.Where(x => x.Listing.SellerId == sellerId.Value);

        if (!string.IsNullOrWhiteSpace(status) &&
            !string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x =>
                x.Listing.Status == status || x.Listing.ModerationStatus == status);
        }
        else if (!sellerId.HasValue)
        {
            query = query.Where(x =>
                x.Listing.Status == "Published" &&
                (!x.Listing.ExpiresAt.HasValue || x.Listing.ExpiresAt >= now));
        }

        if (categoryId.HasValue)
            query = query.Where(x => x.Listing.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(category))
        {
            var categoryText = category.Trim();
            query = query.Where(x =>
                x.Category != null &&
                x.Category.IsActive &&
                !x.Category.IsDeleted &&
                (EF.Functions.ILike(x.Category.Code, categoryText) ||
                 EF.Functions.ILike(x.Category.Slug, categoryText) ||
                 EF.Functions.ILike(x.Category.Name, categoryText)));
        }

        string? searchTerm = null;
        string? containsPattern = null;
        string? startsWithPattern = null;

        if (!string.IsNullOrWhiteSpace(q))
        {
            searchTerm = string.Join(' ', q
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                .Trim();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                containsPattern = $"%{searchTerm}%";
                startsWithPattern = $"{searchTerm}%";

                query = query.Where(x =>
                    EF.Functions.ILike(x.Listing.Title, containsPattern) ||
                    EF.Functions.ILike(x.Listing.Description, containsPattern) ||
                    EF.Functions.ILike(x.Listing.Slug, containsPattern) ||
                    EF.Functions.ILike(x.Listing.Location, containsPattern) ||
                    EF.Functions.ILike(x.Listing.AddressLine, containsPattern) ||
                    EF.Functions.ILike(x.Listing.City, containsPattern) ||
                    EF.Functions.ILike(x.Listing.State, containsPattern) ||
                    EF.Functions.ILike(x.Listing.PostalCode, containsPattern) ||
                    (x.Category != null &&
                     (EF.Functions.ILike(x.Category.Name, containsPattern) ||
                      EF.Functions.ILike(x.Category.Code, containsPattern) ||
                      EF.Functions.ILike(x.Category.Slug, containsPattern))) ||
                    (x.Seller != null &&
                     EF.Functions.ILike(x.Seller.Name, containsPattern)));
            }
        }

        var total = await query.CountAsync(ct);

        var normalizedSort = sort?.Trim().ToLowerInvariant();

        var ordered = query
            .OrderByDescending(x => x.Listing.IsPinned)
            .ThenByDescending(x => x.Listing.IsFeatured);

        if (!string.IsNullOrWhiteSpace(searchTerm) &&
            containsPattern is not null &&
            startsWithPattern is not null)
        {
            ordered = ordered
                .ThenByDescending(x => EF.Functions.ILike(x.Listing.Title, searchTerm))
                .ThenByDescending(x => EF.Functions.ILike(x.Listing.Title, startsWithPattern))
                .ThenByDescending(x => EF.Functions.ILike(x.Listing.Title, containsPattern))
                .ThenByDescending(x => x.Category != null &&
                    (EF.Functions.ILike(x.Category.Name, searchTerm) ||
                     EF.Functions.ILike(x.Category.Code, searchTerm) ||
                     EF.Functions.ILike(x.Category.Slug, searchTerm)))
                .ThenByDescending(x => x.Category != null &&
                    (EF.Functions.ILike(x.Category.Name, containsPattern) ||
                     EF.Functions.ILike(x.Category.Code, containsPattern) ||
                     EF.Functions.ILike(x.Category.Slug, containsPattern)))
                .ThenByDescending(x => EF.Functions.ILike(x.Listing.City, containsPattern))
                .ThenByDescending(x => EF.Functions.ILike(x.Listing.AddressLine, containsPattern))
                .ThenByDescending(x => EF.Functions.ILike(x.Listing.Description, containsPattern));
        }

        ordered = normalizedSort switch
        {
            "price-low" => ordered.ThenBy(x => x.Listing.Price ?? decimal.MaxValue),
            "price-high" => ordered.ThenByDescending(x => x.Listing.Price ?? decimal.MinValue),
            "popular" => ordered
                .ThenByDescending(x => x.Listing.ViewCount)
                .ThenByDescending(x => x.Listing.FavoriteCount)
                .ThenByDescending(x => x.Listing.LikeCount),
            _ => ordered.ThenByDescending(x => x.Listing.CreatedAt)
        };

        var items = await ordered
            .ThenByDescending(x => x.Listing.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                id = x.Listing.Id,
                title = x.Listing.Title,
                price = x.Listing.Price,
                currency = x.Listing.Currency,
                location = x.Listing.Location,
                addressLine = x.Listing.HideExactLocation ? "" : x.Listing.AddressLine,
                city = x.Listing.City,
                state = x.Listing.State,
                postalCode = x.Listing.PostalCode,
                country = x.Listing.Country,
                latitude = x.Listing.Latitude,
                longitude = x.Listing.Longitude,
                locationSource = x.Listing.LocationSource,
                locationPrecision = x.Listing.LocationPrecision,
                hideExactLocation = x.Listing.HideExactLocation,
                description = x.Listing.Description,
                status = x.Listing.Status,
                moderationStatus = x.Listing.ModerationStatus,
                isFeatured = x.Listing.IsFeatured,
                isUrgent = x.Listing.IsUrgent,
                isPinned = x.Listing.IsPinned,
                packageCode = x.Listing.PackageCode,
                packageStatus = x.Listing.PackageStatus,
                packageStartsAt = x.Listing.PackageStartsAt,
                packageEndsAt = x.Listing.PackageEndsAt,
                viewCount = x.Listing.ViewCount,
                favoriteCount = x.Listing.FavoriteCount,
                likeCount = x.Listing.LikeCount,
                commentCount = x.Listing.CommentCount,
                createdAt = x.Listing.CreatedAt,
                categoryId = x.Listing.CategoryId,
                categoryCode = x.Category != null ? x.Category.Code : null,
                categoryName = x.Category != null ? x.Category.Name : null,
                categoryIconKey = x.Category != null ? x.Category.IconKey : null,
                sellerName = x.Seller != null ? x.Seller.Name : null,
                imageUrl = db.MediaAssets
                    .Where(m => m.ListingId == x.Listing.Id)
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => m.Url)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        ExternalMarketplaceSearchResult? external = null;
        var isPublicSearch = !sellerId.HasValue &&
            (string.IsNullOrWhiteSpace(status) || string.Equals(status, "All", StringComparison.OrdinalIgnoreCase));

        if (isPublicSearch)
        {
            // Customer calls only this listings endpoint. The backend owns all
            // provider enable/disable, local-threshold, cache and eBay API logic.
            var externalQuery = searchTerm;
            if (string.IsNullOrWhiteSpace(externalQuery) && !string.IsNullOrWhiteSpace(category))
                externalQuery = category.Trim().Replace('_', ' ').Replace('-', ' ');

            if (!string.IsNullOrWhiteSpace(externalQuery) && externalQuery.Length >= 2)
            {
                external = await externalMarketplaces.SearchAsync(
                    externalQuery,
                    categoryId: null,
                    postalCode: null,
                    limit: 100,
                    force: false,
                    localResultCount: total,
                    ct: ct);
            }
        }

        return Ok(ApiResponse<object>.Ok(new
        {
            items,
            page,
            pageSize,
            totalItems = total,
            totalPages = (int)Math.Ceiling(total / (double)pageSize),
            query = searchTerm,
            external
        }, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> GetById(Guid id, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var listing = await db.Listings.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && x.Status == "Published" && (!x.ExpiresAt.HasValue || x.ExpiresAt >= now), ct);
        if (listing is null) return NotFound(ApiResponse<object>.Fail("NotFound", "Listing not found", HttpContext.TraceIdentifier));

        listing.ViewCount++;
        await db.SaveChangesAsync(ct);

        var media = await db.MediaAssets.AsNoTracking().Where(x => x.ListingId == id).OrderBy(x => x.CreatedAt).ToListAsync(ct);
        var comments = await db.ListingComments.AsNoTracking().Where(x => x.ListingId == id && x.Status == "Published").OrderByDescending(x => x.CreatedAt).Take(20).ToListAsync(ct);
        var category = await db.Categories.AsNoTracking().Where(x => x.Id == listing.CategoryId).Select(x => new { x.Id, x.Code, x.IconKey, x.ParentCode, x.Name, x.Slug }).FirstOrDefaultAsync(ct);
        var seller = await db.UserProfiles.AsNoTracking().Where(x => x.Id == listing.SellerId).Select(x => new { x.Id, x.Name, x.Email, x.Phone, x.Location, x.Rating, x.ReviewCount, x.TrustScore, x.EmailVerified, x.PhoneVerified, x.IdVerified, x.BusinessVerified }).FirstOrDefaultAsync(ct);

        return Ok(ApiResponse<object>.Ok(new
        {
            listing = new
            {
                id = listing.Id,
                title = listing.Title,
                price = listing.Price,
                currency = listing.Currency,
                location = listing.Location,
                addressLine = listing.HideExactLocation ? "" : listing.AddressLine,
                city = listing.City,
                state = listing.State,
                postalCode = listing.PostalCode,
                country = listing.Country,
                latitude = listing.Latitude,
                longitude = listing.Longitude,
                locationSource = listing.LocationSource,
                locationPrecision = listing.LocationPrecision,
                hideExactLocation = listing.HideExactLocation,
                description = listing.Description,
                status = listing.Status,
                moderationStatus = listing.ModerationStatus,
                isFeatured = listing.IsFeatured,
                isUrgent = listing.IsUrgent,
                isPinned = listing.IsPinned,
                packageCode = listing.PackageCode,
                packageStatus = listing.PackageStatus,
                packageStartsAt = listing.PackageStartsAt,
                packageEndsAt = listing.PackageEndsAt,
                viewCount = listing.ViewCount,
                favoriteCount = listing.FavoriteCount,
                likeCount = listing.LikeCount,
                commentCount = listing.CommentCount,
                createdAt = listing.CreatedAt,
                categoryId = listing.CategoryId,
                categoryCode = category != null ? category.Code : null,
                categoryName = category != null ? category.Name : null,
                categoryIconKey = category != null ? category.IconKey : null,
                imageUrl = media.Select(x => x.Url).FirstOrDefault()
            },
            media,
            comments,
            category,
            seller
        }, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Listing>>> Create(CreateListingRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.City) || string.IsNullOrWhiteSpace(request.AddressLine))
            return BadRequest(ApiResponse<Listing>.Fail("Validation", "City and address or pickup area are required", HttpContext.TraceIdentifier));
        var normalizedPackageCode = NormalizePackageCode(request.PackageCode);
        var package = await db.Packages.AsNoTracking()
            .FirstOrDefaultAsync(x => (request.PackageId.HasValue && x.Id == request.PackageId.Value) || (x.Code == normalizedPackageCode && x.IsActive && !x.IsDeleted), ct);
        if (package is not null) normalizedPackageCode = NormalizePackageCode(package.Code);
        var now = DateTimeOffset.UtcNow;
        var packageStatus = package is null || package.Price <= 0 ? "Active" : "PendingPayment";
        var listing = new Listing
        {
            SellerId = request.SellerId == Guid.Empty ? DemoIds.Customer : request.SellerId,
            CategoryId = request.CategoryId,
            Title = request.Title.Trim(),
            Slug = Slugify(request.Title),
            Description = request.Description.Trim(),
            Price = request.Price,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency,
            Location = BuildDisplayLocation(request),
            AddressLine = request.AddressLine?.Trim() ?? "",
            City = request.City?.Trim() ?? "",
            State = request.State?.Trim() ?? "",
            PostalCode = request.PostalCode?.Trim() ?? "",
            Country = string.IsNullOrWhiteSpace(request.Country) ? "US" : request.Country.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            LocationSource = NormalizeLocationSource(request.LocationSource),
            LocationPrecision = NormalizeLocationPrecision(request.LocationPrecision, request.HideExactLocation),
            HideExactLocation = request.HideExactLocation ?? true,
            Status = packageStatus == "Active" ? "Published" : "Pending",
            ModerationStatus = "Pending",
            PackageCode = package?.Code ?? normalizedPackageCode,
            PackageStatus = packageStatus,
            PackageStartsAt = packageStatus == "Active" ? now : null,
            PackageEndsAt = packageStatus == "Active" ? now.AddDays(package?.DurationDays ?? 30) : null,
            IsFeatured = packageStatus == "Active" && string.Equals(normalizedPackageCode, "FEATURED", StringComparison.OrdinalIgnoreCase),
            IsUrgent = packageStatus == "Active" && string.Equals(normalizedPackageCode, "URGENT", StringComparison.OrdinalIgnoreCase),
            IsPinned = packageStatus == "Active" && string.Equals(normalizedPackageCode, "PREMIUM", StringComparison.OrdinalIgnoreCase),
            ExpiresAt = now.AddDays(package?.DurationDays ?? 30)
        };
        var moderationResult = await moderation.CheckTextAsync(listing.Title, listing.Description, ct);
        ApplyModeration(listing, moderationResult, packageStatus);
        db.Listings.Add(listing);
        db.ListingModerationResults.Add(ToModerationEntity(listing.Id, "Text", moderationResult));
        await SaveUserLocationAsync(listing, ct);
        db.Notifications.Add(new Notification
        {
            UserId = listing.SellerId,
            Type = "Listing",
            Title = "Listing submitted",
            Body = $"{listing.Title} is pending review.",
            Url = "/my-listings",
            EntityType = "Listing",
            EntityId = listing.Id
        });
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<Listing>.Ok(listing, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<Listing>>> Update(Guid id, CreateListingRequest request, CancellationToken ct)
    {
        var listing = await db.Listings.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (listing is null) return NotFound(ApiResponse<Listing>.Fail("NotFound", "Listing not found", HttpContext.TraceIdentifier));
        listing.Title = request.Title.Trim();
        listing.Slug = Slugify(request.Title);
        listing.Description = request.Description.Trim();
        listing.Price = request.Price;
        listing.Currency = request.Currency ?? "USD";
        listing.Location = BuildDisplayLocation(request);
        listing.AddressLine = request.AddressLine?.Trim() ?? listing.AddressLine;
        listing.City = request.City?.Trim() ?? listing.City;
        listing.State = request.State?.Trim() ?? listing.State;
        listing.PostalCode = request.PostalCode?.Trim() ?? listing.PostalCode;
        listing.Country = string.IsNullOrWhiteSpace(request.Country) ? listing.Country : request.Country.Trim();
        listing.Latitude = request.Latitude ?? listing.Latitude;
        listing.Longitude = request.Longitude ?? listing.Longitude;
        listing.LocationSource = NormalizeLocationSource(request.LocationSource);
        listing.LocationPrecision = NormalizeLocationPrecision(request.LocationPrecision, request.HideExactLocation);
        listing.HideExactLocation = request.HideExactLocation ?? listing.HideExactLocation;
        listing.CategoryId = request.CategoryId;
        var moderationResult = await moderation.CheckTextAsync(listing.Title, listing.Description, ct);
        ApplyModeration(listing, moderationResult, listing.PackageStatus);
        listing.UpdatedAt = DateTimeOffset.UtcNow;
        db.ListingModerationResults.Add(ToModerationEntity(listing.Id, "Text", moderationResult));
        await SaveUserLocationAsync(listing, ct);
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<Listing>.Ok(listing, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/favorite")]
    public async Task<ActionResult<ApiResponse<object>>> Favorite(Guid id, [FromBody] UserActionRequest request, CancellationToken ct)
    {
        var userId = request.UserId == Guid.Empty ? DemoIds.Customer : request.UserId;
        if (!await db.Favorites.AnyAsync(x => x.UserId == userId && x.ListingId == id, ct))
        {
            db.Favorites.Add(new Favorite { UserId = userId, ListingId = id });
            var listing = await db.Listings.FindAsync([id], ct);
            if (listing is not null)
            {
                listing.FavoriteCount++;
                if (listing.SellerId != userId)
                {
                    var imageUrl = await db.MediaAssets.AsNoTracking()
                        .Where(x => x.ListingId == listing.Id)
                        .OrderBy(x => x.CreatedAt)
                        .Select(x => x.Url)
                        .FirstOrDefaultAsync(ct) ?? "";
                    db.Notifications.Add(new Notification
                    {
                        UserId = listing.SellerId,
                        Type = "Listing",
                        Title = "Listing saved",
                        Body = $"Someone saved {listing.Title}.",
                        Url = $"/listings/{listing.Id}",
                        EntityType = "Listing",
                        EntityId = listing.Id,
                        ImageUrl = imageUrl
                    });
                }
            }
            await db.SaveChangesAsync(ct);
        }
        return Ok(ApiResponse<object>.Ok(new { saved = true }, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/like")]
    public async Task<ActionResult<ApiResponse<object>>> Like(Guid id, [FromBody] UserActionRequest request, CancellationToken ct)
    {
        var userId = request.UserId == Guid.Empty ? DemoIds.Customer : request.UserId;
        if (!await db.ListingLikes.AnyAsync(x => x.UserId == userId && x.ListingId == id, ct)) { db.ListingLikes.Add(new ListingLike{UserId=userId,ListingId=id}); var listing = await db.Listings.FindAsync([id], ct); if (listing is not null) listing.LikeCount++; await db.SaveChangesAsync(ct); }
        return Ok(ApiResponse<object>.Ok(new{liked=true}, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<ApiResponse<ListingComment>>> Comment(Guid id, CommentRequest request, CancellationToken ct)
    {
        var comment = new ListingComment { ListingId = id, UserId = request.UserId == Guid.Empty ? DemoIds.Customer : request.UserId, Body = request.Body };
        db.ListingComments.Add(comment);
        var listing = await db.Listings.FindAsync([id], ct);
        if (listing is not null)
        {
            listing.CommentCount++;
            if (listing.SellerId != comment.UserId)
            {
                var imageUrl = await db.MediaAssets.AsNoTracking()
                    .Where(x => x.ListingId == listing.Id)
                    .OrderBy(x => x.CreatedAt)
                    .Select(x => x.Url)
                    .FirstOrDefaultAsync(ct) ?? "";
                db.Notifications.Add(new Notification
                {
                    UserId = listing.SellerId,
                    Type = "Listing",
                    Title = "New comment",
                    Body = comment.Body.Length > 160 ? comment.Body[..160] : comment.Body,
                    Url = $"/listings/{listing.Id}",
                    EntityType = "Listing",
                    EntityId = listing.Id,
                    ImageUrl = imageUrl
                });
            }
        }
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<ListingComment>.Ok(comment, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/report")]
    public async Task<ActionResult<ApiResponse<Report>>> Report(Guid id, ReportRequest request, CancellationToken ct)
    {
        var report = new Report{ReporterId=request.UserId==Guid.Empty?DemoIds.Customer:request.UserId,TargetType="Listing",TargetId=id,Reason=request.Reason,Details=request.Details??""};
        db.Reports.Add(report); await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<Report>.Ok(report, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/message")]
    public async Task<ActionResult<ApiResponse<object>>> MessageSeller(Guid id, MessageSellerRequest request, CancellationToken ct)
    {
        var listing = await db.Listings.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (listing is null) return NotFound(ApiResponse<object>.Fail("NotFound", "Listing not found", HttpContext.TraceIdentifier));

        var body = (request.Body ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(body)) return BadRequest(ApiResponse<object>.Fail("Validation", "Message body is required", HttpContext.TraceIdentifier));

        var buyerId = request.BuyerId == Guid.Empty ? DemoIds.Customer : request.BuyerId;
        if (buyerId == listing.SellerId) return BadRequest(ApiResponse<object>.Fail("Validation", "Seller cannot message their own listing", HttpContext.TraceIdentifier));

        var conversation = await db.Conversations.FirstOrDefaultAsync(x => x.ListingId == id && x.BuyerId == buyerId && x.SellerId == listing.SellerId && !x.IsDeleted, ct);
        if (conversation is null)
        {
            conversation = new Conversation { ListingId = id, BuyerId = buyerId, SellerId = listing.SellerId, Subject = listing.Title };
            db.Conversations.Add(conversation);
        }

        var message = new Message { ConversationId = conversation.Id, SenderId = buyerId, ReceiverId = listing.SellerId, MessageType = "Text", Body = body };
        conversation.LastMessageAt = DateTimeOffset.UtcNow;
        conversation.LastMessageId = message.Id;
        conversation.LastMessagePreview = body.Length > 160 ? body[..160] : body;
        conversation.UpdatedAt = DateTimeOffset.UtcNow;
        db.Messages.Add(message);
        var listingImage = await db.MediaAssets.AsNoTracking()
            .Where(x => x.ListingId == listing.Id)
            .OrderBy(x => x.CreatedAt)
            .Select(x => x.Url)
            .FirstOrDefaultAsync(ct) ?? "";
        db.Notifications.Add(new Notification
        {
            UserId = listing.SellerId,
            Type = "Message",
            Title = "New message",
            Body = body.Length > 160 ? body[..160] : body,
            Url = $"/messages?conversationId={conversation.Id}",
            EntityType = "Conversation",
            EntityId = conversation.Id,
            ImageUrl = listingImage
        });
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { conversation, message }, HttpContext.TraceIdentifier));
    }

    private static void ApplyModeration(Listing listing, ModerationCheckResult result, string packageStatus)
    {
        listing.ModerationStatus = result.Status;
        listing.ModerationReason = result.IsSafe ? string.Empty : result.Reason;
        if (result.IsRejected)
        {
            listing.Status = "Rejected";
            return;
        }

        if (result.NeedsReview)
        {
            listing.Status = "Pending";
            return;
        }

        listing.Status = packageStatus == "Active" ? "Published" : "Pending";
    }

    private static ListingModerationResult ToModerationEntity(Guid listingId, string targetType, ModerationCheckResult result) => new()
    {
        ListingId = listingId,
        TargetType = targetType,
        Status = result.Status,
        Reason = result.Reason,
        Categories = result.Categories,
        MaxScore = result.MaxScore,
        RawResponse = result.RawResponse
    };

    private async Task SaveUserLocationAsync(Listing listing, CancellationToken ct)
    {
        if (listing.SellerId == Guid.Empty || string.IsNullOrWhiteSpace(listing.City) || string.IsNullOrWhiteSpace(listing.AddressLine)) return;
        var city = listing.City.Trim(); var address = listing.AddressLine.Trim();
        var normalizedCity = city.ToLower(); var normalizedAddress = address.ToLower();
        var saved = await db.UserLocations.FirstOrDefaultAsync(x => x.UserId == listing.SellerId && !x.IsDeleted && x.City.ToLower() == normalizedCity && x.AddressLine.ToLower() == normalizedAddress, ct);
        if (saved is null)
        {
            saved = new OpenMarketplace.Domain.Locations.UserLocation { UserId = listing.SellerId, CreatedBy = listing.SellerId };
            db.UserLocations.Add(saved);
        }
        saved.Label = $"{address}, {city}"; saved.AddressLine = address; saved.City = city; saved.State = listing.State; saved.PostalCode = listing.PostalCode; saved.Country = listing.Country;
        saved.Latitude = listing.Latitude; saved.Longitude = listing.Longitude; saved.UseCount++; saved.LastUsedAt = DateTimeOffset.UtcNow; saved.UpdatedAt = DateTimeOffset.UtcNow; saved.UpdatedBy = listing.SellerId;
    }

    private static string BuildDisplayLocation(CreateListingRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Location)) return request.Location.Trim();
        var parts = new[] { request.City, request.State, request.PostalCode }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim());
        var display = string.Join(", ", parts);
        return string.IsNullOrWhiteSpace(display) ? "" : display;
    }

    private static string NormalizeLocationSource(string? value)
    {
        var source = (value ?? "Manual").Trim();
        return source.Equals("Geocoded", StringComparison.OrdinalIgnoreCase) ? "Geocoded"
            : source.Equals("SavedLocation", StringComparison.OrdinalIgnoreCase) ? "SavedLocation"
            : source.Equals("PinAdjusted", StringComparison.OrdinalIgnoreCase) ? "PinAdjusted"
            : source.Equals("CurrentLocation", StringComparison.OrdinalIgnoreCase) ? "CurrentLocation"
            : "Manual";
    }

    private static string NormalizeLocationPrecision(string? value, bool? hideExactLocation)
    {
        var precision = (value ?? string.Empty).Trim();
        if (precision.Equals("Exact", StringComparison.OrdinalIgnoreCase) && hideExactLocation == false) return "Exact";
        if (precision.Equals("Hidden", StringComparison.OrdinalIgnoreCase)) return "Hidden";
        return "ApproximateCity";
    }

    private static string NormalizePackageCode(string? code)
    {
        var normalized = (code ?? "FREE").Trim().ToUpperInvariant();
        return normalized switch
        {
            "BASIC" => "BASIC",
            "FEATURED" => "FEATURED",
            "URGENT" => "URGENT",
            "PREMIUM" => "PREMIUM",
            _ => "FREE"
        };
    }

    private static string Slugify(string value) => string.Join('-', value.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries)).Trim('-') + "-" + Guid.NewGuid().ToString("N")[..6];
}

public sealed record CreateListingRequest(
    Guid SellerId,
    Guid CategoryId,
    string Title,
    string Description,
    decimal? Price,
    string? Currency,
    string? Location,
    string? PackageCode,
    Guid? PackageId,
    string? AddressLine,
    string? City,
    string? State,
    string? PostalCode,
    string? Country,
    decimal? Latitude,
    decimal? Longitude,
    string? LocationSource,
    string? LocationPrecision,
    bool? HideExactLocation);
public sealed record UserActionRequest(Guid UserId);
public sealed record CommentRequest(Guid UserId, string Body);
public sealed record ReportRequest(Guid UserId, string Reason, string? Details);
public sealed record MessageSellerRequest(Guid BuyerId, string Body);
public static class DemoIds { public static readonly Guid Customer = Guid.Parse("01990000-0000-7000-8000-000000000001"); public static readonly Guid Seller = Guid.Parse("01990000-0000-7000-8000-000000000002"); }
