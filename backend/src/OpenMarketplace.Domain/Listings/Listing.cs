namespace OpenMarketplace.Domain.Listings;
public sealed class Listing : OpenMarketplace.Domain.Common.Entity
{
    public Guid SellerId { get; set; }
    public Guid CategoryId { get; set; }
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal? Price { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = "Draft";
    public string ModerationStatus { get; set; } = "Pending";
    public string ModerationReason { get; set; } = "";
    public string Location { get; set; } = "";
    public string AddressLine { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string Country { get; set; } = "US";
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string LocationSource { get; set; } = "Manual";
    public string LocationPrecision { get; set; } = "ApproximateCity";
    public bool HideExactLocation { get; set; } = true;
    public bool IsFeatured { get; set; }
    public bool IsUrgent { get; set; }
    public bool IsPinned { get; set; }
    public bool IsSold { get; set; }
    public string PackageCode { get; set; } = "FREE";
    public string PackageStatus { get; set; } = "Active";
    public DateTimeOffset? PackageStartsAt { get; set; }
    public DateTimeOffset? PackageEndsAt { get; set; }
    public int ViewCount { get; set; }
    public int FavoriteCount { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}
