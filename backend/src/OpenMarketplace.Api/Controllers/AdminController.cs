using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenMarketplace.Domain.Moderation;
using OpenMarketplace.Domain.Communication;
using OpenMarketplace.Domain.Categories;
using OpenMarketplace.Domain.Commerce;
using OpenMarketplace.Domain.Users;
using OpenMarketplace.Infrastructure.Persistence;
using OpenMarketplace.Shared.Api;

namespace OpenMarketplace.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = "Admin,SuperAdmin,System,Moderator,Support")]
public sealed class AdminController(AppDbContext db) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<object>>> Dashboard(CancellationToken ct)
    {
        var totalListings = await db.Listings.CountAsync(ct);
        var pendingListings = await db.Listings.CountAsync(x=>x.Status=="Pending" || x.ModerationStatus=="Pending",ct);
        var users = await db.UserProfiles.CountAsync(ct);
        var reports = await db.Reports.CountAsync(x=>x.Status=="Open",ct);
        var orders = await db.Orders.CountAsync(ct);
        var revenue = await db.Payments.Where(x=>x.Status=="Succeeded").SumAsync(x=>(decimal?)x.Amount,ct) ?? 0;
        var recentListings = await db.Listings.AsNoTracking().OrderByDescending(x=>x.CreatedAt).Take(10).ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(new{stats=new{totalListings,pendingListings,users,reports,orders,revenue},recentListings},HttpContext.TraceIdentifier));
    }

    [HttpGet("listings")]
    public async Task<ActionResult<ApiResponse<object>>> Listings([FromQuery]string? status, [FromQuery]string? moderationStatus, [FromQuery]string? q, [FromQuery]int page = 1, [FromQuery]int pageSize = 10, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : pageSize > 100 ? 100 : pageSize;
        var query = db.Listings.AsNoTracking().AsQueryable();
        if(!string.IsNullOrWhiteSpace(status) && status != "All") query = query.Where(x=>x.Status==status || x.ModerationStatus==status);
        if(!string.IsNullOrWhiteSpace(moderationStatus) && moderationStatus != "All") query = query.Where(x=>x.ModerationStatus==moderationStatus || x.Status==moderationStatus);
        if(!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(x => x.Title.ToLower().Contains(term) || x.Description.ToLower().Contains(term) || x.City.ToLower().Contains(term) || x.State.ToLower().Contains(term));
        }
        var total = await query.CountAsync(ct);
        var listings = await query.OrderByDescending(x=>x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var sellerIds = listings.Select(x => x.SellerId).Distinct().ToList();
        var categoryIds = listings.Select(x => x.CategoryId).Distinct().ToList();
        var listingIds = listings.Select(x => x.Id).ToList();
        var sellers = await db.UserProfiles.AsNoTracking().Where(x => sellerIds.Contains(x.Id)).Select(x => new { x.Id, x.Name, x.Email }).ToDictionaryAsync(x => x.Id, ct);
        var categories = await db.Categories.AsNoTracking().Where(x => categoryIds.Contains(x.Id)).Select(x => new { x.Id, x.Name, x.Code }).ToDictionaryAsync(x => x.Id, ct);
        var media = await db.MediaAssets.AsNoTracking().Where(x => x.ListingId != null && listingIds.Contains(x.ListingId.Value) && !x.IsDeleted).OrderBy(x => x.CreatedAt).Select(x => new { ListingId = x.ListingId!.Value, x.Url }).ToListAsync(ct);
        var images = media.GroupBy(x => x.ListingId).ToDictionary(x => x.Key, x => x.First().Url);
        var items = listings.Select(x =>
        {
            sellers.TryGetValue(x.SellerId, out var seller);
            categories.TryGetValue(x.CategoryId, out var category);
            images.TryGetValue(x.Id, out var imageUrl);
            return new { x.Id, x.Title, x.Price, x.Currency, x.Status, x.ModerationStatus, x.City, x.State, x.CreatedAt, x.PublishedAt, x.ExpiresAt, Seller = seller, Category = category, ImageUrl = imageUrl ?? "" };
        });
        return Ok(ApiResponse<object>.Ok(new{items,total,page,pageSize},HttpContext.TraceIdentifier));
    }

    [HttpPost("listings/{id:guid}/moderate")]
    public async Task<ActionResult<ApiResponse<object>>> Moderate(Guid id, ModerateRequest request, CancellationToken ct)
    {
        var listing = await db.Listings.FindAsync([id],ct);
        if(listing is null) return NotFound(ApiResponse<object>.Fail("NotFound","Listing not found",HttpContext.TraceIdentifier));
        listing.ModerationStatus = request.Decision;
        listing.ModerationReason = request.Reason ?? "";
        listing.Status = request.Decision == "Approved" ? "Published" : request.Decision == "Rejected" ? "Rejected" : "Pending";
        listing.PublishedAt = listing.Status=="Published" ? DateTimeOffset.UtcNow : listing.PublishedAt;
        db.AuditLogs.Add(new AuditLog{ActorId=request.AdminId,Action=$"Listing {request.Decision}",EntityType="Listing",EntityId=id,Details=request.Reason??""});
        db.Notifications.Add(new OpenMarketplace.Domain.Communication.Notification{UserId=listing.SellerId,Type="Moderation",Title=$"Listing {request.Decision}",Body=listing.ModerationReason,Url="/my-listings"});
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new{listing},HttpContext.TraceIdentifier));
    }


    [HttpPost("listings/{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateListingStatus(Guid id, UpdateListingStatusRequest request, CancellationToken ct)
    {
        var listing = await db.Listings.FindAsync([id], ct);
        if (listing is null) return NotFound(ApiResponse<object>.Fail("NotFound", "Listing not found", HttpContext.TraceIdentifier));
        var status = string.IsNullOrWhiteSpace(request.Status) ? listing.Status : request.Status.Trim();
        listing.Status = status;
        listing.ModerationStatus = status switch
        {
            "Active" or "Published" or "Approved" => "Approved",
            "Inactive" or "Rejected" => "Rejected",
            "Pending" or "PendingApproval" => "Pending",
            _ => listing.ModerationStatus
        };
        listing.ModerationReason = request.Reason ?? listing.ModerationReason;
        if (listing.Status is "Published" or "Active" or "Approved") listing.PublishedAt ??= DateTimeOffset.UtcNow;
        listing.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { ActorId = request.AdminId, Action = $"Listing status changed to {listing.Status}", EntityType = "Listing", EntityId = id, Details = request.Reason ?? string.Empty });
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { listing }, HttpContext.TraceIdentifier));
    }



    [HttpGet("listing-review")]
    [HttpGet("listings/review")]
    public async Task<ActionResult<ApiResponse<object>>> ListingReview([FromQuery] string? status, [FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : pageSize > 100 ? 100 : pageSize;
        var query = db.Listings.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) && status != "All")
            query = query.Where(x => x.Status == status || x.ModerationStatus == status);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(x => x.Title.ToLower().Contains(term) || x.Description.ToLower().Contains(term) || x.City.ToLower().Contains(term));
        }

        var total = await query.CountAsync(ct);
        var listings = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var listingIds = listings.Select(x => x.Id).ToList();
        var sellerIds = listings.Select(x => x.SellerId).Distinct().ToList();
        var categoryIds = listings.Select(x => x.CategoryId).Distinct().ToList();

        var sellers = await db.UserProfiles.AsNoTracking()
            .Where(x => sellerIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Name, x.Email, x.Status })
            .ToDictionaryAsync(x => x.Id, ct);
        var categories = await db.Categories.AsNoTracking()
            .Where(x => categoryIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Name, x.Code })
            .ToDictionaryAsync(x => x.Id, ct);
        var mediaRows = await db.MediaAssets.AsNoTracking()
            .Where(x => x.ListingId != null && listingIds.Contains(x.ListingId.Value) && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new { ListingId = x.ListingId!.Value, x.Url })
            .ToListAsync(ct);
        var images = mediaRows
            .GroupBy(x => x.ListingId)
            .ToDictionary(g => g.Key, g => new { ListingId = g.Key, Url = g.First().Url });

        var items = listings.Select(x =>
        {
            sellers.TryGetValue(x.SellerId, out var seller);
            categories.TryGetValue(x.CategoryId, out var category);
            images.TryGetValue(x.Id, out var image);
            return new
            {
                x.Id,
                x.Title,
                x.Price,
                x.Currency,
                x.Status,
                x.ModerationStatus,
                x.ModerationReason,
                x.City,
                x.State,
                x.CreatedAt,
                x.PublishedAt,
                x.ExpiresAt,
                Seller = seller,
                Category = category,
                ImageUrl = image?.Url ?? "",
                x.ViewCount,
                x.FavoriteCount
            };
        });

        return Ok(ApiResponse<object>.Ok(new { items, total, page, pageSize }, HttpContext.TraceIdentifier));
    }

    [HttpGet("ads")]
    public async Task<ActionResult<ApiResponse<object>>> Ads([FromQuery] string? placement, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : pageSize > 100 ? 100 : pageSize;

        var creativesQuery = db.AdCreatives.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(placement) && placement != "All") creativesQuery = creativesQuery.Where(x => x.Placement == placement);
        if (!string.IsNullOrWhiteSpace(status) && status != "All") creativesQuery = creativesQuery.Where(x => x.Status == status);

        var total = await creativesQuery.CountAsync(ct);
        var creatives = await creativesQuery
            .OrderBy(x => x.Placement)
            .ThenBy(x => x.SortOrder)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var campaignIds = creatives.Select(x => x.CampaignId).Distinct().ToList();
        var campaigns = await db.AdCampaigns.AsNoTracking()
            .Where(x => campaignIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Name, x.Status, x.StartDate, x.EndDate, x.Priority, x.Budget })
            .ToDictionaryAsync(x => x.Id, ct);

        var now = DateTimeOffset.UtcNow;
        var stats = new
        {
            Total = await db.AdCreatives.CountAsync(ct),
            Active = await db.AdCreatives.CountAsync(x => x.Status == "Active", ct),
            Pending = await db.AdCreatives.CountAsync(x => x.Status == "Pending", ct),
            Expired = await db.AdCampaigns.CountAsync(x => x.EndDate < now, ct),
            Impressions = await db.AdCreatives.SumAsync(x => (int?)x.CurrentImpressions, ct) ?? 0,
            Clicks = await db.AdCreatives.SumAsync(x => (int?)x.CurrentClicks, ct) ?? 0
        };

        var items = creatives.Select(x =>
        {
            campaigns.TryGetValue(x.CampaignId, out var campaign);
            return new
            {
                x.Id,
                x.CampaignId,
                Campaign = campaign,
                x.Placement,
                x.Title,
                x.Description,
                ImageUrl = string.IsNullOrWhiteSpace(x.DesktopImageUrl) ? x.MobileImageUrl : x.DesktopImageUrl,
                x.DesktopImageUrl,
                x.MobileImageUrl,
                x.TargetUrl,
                x.OpenInNewTab,
                x.Status,
                x.SortOrder,
                x.CurrentImpressions,
                x.CurrentClicks,
                ExpiresAt = campaign?.EndDate,
                EndDate = campaign?.EndDate,
                StartDate = campaign?.StartDate,
                x.MaxImpressions,
                x.MaxClicks
            };
        });

        return Ok(ApiResponse<object>.Ok(new { stats, items, total, page, pageSize }, HttpContext.TraceIdentifier));
    }

    [HttpGet("ad-placements")]
    public async Task<ActionResult<ApiResponse<object>>> AdPlacements([FromQuery] string? q, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : pageSize > 100 ? 100 : pageSize;

        var query = db.AdPlacements.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(x => x.Code.ToLower().Contains(term) || x.Name.ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            var isActive = status.Equals("Active", StringComparison.OrdinalIgnoreCase);
            query = query.Where(x => x.IsActive == isActive);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(x => x.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.InsertEvery,
                x.IsActive,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Ok(new { items, total, page, pageSize }, HttpContext.TraceIdentifier));
    }

    [HttpPost("ads")]
    public async Task<ActionResult<ApiResponse<object>>> CreateAd(CreateAdRequest request, CancellationToken ct)
    {
        var title = (request.Title ?? string.Empty).Trim();
        var placement = NormalizeAdPlacement(request.Placement);
        if (string.IsNullOrWhiteSpace(title))
            return BadRequest(ApiResponse<object>.Fail("Validation", "Title is required", HttpContext.TraceIdentifier));
        if (string.IsNullOrWhiteSpace(placement))
            return BadRequest(ApiResponse<object>.Fail("Validation", "Placement is required", HttpContext.TraceIdentifier));

        var campaign = await db.AdCampaigns.FirstOrDefaultAsync(x => x.Name == (request.CampaignName ?? "Admin Ads"), ct);
        var requestedEndDate = request.ResolvedEndDate;
        if (campaign is null)
        {
            campaign = new OpenMarketplace.Domain.Advertising.AdCampaign
            {
                Name = string.IsNullOrWhiteSpace(request.CampaignName) ? "Admin Ads" : request.CampaignName.Trim(),
                Status = "Active",
                StartDate = DateTimeOffset.UtcNow.AddDays(-1),
                EndDate = requestedEndDate ?? DateTimeOffset.UtcNow.AddYears(1),
                Priority = 100
            };
            db.AdCampaigns.Add(campaign);
            await db.SaveChangesAsync(ct);
        }
        else if (requestedEndDate.HasValue)
        {
            campaign.EndDate = requestedEndDate.Value;
            campaign.UpdatedAt = DateTimeOffset.UtcNow;
        }

        var creative = new OpenMarketplace.Domain.Advertising.AdCreative
        {
            CampaignId = campaign.Id,
            Placement = placement,
            Title = title,
            Description = request.Description?.Trim() ?? string.Empty,
            TargetUrl = request.TargetUrl?.Trim() ?? string.Empty,
            DesktopImageUrl = request.PrimaryImageUrl,
            MobileImageUrl = request.PrimaryImageUrl,
            OpenInNewTab = request.OpenInNewTab ?? ((request.TargetUrl ?? string.Empty).StartsWith("http", StringComparison.OrdinalIgnoreCase)),
            SortOrder = request.SortOrder ?? 10,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim(),
            MaxImpressions = request.MaxImpressions ?? 0,
            MaxClicks = request.MaxClicks ?? 0
        };

        db.AdCreatives.Add(creative);
        db.AuditLogs.Add(new AuditLog { ActorId = request.AdminId, Action = "Ad created", EntityType = "AdCreative", EntityId = creative.Id });
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { creative }, HttpContext.TraceIdentifier));
    }

    [HttpPost("ads/{id:guid}/moderate")]
    public async Task<ActionResult<ApiResponse<object>>> ModerateAd(Guid id, ModerateAdRequest request, CancellationToken ct)
    {
        var creative = await db.AdCreatives.FindAsync([id], ct);
        if (creative is null) return NotFound(ApiResponse<object>.Fail("NotFound", "Ad creative not found", HttpContext.TraceIdentifier));
        creative.Status = string.IsNullOrWhiteSpace(request.Status) ? creative.Status : request.Status.Trim();
        creative.SortOrder = request.SortOrder ?? creative.SortOrder;
        creative.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { ActorId = request.AdminId, Action = $"Ad {creative.Status}", EntityType = "AdCreative", EntityId = id, Details = request.Reason ?? "" });
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { creative }, HttpContext.TraceIdentifier));
    }

    [HttpPost("ads/{id:guid}/update")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateAd(Guid id, UpdateAdRequest request, CancellationToken ct)
    {
        var creative = await db.AdCreatives.FindAsync([id], ct);
        if (creative is null) return NotFound(ApiResponse<object>.Fail("NotFound", "Ad creative not found", HttpContext.TraceIdentifier));
        creative.Title = request.Title ?? creative.Title;
        creative.Description = request.Description ?? creative.Description;
        creative.TargetUrl = request.TargetUrl ?? creative.TargetUrl;
        var imageUrl = request.PrimaryImageUrl;
        if (request.HasImageUrl)
        {
            creative.DesktopImageUrl = imageUrl;
            creative.MobileImageUrl = imageUrl;
        }
        creative.Placement = string.IsNullOrWhiteSpace(request.Placement) ? creative.Placement : NormalizeAdPlacement(request.Placement);
        creative.OpenInNewTab = request.OpenInNewTab ?? creative.OpenInNewTab;
        creative.Status = request.Status ?? creative.Status;
        creative.SortOrder = request.SortOrder ?? creative.SortOrder;
        creative.MaxImpressions = request.MaxImpressions ?? creative.MaxImpressions;
        creative.MaxClicks = request.MaxClicks ?? creative.MaxClicks;
        var campaign = await db.AdCampaigns.FirstOrDefaultAsync(x => x.Id == creative.CampaignId, ct);
        if (campaign is not null)
        {
            var requestedEndDate = request.ResolvedEndDate;
            if (requestedEndDate.HasValue) campaign.EndDate = requestedEndDate.Value;
            if (!string.IsNullOrWhiteSpace(request.CampaignName)) campaign.Name = request.CampaignName.Trim();
            campaign.UpdatedAt = DateTimeOffset.UtcNow;
        }
        creative.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { ActorId = request.AdminId, Action = "Ad updated", EntityType = "AdCreative", EntityId = id });
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { creative }, HttpContext.TraceIdentifier));
    }

    [HttpPost("ad-placements/{id:guid}/toggle")]
    public async Task<ActionResult<ApiResponse<object>>> ToggleAdPlacement(Guid id, TogglePlacementRequest request, CancellationToken ct)
    {
        var placement = await db.AdPlacements.FindAsync([id], ct);
        if (placement is null) return NotFound(ApiResponse<object>.Fail("NotFound", "Ad placement not found", HttpContext.TraceIdentifier));
        placement.IsActive = request.IsActive;
        placement.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { ActorId = request.AdminId, Action = request.IsActive ? "Placement enabled" : "Placement disabled", EntityType = "AdPlacement", EntityId = id });
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { placement }, HttpContext.TraceIdentifier));
    }


    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<object>>> AdminCategories([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : pageSize > 100 ? 100 : pageSize;

        var query = db.Categories.AsNoTracking().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(term) || x.Code.ToLower().Contains(term) || x.Slug.ToLower().Contains(term) || x.IconKey.ToLower().Contains(term));
        }

        var total = await query.CountAsync(ct);
        var categories = await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        var categoryIds = categories.Select(x => x.Id).ToList();
        var counts = await db.Listings.AsNoTracking()
            .Where(x => categoryIds.Contains(x.CategoryId) && !x.IsDeleted)
            .GroupBy(x => x.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count, ct);

        var items = categories.Select(x => new
        {
            x.Id,
            x.Code,
            x.ParentCode,
            x.IconKey,
            x.Name,
            x.Slug,
            Description = string.Empty,
            x.SortOrder,
            x.IsActive,
            Status = x.IsActive ? "Active" : "Inactive",
            Count = counts.TryGetValue(x.Id, out var count) ? count : 0,
            x.CreatedAt,
            x.UpdatedAt
        });

        return Ok(ApiResponse<object>.Ok(new { items, total, page, pageSize }, HttpContext.TraceIdentifier));
    }

    [HttpPost("categories")]
    public async Task<ActionResult<ApiResponse<object>>> CreateCategory(AdminCategoryRequest request, CancellationToken ct)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name)) return BadRequest(ApiResponse<object>.Fail("Validation", "Category name is required", HttpContext.TraceIdentifier));

        var slug = NormalizeCategorySlug(string.IsNullOrWhiteSpace(request.Slug) ? name : request.Slug);
        var code = NormalizeCategoryCode(string.IsNullOrWhiteSpace(request.Code) ? slug : request.Code);
        if (await db.Categories.AnyAsync(x => !x.IsDeleted && (x.Code == code || x.Slug == slug), ct))
            return BadRequest(ApiResponse<object>.Fail("Duplicate", "Category code or slug already exists", HttpContext.TraceIdentifier));

        var category = new Category
        {
            Name = name,
            Slug = slug,
            Code = code,
            ParentCode = string.IsNullOrWhiteSpace(request.ParentCode) ? null : NormalizeCategoryCode(request.ParentCode),
            IconKey = string.IsNullOrWhiteSpace(request.IconKey) ? "category" : request.IconKey.Trim(),
            SortOrder = request.SortOrder ?? await NextCategorySortOrder(ct),
            IsActive = request.IsActive ?? !string.Equals(request.Status, "Inactive", StringComparison.OrdinalIgnoreCase)
        };

        db.Categories.Add(category);
        db.AuditLogs.Add(new AuditLog { ActorId = request.AdminId, Action = "Category created", EntityType = "Category", EntityId = category.Id, Details = category.Code });
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { category }, HttpContext.TraceIdentifier));
    }

    [HttpPost("categories/{id:guid}/update")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateCategory(Guid id, AdminCategoryRequest request, CancellationToken ct)
    {
        var category = await db.Categories.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (category is null) return NotFound(ApiResponse<object>.Fail("NotFound", "Category not found", HttpContext.TraceIdentifier));

        var name = (request.Name ?? category.Name).Trim();
        if (string.IsNullOrWhiteSpace(name)) return BadRequest(ApiResponse<object>.Fail("Validation", "Category name is required", HttpContext.TraceIdentifier));

        var slug = request.Slug is null ? category.Slug : NormalizeCategorySlug(string.IsNullOrWhiteSpace(request.Slug) ? name : request.Slug);
        var code = request.Code is null ? category.Code : NormalizeCategoryCode(string.IsNullOrWhiteSpace(request.Code) ? slug : request.Code);
        if (await db.Categories.AnyAsync(x => x.Id != id && !x.IsDeleted && (x.Code == code || x.Slug == slug), ct))
            return BadRequest(ApiResponse<object>.Fail("Duplicate", "Category code or slug already exists", HttpContext.TraceIdentifier));

        category.Name = name;
        category.Slug = slug;
        category.Code = code;
        if (request.ParentCode is not null) category.ParentCode = string.IsNullOrWhiteSpace(request.ParentCode) ? null : NormalizeCategoryCode(request.ParentCode);
        if (request.IconKey is not null) category.IconKey = string.IsNullOrWhiteSpace(request.IconKey) ? "category" : request.IconKey.Trim();
        if (request.SortOrder.HasValue) category.SortOrder = request.SortOrder.Value;
        if (request.IsActive.HasValue) category.IsActive = request.IsActive.Value;
        else if (!string.IsNullOrWhiteSpace(request.Status)) category.IsActive = !string.Equals(request.Status, "Inactive", StringComparison.OrdinalIgnoreCase);
        category.UpdatedAt = DateTimeOffset.UtcNow;

        db.AuditLogs.Add(new AuditLog { ActorId = request.AdminId, Action = "Category updated", EntityType = "Category", EntityId = id, Details = category.Code });
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { category }, HttpContext.TraceIdentifier));
    }

    [HttpPost("categories/{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateCategoryStatus(Guid id, CategoryStatusRequest request, CancellationToken ct)
    {
        var category = await db.Categories.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (category is null) return NotFound(ApiResponse<object>.Fail("NotFound", "Category not found", HttpContext.TraceIdentifier));
        category.IsActive = request.IsActive ?? !string.Equals(request.Status, "Inactive", StringComparison.OrdinalIgnoreCase);
        category.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { ActorId = request.AdminId, Action = category.IsActive ? "Category enabled" : "Category disabled", EntityType = "Category", EntityId = id });
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { category }, HttpContext.TraceIdentifier));
    }


    [HttpGet("packages")]
    public async Task<ActionResult<ApiResponse<object>>> AdminPackages([FromQuery] string? q, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : pageSize > 100 ? 100 : pageSize;

        var query = db.Packages.AsNoTracking().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(term) || x.Code.ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            var isActive = status.Equals("Active", StringComparison.OrdinalIgnoreCase);
            query = query.Where(x => x.IsActive == isActive);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Price)
            .ThenBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.Price,
                x.Currency,
                x.DurationDays,
                x.SortOrder,
                x.IsActive,
                Status = x.IsActive ? "Active" : "Inactive",
                Description = string.Empty,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Ok(new { items, total, page, pageSize }, HttpContext.TraceIdentifier));
    }

    [HttpPost("packages")]
    public async Task<ActionResult<ApiResponse<object>>> CreatePackage(AdminPackageRequest request, CancellationToken ct)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name)) return BadRequest(ApiResponse<object>.Fail("Validation", "Package name is required", HttpContext.TraceIdentifier));

        var code = NormalizePackageCode(string.IsNullOrWhiteSpace(request.Code) ? name : request.Code);
        if (await db.Packages.AnyAsync(x => !x.IsDeleted && x.Code == code, ct))
            return BadRequest(ApiResponse<object>.Fail("Duplicate", "Package code already exists", HttpContext.TraceIdentifier));

        var package = new Package
        {
            Code = code,
            Name = name,
            Price = request.Price ?? 0,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency.Trim().ToUpperInvariant(),
            DurationDays = request.DurationDays is > 0 ? request.DurationDays.Value : 30,
            SortOrder = request.SortOrder ?? 0,
            IsActive = request.IsActive ?? !string.Equals(request.Status, "Inactive", StringComparison.OrdinalIgnoreCase)
        };

        db.Packages.Add(package);
        db.AuditLogs.Add(new AuditLog { ActorId = request.AdminId, Action = "Package created", EntityType = "Package", EntityId = package.Id, Details = package.Code });
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { package }, HttpContext.TraceIdentifier));
    }

    [HttpPost("packages/{id:guid}/update")]
    public async Task<ActionResult<ApiResponse<object>>> UpdatePackage(Guid id, AdminPackageRequest request, CancellationToken ct)
    {
        var package = await db.Packages.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (package is null) return NotFound(ApiResponse<object>.Fail("NotFound", "Package not found", HttpContext.TraceIdentifier));

        var name = (request.Name ?? package.Name).Trim();
        if (string.IsNullOrWhiteSpace(name)) return BadRequest(ApiResponse<object>.Fail("Validation", "Package name is required", HttpContext.TraceIdentifier));

        var code = request.Code is null ? package.Code : NormalizePackageCode(string.IsNullOrWhiteSpace(request.Code) ? name : request.Code);
        if (await db.Packages.AnyAsync(x => x.Id != id && !x.IsDeleted && x.Code == code, ct))
            return BadRequest(ApiResponse<object>.Fail("Duplicate", "Package code already exists", HttpContext.TraceIdentifier));

        package.Name = name;
        package.Code = code;
        if (request.Price.HasValue) package.Price = request.Price.Value;
        if (!string.IsNullOrWhiteSpace(request.Currency)) package.Currency = request.Currency.Trim().ToUpperInvariant();
        if (request.DurationDays is > 0) package.DurationDays = request.DurationDays.Value;
        if (request.SortOrder.HasValue) package.SortOrder = request.SortOrder.Value;
        if (request.IsActive.HasValue) package.IsActive = request.IsActive.Value;
        else if (!string.IsNullOrWhiteSpace(request.Status)) package.IsActive = !string.Equals(request.Status, "Inactive", StringComparison.OrdinalIgnoreCase);
        package.UpdatedAt = DateTimeOffset.UtcNow;

        db.AuditLogs.Add(new AuditLog { ActorId = request.AdminId, Action = "Package updated", EntityType = "Package", EntityId = id, Details = package.Code });
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { package }, HttpContext.TraceIdentifier));
    }

    [HttpPost("packages/{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<object>>> UpdatePackageStatus(Guid id, PackageStatusRequest request, CancellationToken ct)
    {
        var package = await db.Packages.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (package is null) return NotFound(ApiResponse<object>.Fail("NotFound", "Package not found", HttpContext.TraceIdentifier));
        package.IsActive = request.IsActive ?? !string.Equals(request.Status, "Inactive", StringComparison.OrdinalIgnoreCase);
        package.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { ActorId = request.AdminId, Action = package.IsActive ? "Package enabled" : "Package disabled", EntityType = "Package", EntityId = id });
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { package }, HttpContext.TraceIdentifier));
    }

    [HttpGet("users")]
    public async Task<ActionResult<ApiResponse<object>>> Users([FromQuery] string? role, [FromQuery] string? source, [FromQuery] string? status, [FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : pageSize > 100 ? 100 : pageSize;
        var query = db.UserProfiles.AsNoTracking().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(role) && role != "All") query = query.Where(x => x.Role == role);
        if (!string.IsNullOrWhiteSpace(source) && source != "All") query = query.Where(x => x.Source == source);
        if (!string.IsNullOrWhiteSpace(status) && status != "All") query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(term) || x.Email.ToLower().Contains(term) || x.Phone.ToLower().Contains(term));
        }
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new { x.Id, x.Name, x.Email, x.Phone, x.Location, x.AvatarUrl, x.Role, x.Source, x.Status, x.EmailVerified, x.PhoneVerified, x.IdVerified, x.BusinessVerified, x.TrustScore, x.Rating, x.ReviewCount, x.CreatedAt })
            .ToListAsync(ct);
        var stats = new
        {
            Total = await db.UserProfiles.CountAsync(x => !x.IsDeleted, ct),
            WebCustomer = await db.UserProfiles.CountAsync(x => !x.IsDeleted && x.Source == "WebCustomer", ct),
            AdminCreated = await db.UserProfiles.CountAsync(x => !x.IsDeleted && x.Source == "AdminCreated", ct),
            SystemManaged = await db.UserProfiles.CountAsync(x => !x.IsDeleted && x.Source == "SystemManaged", ct),
            Admins = await db.UserProfiles.CountAsync(x => !x.IsDeleted && (x.Role == "Admin" || x.Role == "SuperAdmin"), ct)
        };
        return Ok(ApiResponse<object>.Ok(new { stats, items, total, page, pageSize }, HttpContext.TraceIdentifier));
    }


    [HttpPost("users")]
    [HttpPost("users/create")]
    public async Task<ActionResult<ApiResponse<object>>> CreateUser(AdminUserRequest request, CancellationToken ct)
    {
        var name = (request.Name ?? string.Empty).Trim();
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(name)) return BadRequest(ApiResponse<object>.Fail("Validation", "Name is required", HttpContext.TraceIdentifier));
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) return BadRequest(ApiResponse<object>.Fail("Validation", "Valid email is required", HttpContext.TraceIdentifier));
        if (await db.UserProfiles.AnyAsync(x => !x.IsDeleted && x.Email == email, ct)) return Conflict(ApiResponse<object>.Fail("EmailExists", "Email already exists", HttpContext.TraceIdentifier));

        var role = string.IsNullOrWhiteSpace(request.Role) ? "Customer" : request.Role.Trim();
        var source = string.IsNullOrWhiteSpace(request.Source) ? "AdminCreated" : request.Source.Trim();
        if (!IsAllowedRole(role)) return BadRequest(ApiResponse<object>.Fail("Validation", "Invalid role", HttpContext.TraceIdentifier));
        if (!IsAllowedSource(source)) return BadRequest(ApiResponse<object>.Fail("Validation", "Invalid source", HttpContext.TraceIdentifier));

        var user = new UserProfile
        {
            Name = name,
            Email = email,
            Phone = request.Phone?.Trim() ?? string.Empty,
            Location = request.Location?.Trim() ?? string.Empty,
            AvatarUrl = request.AvatarUrl?.Trim() ?? string.Empty,
            Role = role,
            Source = source,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim(),
            PasswordHash = string.IsNullOrWhiteSpace(request.Password) ? string.Empty : AuthController.HashPassword(request.Password),
            TrustScore = request.TrustScore ?? 50,
            EmailVerified = request.EmailVerified ?? false,
            PhoneVerified = request.PhoneVerified ?? false,
            IdVerified = request.IdVerified ?? false,
            BusinessVerified = request.BusinessVerified ?? false
        };
        db.UserProfiles.Add(user);
        db.AuditLogs.Add(new AuditLog { ActorId = request.AdminId, Action = "User created", EntityType = "UserProfile", EntityId = user.Id });
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { user }, HttpContext.TraceIdentifier));
    }

    [HttpPost("users/{id:guid}/update")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateUser(Guid id, AdminUserRequest request, CancellationToken ct)
    {
        var user = await db.UserProfiles.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (user is null) return NotFound(ApiResponse<object>.Fail("NotFound", "User not found", HttpContext.TraceIdentifier));
        if (!string.IsNullOrWhiteSpace(request.Name)) user.Name = request.Name.Trim();
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = request.Email.Trim().ToLowerInvariant();
            if (!email.Contains('@')) return BadRequest(ApiResponse<object>.Fail("Validation", "Valid email is required", HttpContext.TraceIdentifier));
            if (await db.UserProfiles.AnyAsync(x => !x.IsDeleted && x.Email == email && x.Id != id, ct)) return Conflict(ApiResponse<object>.Fail("EmailExists", "Email already exists", HttpContext.TraceIdentifier));
            user.Email = email;
        }
        if (request.Phone is not null) user.Phone = request.Phone.Trim();
        if (request.Location is not null) user.Location = request.Location.Trim();
        if (request.AvatarUrl is not null) user.AvatarUrl = request.AvatarUrl.Trim();
        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            var role = request.Role.Trim();
            if (!IsAllowedRole(role)) return BadRequest(ApiResponse<object>.Fail("Validation", "Invalid role", HttpContext.TraceIdentifier));
            user.Role = role;
        }
        if (!string.IsNullOrWhiteSpace(request.Source))
        {
            var source = request.Source.Trim();
            if (!IsAllowedSource(source)) return BadRequest(ApiResponse<object>.Fail("Validation", "Invalid source", HttpContext.TraceIdentifier));
            user.Source = source;
        }
        if (!string.IsNullOrWhiteSpace(request.Status)) user.Status = request.Status.Trim();
        if (!string.IsNullOrWhiteSpace(request.Password)) user.PasswordHash = AuthController.HashPassword(request.Password);
        user.EmailVerified = request.EmailVerified ?? user.EmailVerified;
        user.PhoneVerified = request.PhoneVerified ?? user.PhoneVerified;
        user.IdVerified = request.IdVerified ?? user.IdVerified;
        user.BusinessVerified = request.BusinessVerified ?? user.BusinessVerified;
        user.TrustScore = request.TrustScore ?? user.TrustScore;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { ActorId = request.AdminId, Action = "User updated", EntityType = "UserProfile", EntityId = id });
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { user }, HttpContext.TraceIdentifier));
    }

    [HttpPost("users/{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateUserStatus(Guid id, UserStatusRequest request, CancellationToken ct)
    {
        var user = await db.UserProfiles.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (user is null) return NotFound(ApiResponse<object>.Fail("NotFound", "User not found", HttpContext.TraceIdentifier));
        user.Status = string.IsNullOrWhiteSpace(request.Status) ? (request.IsActive == false ? "Inactive" : "Active") : request.Status.Trim();
        user.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { ActorId = request.AdminId, Action = $"User status changed to {user.Status}", EntityType = "UserProfile", EntityId = id });
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { user }, HttpContext.TraceIdentifier));
    }


    [HttpPost("users/{id:guid}/role-source")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateUserRoleSource(Guid id, UpdateUserRoleSourceRequest request, CancellationToken ct)
    {
        var user = await db.UserProfiles.FindAsync([id], ct);
        if (user is null) return NotFound(ApiResponse<object>.Fail("NotFound", "User not found", HttpContext.TraceIdentifier));
        if (!IsAllowedRole(request.Role)) return BadRequest(ApiResponse<object>.Fail("Validation", "Invalid role", HttpContext.TraceIdentifier));
        if (!IsAllowedSource(request.Source)) return BadRequest(ApiResponse<object>.Fail("Validation", "Invalid source", HttpContext.TraceIdentifier));
        user.Role = request.Role;
        user.Source = request.Source;
        if (!string.IsNullOrWhiteSpace(request.Status)) user.Status = request.Status.Trim();
        user.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { ActorId = request.AdminId, Action = "User role/source updated", EntityType = "UserProfile", EntityId = id, Details = $"Role={user.Role}; Source={user.Source}; Status={user.Status}" });
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { user }, HttpContext.TraceIdentifier));
    }


    private async Task<int> NextCategorySortOrder(CancellationToken ct)
    {
        var max = await db.Categories.Where(x => !x.IsDeleted).Select(x => (int?)x.SortOrder).MaxAsync(ct) ?? 0;
        return max + 10;
    }

    private static string NormalizeCategoryCode(string? value)
    {
        var raw = string.IsNullOrWhiteSpace(value) ? "CATEGORY" : value.Trim();
        var chars = raw.Select(ch => char.IsLetterOrDigit(ch) ? char.ToUpperInvariant(ch) : '_').ToArray();
        var code = string.Join("_", new string(chars).Split('_', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(code) ? "CATEGORY" : code;
    }

    private static string NormalizeCategorySlug(string? value)
    {
        var raw = string.IsNullOrWhiteSpace(value) ? "category" : value.Trim().ToLowerInvariant();
        var chars = raw.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray();
        var slug = string.Join("-", new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(slug) ? "category" : slug;
    }


    private static string NormalizePackageCode(string? value)
    {
        var raw = string.IsNullOrWhiteSpace(value) ? "PACKAGE" : value.Trim();
        var chars = raw.Select(ch => char.IsLetterOrDigit(ch) ? char.ToUpperInvariant(ch) : '_').ToArray();
        var code = string.Join("_", new string(chars).Split('_', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(code) ? "PACKAGE" : code;
    }

    private static string NormalizeAdPlacement(string? placement)
    {
        if (string.IsNullOrWhiteSpace(placement) || placement == "All") return string.Empty;
        var value = placement.Trim().Replace("-", "_").Replace(" ", "_").ToUpperInvariant();
        return value switch
        {
            "HOMEHERO" or "HERO_SEARCH" or "HEROSEARCH" or "SEARCH_HERO" or "SEARCHHERO" => "HOME_HERO",
            "HOMEFEED" => "HOME_FEED",
            "LISTINGDETAIL" or "DETAIL" => "LISTING_DETAIL",
            "SELLERPROFILE" => "SELLER_PROFILE",
            _ => value
        };
    }

    private static bool IsAllowedRole(string role) => role is "Customer" or "Seller" or "Admin" or "SuperAdmin" or "System";
    private static bool IsAllowedSource(string source) => source is "WebCustomer" or "AdminCreated" or "SystemManaged" or "Imported";

    [HttpPost("users/{id:guid}/badge")]
    public async Task<ActionResult<ApiResponse<object>>> Badge(Guid id, BadgeRequest request, CancellationToken ct)
    {
        var user = await db.UserProfiles.FindAsync([id],ct);
        if(user is null) return NotFound(ApiResponse<object>.Fail("NotFound","User not found",HttpContext.TraceIdentifier));
        user.EmailVerified=request.EmailVerified; user.PhoneVerified=request.PhoneVerified; user.IdVerified=request.IdVerified; user.BusinessVerified=request.BusinessVerified; user.TrustScore=request.TrustScore;
        db.AuditLogs.Add(new AuditLog{ActorId=request.AdminId,Action="Trust badges updated",EntityType="UserProfile",EntityId=id});
        await db.SaveChangesAsync(ct); return Ok(ApiResponse<object>.Ok(new{user},HttpContext.TraceIdentifier));
    }

    [HttpGet("reports")]
    public async Task<ActionResult<ApiResponse<object>>> Reports([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : pageSize > 100 ? 100 : pageSize;
        var query = db.Reports.AsNoTracking();
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x=>x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(new{items,total,page,pageSize},HttpContext.TraceIdentifier));
    }

    [HttpGet("reports/overview")]
    public async Task<ActionResult<ApiResponse<object>>> ReportsOverview(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var from = new DateTimeOffset(now.UtcDateTime.Date.AddDays(-13), TimeSpan.Zero);
        var listings = await db.Listings.AsNoTracking().Where(x => x.CreatedAt >= from).ToListAsync(ct);
        var users = await db.UserProfiles.AsNoTracking().Where(x => x.CreatedAt >= from && !x.IsDeleted).ToListAsync(ct);
        var payments = await db.Payments.AsNoTracking().Where(x => x.CreatedAt >= from).ToListAsync(ct);
        var reports = await db.Reports.AsNoTracking().Where(x => x.CreatedAt >= from).ToListAsync(ct);
        var trends = Enumerable.Range(0, 14).Select(i =>
        {
            var day = from.AddDays(i);
            var next = day.AddDays(1);
            return new
            {
                Date = day.ToString("yyyy-MM-dd"),
                Listings = listings.Count(x => x.CreatedAt >= day && x.CreatedAt < next),
                Users = users.Count(x => x.CreatedAt >= day && x.CreatedAt < next),
                Revenue = payments.Where(x => x.CreatedAt >= day && x.CreatedAt < next && x.Status == "Succeeded").Sum(x => x.Amount),
                Reports = reports.Count(x => x.CreatedAt >= day && x.CreatedAt < next)
            };
        }).ToList();

        var stats = new
        {
            TotalListings = await db.Listings.CountAsync(ct),
            ActiveListings = await db.Listings.CountAsync(x => x.Status == "Published" || x.Status == "Active", ct),
            TotalUsers = await db.UserProfiles.CountAsync(x => !x.IsDeleted, ct),
            OpenReports = await db.Reports.CountAsync(x => x.Status == "Open", ct),
            Revenue = await db.Payments.Where(x => x.Status == "Succeeded").SumAsync(x => (decimal?)x.Amount, ct) ?? 0
        };
        return Ok(ApiResponse<object>.Ok(new { stats, trends }, HttpContext.TraceIdentifier));
    }

    [HttpPost("reports/{id:guid}/resolve")]
    public async Task<ActionResult<ApiResponse<object>>> ResolveReport(Guid id, ResolveReportRequest request, CancellationToken ct)
    { var report=await db.Reports.FindAsync([id],ct); if(report is not null){report.Status=request.Status; db.AuditLogs.Add(new AuditLog{ActorId=request.AdminId,Action="Report resolved",EntityType="Report",EntityId=id,Details=request.Status}); await db.SaveChangesAsync(ct);} return Ok(ApiResponse<object>.Ok(new{report},HttpContext.TraceIdentifier)); }

    [HttpGet("orders")]
    public async Task<ActionResult<ApiResponse<object>>> Orders([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page; pageSize = pageSize < 1 ? 10 : pageSize > 100 ? 100 : pageSize;
        var query = db.Orders.AsNoTracking(); var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x=>x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(new{items,total,page,pageSize},HttpContext.TraceIdentifier));
    }
    [HttpGet("payments")]
    public async Task<ActionResult<ApiResponse<object>>> Payments([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 25 : pageSize > 100 ? 100 : pageSize;
        var query = db.Payments.AsNoTracking();
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { items, total, page, pageSize }, HttpContext.TraceIdentifier));
    }

    [HttpGet("payments/{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> PaymentDetail(Guid id, CancellationToken ct)
    {
        var payment = await db.Payments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (payment is null) return NotFound(ApiResponse<object>.Fail("NotFound", "Payment not found", HttpContext.TraceIdentifier));
        return Ok(ApiResponse<object>.Ok(new { payment }, HttpContext.TraceIdentifier));
    }
    [HttpGet("invoices")]
    public async Task<ActionResult<ApiResponse<object>>> Invoices([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page; pageSize = pageSize < 1 ? 10 : pageSize > 100 ? 100 : pageSize;
        var query = db.Invoices.AsNoTracking(); var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x=>x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(new{items,total,page,pageSize},HttpContext.TraceIdentifier));
    }


    [HttpGet("messages/conversations")]
    public async Task<ActionResult<ApiResponse<object>>> MessageConversations([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : pageSize > 100 ? 100 : pageSize;
        var query = db.Conversations.AsNoTracking().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(x => x.Subject.ToLower().Contains(term) || x.LastMessagePreview.ToLower().Contains(term));
        }

        var total = await query.CountAsync(ct);
        var conversations = await query.OrderByDescending(x => x.LastMessageAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var conversationIds = conversations.Select(x => x.Id).ToList();
        var listingIds = conversations.Select(x => x.ListingId).Distinct().ToList();
        var userIds = conversations.SelectMany(x => new[] { x.BuyerId, x.SellerId }).Distinct().ToList();

        var listings = await db.Listings.AsNoTracking()
            .Where(x => listingIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Title, x.Price, x.Currency, x.Status })
            .ToDictionaryAsync(x => x.Id, ct);

        var users = await db.UserProfiles.AsNoTracking()
            .Where(x => userIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Name, x.Email, x.Status })
            .ToDictionaryAsync(x => x.Id, ct);

        var counts = await db.Messages.AsNoTracking()
            .Where(x => conversationIds.Contains(x.ConversationId) && !x.IsDeleted)
            .GroupBy(x => x.ConversationId)
            .Select(g => new { ConversationId = g.Key, Count = g.Count(), Flagged = g.Count(x => x.ModerationStatus != "Allowed") })
            .ToDictionaryAsync(x => x.ConversationId, ct);

        var items = conversations.Select(c =>
        {
            listings.TryGetValue(c.ListingId, out var listing);
            users.TryGetValue(c.BuyerId, out var buyer);
            users.TryGetValue(c.SellerId, out var seller);
            counts.TryGetValue(c.Id, out var count);
            return new
            {
                c.Id,
                c.ListingId,
                ListingTitle = listing?.Title ?? c.Subject,
                ListingStatus = listing?.Status ?? "",
                Buyer = buyer,
                Seller = seller,
                c.Status,
                c.LastMessageAt,
                c.LastMessagePreview,
                MessageCount = count?.Count ?? 0,
                FlaggedCount = count?.Flagged ?? 0
            };
        });

        return Ok(ApiResponse<object>.Ok(new { items, total, page, pageSize }, HttpContext.TraceIdentifier));
    }

    [HttpGet("messages/conversations/{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> MessageThread(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        var conversation = await db.Conversations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (conversation is null) return NotFound(ApiResponse<object>.Fail("NotFound", "Conversation not found", HttpContext.TraceIdentifier));

        var listing = await db.Listings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == conversation.ListingId, ct);
        var users = await db.UserProfiles.AsNoTracking()
            .Where(x => x.Id == conversation.BuyerId || x.Id == conversation.SellerId)
            .Select(x => new { x.Id, x.Name, x.Email, x.Status })
            .ToListAsync(ct);
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 25 : pageSize > 100 ? 100 : pageSize;
        var messageQuery = db.Messages.AsNoTracking().Where(x => x.ConversationId == id && !x.IsDeleted);
        var total = await messageQuery.CountAsync(ct);
        var messages = await messageQuery
            .OrderBy(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new { x.Id, x.SenderId, x.ReceiverId, x.MessageType, x.Body, x.IsRead, x.ReadAt, x.ModerationStatus, x.CreatedAt })
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Ok(new { conversation, listing, users, messages = new { items = messages, total, page, pageSize }, total, page, pageSize }, HttpContext.TraceIdentifier));
    }

    [HttpPost("messages/{id:guid}/moderate")]
    public async Task<ActionResult<ApiResponse<object>>> ModerateMessage(Guid id, ModerateMessageRequest request, CancellationToken ct)
    {
        var message = await db.Messages.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (message is null) return NotFound(ApiResponse<object>.Fail("NotFound", "Message not found", HttpContext.TraceIdentifier));

        message.ModerationStatus = request.Status;
        message.UpdatedAt = DateTimeOffset.UtcNow;
        if (request.HideMessage) message.IsDeleted = true;
        db.AuditLogs.Add(new AuditLog { ActorId = request.AdminId, Action = "Message moderated", EntityType = "Message", EntityId = id, Details = request.Status });
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { message }, HttpContext.TraceIdentifier));
    }

    [HttpGet("notifications")]
    public async Task<ActionResult<ApiResponse<object>>> AdminNotifications([FromQuery] Guid? userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : pageSize > 100 ? 100 : pageSize;
        var query = db.Notifications.AsNoTracking().Where(x => !x.IsDeleted);
        if (userId is not null) query = query.Where(x => x.UserId == userId);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { items, total, page, pageSize }, HttpContext.TraceIdentifier));
    }

    [HttpPost("notifications/send")]
    public async Task<ActionResult<ApiResponse<object>>> SendNotification(AdminSendNotificationRequest request, CancellationToken ct)
    {
        var title = (request.Title ?? string.Empty).Trim();
        var body = (request.Body ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
            return BadRequest(ApiResponse<object>.Fail("Validation", "Title and body are required", HttpContext.TraceIdentifier));

        var users = request.SendToAll
            ? await db.UserProfiles.AsNoTracking().Where(x => !x.IsDeleted && x.Status != "Banned").Select(x => x.Id).ToListAsync(ct)
            : request.UserIds.Where(x => x != Guid.Empty).Distinct().ToList();

        if (users.Count == 0) return BadRequest(ApiResponse<object>.Fail("Validation", "Select at least one user", HttpContext.TraceIdentifier));

        var notifications = users.Select(userId => new Notification
        {
            UserId = userId,
            Type = string.IsNullOrWhiteSpace(request.Type) ? "Admin" : request.Type.Trim(),
            Title = title,
            Body = body,
            Url = request.Url ?? "",
            EntityType = "AdminMessage",
            ImageUrl = request.ImageUrl ?? ""
        }).ToList();

        db.Notifications.AddRange(notifications);
        db.AuditLogs.Add(new AuditLog { ActorId = request.AdminId, Action = "Admin notification sent", EntityType = "Notification", Details = $"Sent to {notifications.Count} user(s): {title}" });
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { sent = notifications.Count }, HttpContext.TraceIdentifier));
    }

    [HttpGet("comments")]
    public async Task<ActionResult<ApiResponse<object>>> Comments([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page; pageSize = pageSize < 1 ? 10 : pageSize > 100 ? 100 : pageSize;
        var query = db.ListingComments.AsNoTracking(); var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x=>x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(new{items,total,page,pageSize},HttpContext.TraceIdentifier));
    }
    [HttpGet("reviews")]
    public async Task<ActionResult<ApiResponse<object>>> Reviews([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page; pageSize = pageSize < 1 ? 10 : pageSize > 100 ? 100 : pageSize;
        var query = db.ListingReviews.AsNoTracking(); var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x=>x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(new{items,total,page,pageSize},HttpContext.TraceIdentifier));
    }
    [HttpGet("audit-logs")]
    public async Task<ActionResult<ApiResponse<object>>> Audit([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page; pageSize = pageSize < 1 ? 10 : pageSize > 100 ? 100 : pageSize;
        var query = db.AuditLogs.AsNoTracking(); var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x=>x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(new{items,total,page,pageSize},HttpContext.TraceIdentifier));
    }
    [HttpGet("health")]
    public ActionResult<ApiResponse<object>> Health() => Ok(ApiResponse<object>.Ok(new{api="ok",time=DateTimeOffset.UtcNow,swagger="/swagger/index.html"},HttpContext.TraceIdentifier));
}
public sealed record AdminPackageRequest(Guid? AdminId, string? Code, string? Name, decimal? Price, string? Currency, int? DurationDays, int? SortOrder, bool? IsActive, string? Status, string? Description);
public sealed record PackageStatusRequest(Guid? AdminId, bool? IsActive, string? Status);
public sealed record AdminCategoryRequest(Guid? AdminId, string? Name, string? Slug, string? Code, string? Description, string? ParentCode, string? IconKey, int? SortOrder, bool? IsActive, string? Status);
public sealed record CategoryStatusRequest(Guid? AdminId, bool? IsActive, string? Status);
public sealed record UpdateUserRoleSourceRequest(Guid? AdminId, string Role, string Source, string? Status);
public sealed record UpdateListingStatusRequest(Guid? AdminId, string? Status, string? Reason);
public sealed record AdminUserRequest(Guid? AdminId, string? Name, string? Email, string? Phone, string? Location, string? AvatarUrl, string? Role, string? Source, string? Status, string? Password, bool? EmailVerified, bool? PhoneVerified, bool? IdVerified, bool? BusinessVerified, int? TrustScore);
public sealed record UserStatusRequest(Guid? AdminId, string? Status, bool? IsActive);
public sealed record ModerateRequest(Guid? AdminId, string Decision, string? Reason);
public sealed record BadgeRequest(Guid? AdminId, bool EmailVerified, bool PhoneVerified, bool IdVerified, bool BusinessVerified, int TrustScore);
public sealed record ResolveReportRequest(Guid? AdminId, string Status);
public sealed record ModerateAdRequest(Guid? AdminId, string Status, string? Reason, int? SortOrder);
public sealed record CreateAdRequest(Guid? AdminId, string? CampaignName, string? Title, string? Description, string? TargetUrl, string? ImageUrl, string? DesktopImageUrl, string? MobileImageUrl, string? Placement, string? Status, int? SortOrder, bool? OpenInNewTab, int? MaxImpressions, int? MaxClicks, DateTimeOffset? ExpiresAt, DateTimeOffset? EndDate, DateTimeOffset? ExpiredDate)
{
    public string PrimaryImageUrl => (string.IsNullOrWhiteSpace(ImageUrl) ? DesktopImageUrl : ImageUrl)?.Trim() ?? string.Empty;
    public DateTimeOffset? ResolvedEndDate => ExpiresAt ?? EndDate ?? ExpiredDate;
}

public sealed record UpdateAdRequest(Guid? AdminId, string? CampaignName, string? Title, string? Description, string? TargetUrl, string? ImageUrl, string? DesktopImageUrl, string? MobileImageUrl, string? Placement, string? Status, int? SortOrder, bool? OpenInNewTab, int? MaxImpressions, int? MaxClicks, DateTimeOffset? ExpiresAt, DateTimeOffset? EndDate, DateTimeOffset? ExpiredDate)
{
    public bool HasImageUrl => ImageUrl is not null || DesktopImageUrl is not null || MobileImageUrl is not null;
    public string PrimaryImageUrl => (string.IsNullOrWhiteSpace(ImageUrl) ? (string.IsNullOrWhiteSpace(DesktopImageUrl) ? MobileImageUrl : DesktopImageUrl) : ImageUrl)?.Trim() ?? string.Empty;
    public DateTimeOffset? ResolvedEndDate => ExpiresAt ?? EndDate ?? ExpiredDate;
}
public sealed record TogglePlacementRequest(Guid? AdminId, bool IsActive);

public sealed record ModerateMessageRequest(Guid? AdminId, string Status, bool HideMessage);
public sealed record AdminSendNotificationRequest(Guid? AdminId, bool SendToAll, List<Guid> UserIds, string Type, string Title, string Body, string? Url, string? ImageUrl);
