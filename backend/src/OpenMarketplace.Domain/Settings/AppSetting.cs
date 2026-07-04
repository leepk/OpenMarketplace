namespace OpenMarketplace.Domain.Settings;
public sealed class AppSetting : OpenMarketplace.Domain.Common.Entity { public string Key {get;set;}=""; public string Value {get;set;}=""; public string ValueType {get;set;}="String"; public bool IsPublic {get;set;} }
