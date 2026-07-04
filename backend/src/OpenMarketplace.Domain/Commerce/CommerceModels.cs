namespace OpenMarketplace.Domain.Commerce;

public sealed class PaymentProvider : OpenMarketplace.Domain.Common.Entity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "Test";
    public string DisplayName { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
    public bool IsTestMode { get; set; } = true;
    public string Currency { get; set; } = "USD";
    public int SortOrder { get; set; }
    public string ConfigJson { get; set; } = "{}";
    public string PublicConfigJson { get; set; } = "{}";
}
public sealed class Order : OpenMarketplace.Domain.Common.Entity
{
    public Guid UserId { get; set; }
    public Guid? ListingId { get; set; }
    public string OrderNumber { get; set; } = "";
    public decimal Total { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = "Pending";
    public string Provider { get; set; } = "Manual";
    public string ProviderReference { get; set; } = "";
}
public sealed class Payment : OpenMarketplace.Domain.Common.Entity
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = "Pending";
    public string Provider { get; set; } = "Stripe";
    public string ProviderReference { get; set; } = "";
}
public sealed class Invoice : OpenMarketplace.Domain.Common.Entity
{
    public Guid OrderId { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = "Issued";
    public string PdfUrl { get; set; } = "";
}
public sealed class Promotion : OpenMarketplace.Domain.Common.Entity
{
    public Guid ListingId { get; set; }
    public Guid PackageId { get; set; }
    public string Type { get; set; } = "Featured";
    public DateTimeOffset StartsAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset EndsAt { get; set; } = DateTimeOffset.UtcNow.AddDays(7);
    public string Status { get; set; } = "Active";
}
