namespace OpenMarketplace.Domain.Advertising;
public sealed class AdPlacement : OpenMarketplace.Domain.Common.Entity { public string Code {get;set;}=""; public string Name {get;set;}=""; public int InsertEvery {get;set;}=10; public bool IsActive {get;set;}=true; }
