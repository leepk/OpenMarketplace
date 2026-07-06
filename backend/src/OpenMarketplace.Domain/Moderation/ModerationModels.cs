namespace OpenMarketplace.Domain.Moderation;
public sealed class Report : OpenMarketplace.Domain.Common.Entity
{
    public Guid ReporterId { get; set; }
    public string TargetType { get; set; } = "Listing";
    public Guid TargetId { get; set; }
    public string Reason { get; set; } = "";
    public string Details { get; set; } = "";
    public string Status { get; set; } = "Open";
}
public sealed class AuditLog : OpenMarketplace.Domain.Common.Entity
{
    public Guid? ActorId { get; set; }
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public Guid? EntityId { get; set; }
    public string Details { get; set; } = "";
}


public sealed class BlockedWord : OpenMarketplace.Domain.Common.Entity
{
    public string Word { get; set; } = "";
    public string NormalizedWord { get; set; } = "";
    public string Language { get; set; } = "Any";
    public string Severity { get; set; } = "Medium";
    public string MatchType { get; set; } = "Contains";
    public string Category { get; set; } = "General";
    public bool IsActive { get; set; } = true;
    public string Notes { get; set; } = "";
}

public sealed class ListingModerationResult : OpenMarketplace.Domain.Common.Entity
{
    public Guid ListingId { get; set; }
    public string Source { get; set; } = "OpenAI";
    public string TargetType { get; set; } = "Text";
    public string Status { get; set; } = "Safe";
    public string Reason { get; set; } = "";
    public string Categories { get; set; } = "";
    public decimal MaxScore { get; set; }
    public string RawResponse { get; set; } = "";
}
