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
