namespace OpenMarketplace.Domain.Engagement;
public sealed class Favorite : OpenMarketplace.Domain.Common.Entity { public Guid UserId { get; set; } public Guid ListingId { get; set; } }
public sealed class ListingLike : OpenMarketplace.Domain.Common.Entity { public Guid UserId { get; set; } public Guid ListingId { get; set; } }
public sealed class ListingComment : OpenMarketplace.Domain.Common.Entity { public Guid UserId { get; set; } public Guid ListingId { get; set; } public string Body { get; set; } = ""; public string Status { get; set; } = "Published"; }
public sealed class ListingReview : OpenMarketplace.Domain.Common.Entity { public Guid ReviewerId { get; set; } public Guid SellerId { get; set; } public Guid? ListingId { get; set; } public int Rating { get; set; } = 5; public string Body { get; set; } = ""; public string Status { get; set; } = "Published"; }
