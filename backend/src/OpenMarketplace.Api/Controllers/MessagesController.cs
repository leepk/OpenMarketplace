using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenMarketplace.Domain.Communication;
using OpenMarketplace.Infrastructure.Persistence;
using OpenMarketplace.Shared.Api;

namespace OpenMarketplace.Api.Controllers;

[ApiController]
[Route("api/v1/messages")]
public sealed class MessagesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Conversations([FromQuery] Guid? userId, CancellationToken ct)
    {
        var uid = userId ?? DemoIds.Customer;

        var conversations = await db.Conversations.AsNoTracking()
            .Where(x => !x.IsDeleted && (x.BuyerId == uid || x.SellerId == uid))
            .OrderByDescending(x => x.LastMessageAt)
            .ToListAsync(ct);

        var conversationIds = conversations.Select(x => x.Id).ToList();
        var listingIds = conversations.Select(x => x.ListingId).Distinct().ToList();
        var userIds = conversations.SelectMany(x => new[] { x.BuyerId, x.SellerId }).Distinct().ToList();

        var latestMessages = await db.Messages.AsNoTracking()
            .Where(x => conversationIds.Contains(x.ConversationId) && !x.IsDeleted)
            .GroupBy(x => x.ConversationId)
            .Select(g => g.OrderByDescending(x => x.CreatedAt).First())
            .ToListAsync(ct);

        var unreadCounts = await db.Messages.AsNoTracking()
            .Where(x => conversationIds.Contains(x.ConversationId) && !x.IsDeleted && !x.IsRead && x.SenderId != uid)
            .GroupBy(x => x.ConversationId)
            .Select(g => new { ConversationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ConversationId, x => x.Count, ct);

        var listings = await db.Listings.AsNoTracking()
            .Where(x => listingIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Title, ImageUrl = db.MediaAssets
                .Where(media => media.ListingId == x.Id)
                .OrderBy(media => media.CreatedAt)
                .Select(media => media.Url)
                .FirstOrDefault(), x.Price, x.Currency, x.Status })
            .ToDictionaryAsync(x => x.Id, ct);

        var users = await db.UserProfiles.AsNoTracking()
            .Where(x => userIds.Contains(x.Id))
            .Select(x => new { x.Id, DisplayName = x.Name, x.Email, x.AvatarUrl })
            .ToDictionaryAsync(x => x.Id, ct);

        var latestByConversation = latestMessages.ToDictionary(x => x.ConversationId);
        var items = conversations.Select(c =>
        {
            latestByConversation.TryGetValue(c.Id, out var latest);
            listings.TryGetValue(c.ListingId, out var listing);
            var otherUserId = c.BuyerId == uid ? c.SellerId : c.BuyerId;
            users.TryGetValue(otherUserId, out var other);
            unreadCounts.TryGetValue(c.Id, out var unread);

            return new
            {
                c.Id,
                c.ListingId,
                c.BuyerId,
                c.SellerId,
                c.Subject,
                c.Status,
                c.LastMessageAt,
                listing,
                otherUser = other,
                lastMessage = latest is null ? null : new
                {
                    latest.Id,
                    latest.Body,
                    latest.MessageType,
                    latest.SenderId,
                    latest.ReceiverId,
                    latest.CreatedAt,
                    latest.IsRead,
                    isMine = latest.SenderId == uid
                },
                unreadCount = unread
            };
        }).ToList();

        return Ok(ApiResponse<object>.Ok(new { items }, HttpContext.TraceIdentifier));
    }

    [HttpGet("{conversationId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Thread(Guid conversationId, [FromQuery] Guid? userId, CancellationToken ct)
    {
        var uid = userId ?? DemoIds.Customer;
        var conversation = await db.Conversations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == conversationId && !x.IsDeleted, ct);
        if (conversation is null) return NotFound(ApiResponse<object>.Fail("NotFound", "Conversation not found", HttpContext.TraceIdentifier));
        if (conversation.BuyerId != uid && conversation.SellerId != uid) return Forbid();

        var messages = await db.Messages.AsNoTracking()
            .Where(x => x.ConversationId == conversationId && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new { x.Id, x.ConversationId, x.SenderId, x.ReceiverId, x.MessageType, x.Body, x.IsRead, x.ReadAt, x.CreatedAt, x.ModerationStatus, x.MetadataJson, isMine = x.SenderId == uid })
            .ToListAsync(ct);

        var listing = await db.Listings.AsNoTracking()
            .Where(x => x.Id == conversation.ListingId)
            .Select(x => new { x.Id, x.Title, ImageUrl = db.MediaAssets
                .Where(media => media.ListingId == x.Id)
                .OrderBy(media => media.CreatedAt)
                .Select(media => media.Url)
                .FirstOrDefault(), x.Price, x.Currency, x.Status })
            .FirstOrDefaultAsync(ct);

        var otherUserId = conversation.BuyerId == uid ? conversation.SellerId : conversation.BuyerId;
        var otherUser = await db.UserProfiles.AsNoTracking()
            .Where(x => x.Id == otherUserId)
            .Select(x => new { x.Id, DisplayName = x.Name, x.Email, x.AvatarUrl })
            .FirstOrDefaultAsync(ct);

        return Ok(ApiResponse<object>.Ok(new { conversation, listing, otherUser, messages }, HttpContext.TraceIdentifier));
    }

    [HttpPost("{conversationId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Send(Guid conversationId, SendMessageRequest request, CancellationToken ct)
    {
        var senderId = request.SenderId == Guid.Empty ? DemoIds.Customer : request.SenderId;
        var body = (request.Body ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(body)) return BadRequest(ApiResponse<object>.Fail("Validation", "Message body is required", HttpContext.TraceIdentifier));

        var conv = await db.Conversations.FirstOrDefaultAsync(x => x.Id == conversationId && !x.IsDeleted, ct);
        if (conv is null) return NotFound(ApiResponse<object>.Fail("NotFound", "Conversation not found", HttpContext.TraceIdentifier));
        if (conv.BuyerId != senderId && conv.SellerId != senderId) return Forbid();

        var receiverId = conv.BuyerId == senderId ? conv.SellerId : conv.BuyerId;
        var msg = new Message { ConversationId = conversationId, SenderId = senderId, ReceiverId = receiverId, MessageType = "Text", Body = body };
        conv.LastMessageAt = DateTimeOffset.UtcNow;
        conv.LastMessageId = msg.Id;
        conv.LastMessagePreview = body.Length > 160 ? body[..160] : body;
        conv.UpdatedAt = DateTimeOffset.UtcNow;
        db.Messages.Add(msg);
        db.Notifications.Add(new Notification
        {
            UserId = receiverId,
            Type = "Message",
            Title = "New message",
            Body = body.Length > 160 ? body[..160] : body,
            Url = $"/messages?conversationId={conversationId}"
        });

        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { message = msg }, HttpContext.TraceIdentifier));
    }

    [HttpPost("{conversationId:guid}/read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkRead(Guid conversationId, ReadConversationRequest request, CancellationToken ct)
    {
        var uid = request.UserId == Guid.Empty ? DemoIds.Customer : request.UserId;
        var conv = await db.Conversations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == conversationId && !x.IsDeleted, ct);
        if (conv is null) return NotFound(ApiResponse<object>.Fail("NotFound", "Conversation not found", HttpContext.TraceIdentifier));
        if (conv.BuyerId != uid && conv.SellerId != uid) return Forbid();

        var messages = await db.Messages.Where(x => x.ConversationId == conversationId && !x.IsDeleted && !x.IsRead && x.SenderId != uid).ToListAsync(ct);
        foreach (var message in messages)
        {
            message.IsRead = true;
            message.ReadAt = DateTimeOffset.UtcNow;
            message.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { read = messages.Count }, HttpContext.TraceIdentifier));
    }
}

public sealed record SendMessageRequest(Guid SenderId, string Body);
public sealed record ReadConversationRequest(Guid UserId);
