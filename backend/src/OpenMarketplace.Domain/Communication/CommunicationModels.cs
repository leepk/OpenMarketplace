namespace OpenMarketplace.Domain.Communication;
public sealed class Conversation : OpenMarketplace.Domain.Common.Entity
{
    public Guid ListingId { get; set; }
    public Guid BuyerId { get; set; }
    public Guid SellerId { get; set; }
    public string Subject { get; set; } = "";
    public DateTimeOffset LastMessageAt { get; set; } = DateTimeOffset.UtcNow;
    public string Status { get; set; } = "Open";
    public Guid? LastMessageId { get; set; }
    public string LastMessagePreview { get; set; } = "";
}

public sealed class Message : OpenMarketplace.Domain.Common.Entity
{
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public Guid? ReceiverId { get; set; }
    public string MessageType { get; set; } = "Text";
    public string Body { get; set; } = "";
    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
    public string ModerationStatus { get; set; } = "Allowed";
    public string MetadataJson { get; set; } = "{}";
}
public sealed class Notification : OpenMarketplace.Domain.Common.Entity
{
    public Guid UserId { get; set; }
    public string Type { get; set; } = "General";
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string Url { get; set; } = "";
    public string EntityType { get; set; } = "";
    public Guid? EntityId { get; set; }
    public string ImageUrl { get; set; } = "";
    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}
