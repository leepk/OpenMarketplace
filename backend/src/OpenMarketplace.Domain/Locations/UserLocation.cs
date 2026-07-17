using OpenMarketplace.Domain.Common;

namespace OpenMarketplace.Domain.Locations;

public sealed class UserLocation : Entity
{
    public Guid UserId { get; set; }
    public string Label { get; set; } = "";
    public string AddressLine { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string Country { get; set; } = "US";
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int UseCount { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public bool IsDefault { get; set; }
}
