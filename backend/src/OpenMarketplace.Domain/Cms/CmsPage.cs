namespace OpenMarketplace.Domain.Cms;
public sealed class CmsPage : OpenMarketplace.Domain.Common.Entity { public string Slug {get;set;}=""; public string Title {get;set;}=""; public string ContentMd {get;set;}=""; public string Status {get;set;}="Draft"; }
