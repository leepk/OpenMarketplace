namespace OpenMarketplace.Domain.Categories;

public sealed class Category : OpenMarketplace.Domain.Common.Entity
{
    public Guid? ParentId { get; set; }
    public string Code { get; set; } = "";
    public string? ParentCode { get; set; }
    public string IconKey { get; set; } = "category";
    public string Name { get; set; } = ""; // legacy/admin fallback only; customer localizes by Code
    public string Slug { get; set; } = ""; // legacy route fallback; mirrors Code with dash style
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
