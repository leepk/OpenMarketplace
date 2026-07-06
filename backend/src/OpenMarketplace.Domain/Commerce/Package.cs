namespace OpenMarketplace.Domain.Commerce;
public sealed class Package : OpenMarketplace.Domain.Common.Entity { public string Code {get;set;}=""; public string Name {get;set;}=""; public decimal Price {get;set;} public string Currency {get;set;}="USD"; public int DurationDays {get;set;}=30; public int SortOrder {get;set;}=0; public bool IsActive {get;set;}=true; }
