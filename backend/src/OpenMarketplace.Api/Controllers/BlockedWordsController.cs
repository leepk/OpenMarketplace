using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenMarketplace.Api.Services;
using OpenMarketplace.Domain.Moderation;
using OpenMarketplace.Infrastructure.Persistence;
using OpenMarketplace.Shared.Api;

namespace OpenMarketplace.Api.Controllers;

[ApiController]
[Route("api/v1/admin/blocked-words")]
[Authorize(Roles = "Admin,SuperAdmin,System,Moderator,Support")]
public sealed class BlockedWordsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] string? q, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.BlockedWords.AsNoTracking().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(x => x.Word.ToLower().Contains(term) || x.Category.ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            var active = status.Equals("Active", StringComparison.OrdinalIgnoreCase);
            query = query.Where(x => x.IsActive == active);
        }
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { items, total, page, pageSize }, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<BlockedWord>>> Save(BlockedWordRequest request, CancellationToken ct)
    {
        var word = request.Word?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(word)) return BadRequest(ApiResponse<BlockedWord>.Fail("Validation", "Word is required", HttpContext.TraceIdentifier));

        BlockedWord row;
        if (request.Id.HasValue)
        {
            row = await db.BlockedWords.FirstOrDefaultAsync(x => x.Id == request.Id.Value && !x.IsDeleted, ct)
                ?? new BlockedWord { Id = request.Id.Value };
            if (db.Entry(row).State == EntityState.Detached) db.BlockedWords.Add(row);
        }
        else
        {
            row = new BlockedWord();
            db.BlockedWords.Add(row);
        }

        row.Word = word;
        row.NormalizedWord = ContentModerationService.Normalize(word);
        row.Language = string.IsNullOrWhiteSpace(request.Language) ? "Any" : request.Language.Trim();
        row.Severity = NormalizeChoice(request.Severity, "Medium", ["Low", "Medium", "High"]);
        row.MatchType = NormalizeChoice(request.MatchType, "Contains", ["Contains", "Exact", "Regex"]);
        row.Category = string.IsNullOrWhiteSpace(request.Category) ? "General" : request.Category.Trim();
        row.IsActive = request.IsActive;
        row.Notes = request.Notes?.Trim() ?? string.Empty;
        row.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<BlockedWord>.Ok(row, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/toggle")]
    public async Task<ActionResult<ApiResponse<BlockedWord>>> Toggle(Guid id, CancellationToken ct)
    {
        var row = await db.BlockedWords.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (row is null) return NotFound(ApiResponse<BlockedWord>.Fail("NotFound", "Blocked word not found", HttpContext.TraceIdentifier));
        row.IsActive = !row.IsActive;
        row.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<BlockedWord>.Ok(row, HttpContext.TraceIdentifier));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        var row = await db.BlockedWords.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (row is null) return NotFound(ApiResponse<object>.Fail("NotFound", "Blocked word not found", HttpContext.TraceIdentifier));
        row.IsDeleted = true;
        row.IsActive = false;
        row.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { deleted = true }, HttpContext.TraceIdentifier));
    }

    private static string NormalizeChoice(string? value, string fallback, string[] allowed) => allowed.FirstOrDefault(x => x.Equals(value, StringComparison.OrdinalIgnoreCase)) ?? fallback;
}

public sealed record BlockedWordRequest(Guid? Id, string? Word, string? Language, string? Severity, string? MatchType, string? Category, bool IsActive, string? Notes);
