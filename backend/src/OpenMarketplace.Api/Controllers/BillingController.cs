using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenMarketplace.Domain.Commerce;
using OpenMarketplace.Domain.Communication;
using OpenMarketplace.Infrastructure.Persistence;
using OpenMarketplace.Shared.Api;

namespace OpenMarketplace.Api.Controllers;

[ApiController]
[Route("api/v1/billing")]
public sealed class BillingController(AppDbContext db):ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] Guid? userId, CancellationToken ct)
    {
        var uid = userId ?? DemoIds.Customer;
        var orders = await db.Orders.AsNoTracking().Where(x=>x.UserId==uid).OrderByDescending(x=>x.CreatedAt).ToListAsync(ct);
        var orderIds = orders.Select(x=>x.Id).ToArray();
        var payments = await db.Payments.AsNoTracking().Where(x=>orderIds.Contains(x.OrderId)).ToListAsync(ct);
        var invoices = await db.Invoices.AsNoTracking().Where(x=>orderIds.Contains(x.OrderId)).ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(new{orders,payments,invoices},HttpContext.TraceIdentifier));
    }

    [HttpGet("providers")]
    public async Task<ActionResult<ApiResponse<object>>> Providers(CancellationToken ct)
    {
        var providers = await db.PaymentProviders.AsNoTracking()
            .Where(x => x.IsEnabled && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.Type,
                x.DisplayName,
                x.Currency,
                x.IsTestMode,
                x.SortOrder,
                x.PublicConfigJson
            })
            .ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { items = providers }, HttpContext.TraceIdentifier));
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<ApiResponse<object>>> Checkout(CheckoutRequest request, CancellationToken ct)
    {
        var package = await db.Packages.FirstOrDefaultAsync(x=>x.Id==request.PackageId || x.Code==request.PackageCode,ct);
        var amount = package?.Price ?? request.Amount;
        var providerCode = (request.ProviderCode ?? request.PaymentMethod ?? (amount <= 0 ? "FREE" : "TEST")).Trim().ToUpperInvariant();

        PaymentProvider? provider = null;
        if (amount > 0)
        {
            provider = await db.PaymentProviders.FirstOrDefaultAsync(x => x.Code == providerCode && x.IsEnabled && !x.IsDeleted, ct);
            if (provider is null)
            {
                return BadRequest(ApiResponse<object>.Fail("PaymentProviderDisabled", $"Payment provider '{providerCode}' is not enabled.", HttpContext.TraceIdentifier));
            }
        }

        var paymentStatus = ResolvePaymentStatus(provider?.Type ?? providerCode, request.ProviderStatus, request.PaymentToken, amount);
        var hasPayment = amount <= 0 || paymentStatus is "Succeeded" or "Pending";
        var orderStatus = amount <= 0 ? "Paid" : paymentStatus switch
        {
            "Succeeded" => "Paid",
            "Pending" => "Pending",
            _ => "Failed"
        };

        var order = new Order
        {
            UserId=request.UserId==Guid.Empty?DemoIds.Customer:request.UserId,
            ListingId=request.ListingId,
            OrderNumber=$"OM-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100,999)}",
            Total=amount,
            Status=orderStatus,
            Provider=amount <= 0 ? "Free" : providerCode,
            ProviderReference=request.PaymentToken ?? GenerateReference(providerCode)
        };
        db.Orders.Add(order);

        var payment = new Payment
        {
            OrderId=order.Id,
            Amount=amount,
            Currency=package?.Currency ?? "USD",
            Status=paymentStatus,
            Provider=order.Provider,
            ProviderReference=order.ProviderReference
        };
        var invoice = new Invoice
        {
            OrderId=order.Id,
            InvoiceNumber=$"INV-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
            Amount=amount,
            Currency=package?.Currency ?? "USD",
            Status=paymentStatus == "Succeeded" || amount <= 0 ? "Issued" : "Pending",
            PdfUrl=$"/api/v1/billing/invoices/{order.Id}/pdf"
        };
        db.Payments.Add(payment);
        db.Invoices.Add(invoice);

        if(request.ListingId.HasValue && package is not null)
        {
            var now = DateTimeOffset.UtcNow;
            var promotionStatus = hasPayment && paymentStatus != "Failed" ? "Active" : "Pending";
            db.Promotions.Add(new Promotion
            {
                ListingId=request.ListingId.Value,
                PackageId=package.Id,
                Type=package.Code,
                StartsAt=now,
                EndsAt=now.AddDays(package.DurationDays),
                Status=promotionStatus
            });

            var listing = await db.Listings.FirstOrDefaultAsync(x => x.Id == request.ListingId.Value && !x.IsDeleted, ct);
            if (listing is not null)
            {
                ApplyPackageToListing(listing, package, promotionStatus, now);
            }
        }

        db.Notifications.Add(new Notification
        {
            UserId=order.UserId,
            Type="Billing",
            Title=orderStatus == "Paid" ? "Payment received" : orderStatus == "Pending" ? "Checkout pending" : "Payment failed",
            Body=$"Order {order.OrderNumber} is {orderStatus.ToLowerInvariant()} via {order.Provider}.",
            Url="/billing"
        });
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new{order,payment,invoice,provider=provider is null ? null : new { provider.Code, provider.Type, provider.DisplayName }, checkoutUrl=$"/billing?order={order.Id}"},HttpContext.TraceIdentifier));
    }

    [HttpGet("invoices/{orderId:guid}/pdf")]
    public IActionResult Pdf(Guid orderId) => File(System.Text.Encoding.UTF8.GetBytes($"OpenMarketplace invoice for order {orderId}"), "application/pdf", $"invoice-{orderId}.pdf");

    private static string ResolvePaymentStatus(string providerType, string? requestedStatus, string? token, decimal amount)
    {
        if (amount <= 0) return "Succeeded";
        var normalized = (requestedStatus ?? "").Trim().ToLowerInvariant();
        if (providerType.Equals("Test", StringComparison.OrdinalIgnoreCase))
        {
            return normalized switch
            {
                "pending" => "Pending",
                "failed" or "fail" => "Failed",
                _ => "Succeeded"
            };
        }
        return string.IsNullOrWhiteSpace(token) ? "Pending" : "Succeeded";
    }

    private static string GenerateReference(string providerCode) => $"{providerCode.ToLowerInvariant()}_{Guid.NewGuid():N}";

    private static void ApplyPackageToListing(OpenMarketplace.Domain.Listings.Listing listing, Package package, string promotionStatus, DateTimeOffset now)
    {
        listing.PackageCode = package.Code;
        listing.PackageStatus = promotionStatus;
        listing.PackageStartsAt = promotionStatus == "Active" ? now : null;
        listing.PackageEndsAt = promotionStatus == "Active" ? now.AddDays(package.DurationDays) : null;
        listing.ExpiresAt = now.AddDays(package.DurationDays);

        var active = promotionStatus == "Active";
        listing.IsFeatured = active && string.Equals(package.Code, "FEATURED", StringComparison.OrdinalIgnoreCase);
        listing.IsUrgent = active && string.Equals(package.Code, "URGENT", StringComparison.OrdinalIgnoreCase);
        listing.IsPinned = active && string.Equals(package.Code, "PREMIUM", StringComparison.OrdinalIgnoreCase);

        if (active && (listing.Status == "Pending" || listing.Status == "Draft"))
        {
            listing.Status = "Published";
        }
        listing.UpdatedAt = now;
    }
}

public sealed record CheckoutRequest(Guid UserId, Guid? ListingId, Guid? PackageId, string? PackageCode, decimal Amount, string? PaymentMethod, string? PaymentToken, string? ProviderCode, string? ProviderStatus, string? ProviderPayload);
