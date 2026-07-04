namespace OpenMarketplace.Domain.Media;
public sealed class MediaAsset : OpenMarketplace.Domain.Common.Entity
{
    public Guid OwnerId { get; set; }
    public Guid? ListingId { get; set; }
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    public string Url { get; set; } = "";
    public string StorageProvider { get; set; } = "Local";
    public bool IsPrivate { get; set; }
}
