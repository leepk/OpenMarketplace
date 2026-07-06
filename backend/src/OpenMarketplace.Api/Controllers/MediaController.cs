using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using OpenMarketplace.Domain.Media;
using OpenMarketplace.Infrastructure.Persistence;
using OpenMarketplace.Api.Services;
using OpenMarketplace.Shared.Api;

namespace OpenMarketplace.Api.Controllers;
[ApiController]
[Route("api/v1/media")]
public sealed class MediaController(AppDbContext db, IWebHostEnvironment env, IContentModerationService moderation) : ControllerBase
{
    public sealed class UploadMediaForm
    {
        public IFormFile? File { get; set; }
        public Guid? ListingId { get; set; }
        public Guid? OwnerId { get; set; }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] Guid? listingId, CancellationToken ct)
    {
        var q = db.MediaAssets.AsNoTracking();
        if (listingId.HasValue) q = q.Where(x => x.ListingId == listingId.Value);
        return Ok(ApiResponse<object>.Ok(new{items=await q.OrderByDescending(x=>x.CreatedAt).ToListAsync(ct)}, HttpContext.TraceIdentifier));
    }
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(25_000_000)]
    public async Task<ActionResult<ApiResponse<MediaAsset>>> Upload([FromForm] UploadMediaForm form, CancellationToken ct)
    {
        var file = form.File;
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse<MediaAsset>.Fail("InvalidFile", "Please choose an image to upload.", HttpContext.TraceIdentifier));
        }

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(ApiResponse<MediaAsset>.Fail("InvalidFileType", "Only image files are supported.", HttpContext.TraceIdentifier));
        }

        var id = Guid.CreateVersion7();
        var originalName = Path.GetFileName(file.FileName);
        var safeFileName = Regex.Replace(originalName, @"[^a-zA-Z0-9._-]", "-");
        if (string.IsNullOrWhiteSpace(safeFileName)) safeFileName = $"listing-{id}.jpg";

        byte[] bytes;
        await using (var ms = new MemoryStream())
        {
            await file.CopyToAsync(ms, ct);
            bytes = ms.ToArray();
        }

        var moderationResult = await moderation.CheckImageAsync(bytes, file.ContentType, ct);
        if (!moderationResult.IsSafe)
        {
            if (form.ListingId.HasValue)
            {
                var listing = await db.Listings.FirstOrDefaultAsync(x => x.Id == form.ListingId.Value && !x.IsDeleted, ct);
                if (listing is not null)
                {
                    listing.ModerationStatus = moderationResult.Status;
                    listing.ModerationReason = moderationResult.Reason;
                    listing.Status = moderationResult.IsRejected ? "Rejected" : "Pending";
                    db.ListingModerationResults.Add(new OpenMarketplace.Domain.Moderation.ListingModerationResult
                    {
                        ListingId = listing.Id,
                        TargetType = "Image",
                        Status = moderationResult.Status,
                        Reason = moderationResult.Reason,
                        Categories = moderationResult.Categories,
                        MaxScore = moderationResult.MaxScore,
                        RawResponse = moderationResult.RawResponse
                    });
                    await db.SaveChangesAsync(ct);
                }
            }
            return BadRequest(ApiResponse<MediaAsset>.Fail("ModerationRejected", moderationResult.Reason, HttpContext.TraceIdentifier));
        }

        var mediaRoot = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "media", id.ToString());
        Directory.CreateDirectory(mediaRoot);
        var physicalPath = Path.Combine(mediaRoot, safeFileName);
        await System.IO.File.WriteAllBytesAsync(physicalPath, bytes, ct);

        var asset = new MediaAsset
        {
            Id = id,
            OwnerId = form.OwnerId ?? DemoIds.Customer,
            ListingId = form.ListingId,
            FileName = safeFileName,
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            Url = $"/media/{id}/{safeFileName}",
            StorageProvider = "Local"
        };

        db.MediaAssets.Add(asset);
        if (form.ListingId.HasValue)
        {
            db.ListingModerationResults.Add(new OpenMarketplace.Domain.Moderation.ListingModerationResult
            {
                ListingId = form.ListingId.Value,
                TargetType = "Image",
                Status = moderationResult.Status,
                Reason = moderationResult.Reason,
                Categories = moderationResult.Categories,
                MaxScore = moderationResult.MaxScore,
                RawResponse = moderationResult.RawResponse
            });
        }
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<MediaAsset>.Ok(asset, HttpContext.TraceIdentifier));
    }
}
