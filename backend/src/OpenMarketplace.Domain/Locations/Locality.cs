using OpenMarketplace.Domain.Common;

namespace OpenMarketplace.Domain.Locations;

public sealed class Locality : Entity
{
    public string Name { get; set; } = string.Empty;
    public string StateCode { get; set; } = "CA";
    public string CountryCode { get; set; } = "US";
    public string GeoId { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int SortOrder { get; set; }
    public long SelectionCount { get; set; }
    public bool IsActive { get; set; } = true;
}
