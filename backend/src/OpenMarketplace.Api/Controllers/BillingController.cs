using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using OpenMarketplace.Domain.Commerce;
using OpenMarketplace.Domain.Communication;
using OpenMarketplace.Infrastructure.Persistence;
using OpenMarketplace.Shared.Api;

namespace OpenMarketplace.Api.Controllers;

[ApiController]
[Route("api/v1/billing")]
public sealed class BillingController(AppDbContext db, IHttpClientFactory httpClientFactory):ControllerBase
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
        var paymentRows = await db.AppSettings.AsNoTracking()
            .Where(x => !x.IsDeleted && EF.Functions.ILike(x.Key, "payment.%"))
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .ToListAsync(ct);

        // Keep settings lookup case-insensitive and tolerate duplicate keys left by older deployments.
        var rows = paymentRows
            .GroupBy(x => x.Key.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Value, StringComparer.OrdinalIgnoreCase);

        static bool ParseBool(string? value, bool fallback = false)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            return value.Trim().ToLowerInvariant() is "true" or "1" or "yes" or "on" or "enabled";
        }
        bool B(string key, bool fallback = false) => rows.TryGetValue(key, out var value) ? ParseBool(value, fallback) : fallback;
        string V(string key, string fallback = "") => rows.TryGetValue(key, out var value) ? value?.Trim() ?? fallback : fallback;

        var currency = V("payment.currency", "USD");
        var items = new List<object>();
        var stripeConfigured = !string.IsNullOrWhiteSpace(V("payment.stripe_publishable_key"));
        var paypalConfigured = !string.IsNullOrWhiteSpace(V("payment.paypal_client_id"));

        if (B("payment.stripe_enabled") && stripeConfigured)
            items.Add(new { code = "STRIPE", name = "Stripe", type = "Stripe", displayName = "Credit / Debit Card", currency, isTestMode = false, sortOrder = 10, configured = true, publishableKey = V("payment.stripe_publishable_key") });
        if (B("payment.paypal_enabled") && paypalConfigured)
            items.Add(new { code = "PAYPAL", name = "PayPal", type = "PayPal", displayName = "PayPal", currency, isTestMode = V("payment.paypal_mode", "Sandbox").Equals("Sandbox", StringComparison.OrdinalIgnoreCase), sortOrder = 20, configured = true, mode = V("payment.paypal_mode", "Sandbox"), clientId = V("payment.paypal_client_id") });
        if (B("payment.manual_enabled", true))
            items.Add(new { code = "MANUAL", name = "Manual", type = "Manual", displayName = "Manual/Test payment", currency, isTestMode = true, sortOrder = 90, configured = true });

        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        return Ok(ApiResponse<object>.Ok(new { items }, HttpContext.TraceIdentifier));
    }


    [HttpPost("stripe/payment-intent")]
    public async Task<ActionResult<ApiResponse<object>>> CreateStripePaymentIntent(CreateGatewayPaymentRequest request, CancellationToken ct)
    {
        var package = await ResolvePackageAsync(request.PackageId, request.PackageCode, ct);
        if (package is null)
            return BadRequest(ApiResponse<object>.Fail("PackageNotFound", "The selected package was not found.", HttpContext.TraceIdentifier));

        var settings = await GetPaymentSettingsAsync(ct);
        if (!IsEnabled(settings, "payment.stripe_enabled") || !settings.TryGetValue("payment.stripe_secret_key", out var secretKey) || string.IsNullOrWhiteSpace(secretKey))
            return BadRequest(ApiResponse<object>.Fail("StripeNotConfigured", "Stripe is not enabled or the secret key is missing.", HttpContext.TraceIdentifier));

        var currency = Setting(settings, "payment.currency", package.Currency ?? "USD").ToLowerInvariant();
        var amountMinor = DecimalToMinorUnits(package.Price, currency);
        if (amountMinor <= 0)
            return BadRequest(ApiResponse<object>.Fail("InvalidAmount", "Stripe PaymentIntent requires a positive amount.", HttpContext.TraceIdentifier));

        using var message = new HttpRequestMessage(HttpMethod.Post, "https://api.stripe.com/v1/payment_intents");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secretKey.Trim());
        message.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["amount"] = amountMinor.ToString(CultureInfo.InvariantCulture),
            ["currency"] = currency,
            ["payment_method_types[]"] = "card",
            ["metadata[user_id]"] = request.UserId.ToString(),
            ["metadata[package_id]"] = package.Id.ToString(),
            ["metadata[package_code]"] = package.Code
        });

        var client = httpClientFactory.CreateClient();
        using var response = await client.SendAsync(message, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            // A 401 here belongs to Stripe (usually an invalid secret key), not to
            // the Vunoca customer session. Never proxy it as our own HTTP 401,
            // otherwise the customer app interprets it as an expired login.
            var gatewayStatus = response.StatusCode is System.Net.HttpStatusCode.BadRequest
                ? StatusCodes.Status400BadRequest
                : StatusCodes.Status502BadGateway;
            return StatusCode(gatewayStatus, ApiResponse<object>.Fail(
                "StripeError",
                GatewayError(json, "Stripe could not create the payment."),
                HttpContext.TraceIdentifier));
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        return Ok(ApiResponse<object>.Ok(new
        {
            paymentIntentId = root.GetProperty("id").GetString(),
            clientSecret = root.GetProperty("client_secret").GetString(),
            amount = package.Price,
            currency = currency.ToUpperInvariant()
        }, HttpContext.TraceIdentifier));
    }

    [HttpPost("paypal/orders")]
    public async Task<ActionResult<ApiResponse<object>>> CreatePayPalOrder(CreateGatewayPaymentRequest request, CancellationToken ct)
    {
        var package = await ResolvePackageAsync(request.PackageId, request.PackageCode, ct);
        if (package is null)
            return BadRequest(ApiResponse<object>.Fail("PackageNotFound", "The selected package was not found.", HttpContext.TraceIdentifier));

        var settings = await GetPaymentSettingsAsync(ct);
        if (!IsEnabled(settings, "payment.paypal_enabled"))
            return BadRequest(ApiResponse<object>.Fail("PayPalDisabled", "PayPal is disabled.", HttpContext.TraceIdentifier));

        var accessToken = await GetPayPalAccessTokenAsync(settings, ct);
        if (string.IsNullOrWhiteSpace(accessToken))
            return BadRequest(ApiResponse<object>.Fail("PayPalNotConfigured", "PayPal client ID or secret is missing/invalid.", HttpContext.TraceIdentifier));

        var currency = Setting(settings, "payment.currency", package.Currency ?? "USD").ToUpperInvariant();
        var baseUrl = PayPalBaseUrl(settings);
        var payload = JsonSerializer.Serialize(new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    reference_id = package.Id.ToString(),
                    custom_id = request.UserId.ToString(),
                    description = $"{package.Name} listing package",
                    amount = new { currency_code = currency, value = package.Price.ToString("0.00", CultureInfo.InvariantCulture) }
                }
            }
        });

        using var message = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v2/checkout/orders");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        message.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        var client = httpClientFactory.CreateClient();
        using var response = await client.SendAsync(message, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, ApiResponse<object>.Fail("PayPalError", GatewayError(json, "PayPal could not create the order."), HttpContext.TraceIdentifier));

        using var document = JsonDocument.Parse(json);
        return Ok(ApiResponse<object>.Ok(new { orderId = document.RootElement.GetProperty("id").GetString() }, HttpContext.TraceIdentifier));
    }

    [HttpPost("paypal/orders/{orderId}/capture")]
    public async Task<ActionResult<ApiResponse<object>>> CapturePayPalOrder(string orderId, CancellationToken ct)
    {
        var settings = await GetPaymentSettingsAsync(ct);
        var accessToken = await GetPayPalAccessTokenAsync(settings, ct);
        if (string.IsNullOrWhiteSpace(accessToken))
            return BadRequest(ApiResponse<object>.Fail("PayPalNotConfigured", "PayPal client ID or secret is missing/invalid.", HttpContext.TraceIdentifier));

        using var message = new HttpRequestMessage(HttpMethod.Post, $"{PayPalBaseUrl(settings)}/v2/checkout/orders/{Uri.EscapeDataString(orderId)}/capture");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        message.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        var client = httpClientFactory.CreateClient();
        using var response = await client.SendAsync(message, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, ApiResponse<object>.Fail("PayPalCaptureError", GatewayError(json, "PayPal could not capture the order."), HttpContext.TraceIdentifier));

        using var document = JsonDocument.Parse(json);
        var status = document.RootElement.TryGetProperty("status", out var statusNode) ? statusNode.GetString() : "UNKNOWN";
        return Ok(ApiResponse<object>.Ok(new { orderId, status }, HttpContext.TraceIdentifier));
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<ApiResponse<object>>> Checkout(CheckoutRequest request, CancellationToken ct)
    {
        var package = await db.Packages.FirstOrDefaultAsync(x=>x.Id==request.PackageId || x.Code==request.PackageCode,ct);
        var amount = package?.Price ?? request.Amount;
        var providerCode = (request.ProviderCode ?? request.PaymentMethod ?? (amount <= 0 ? "FREE" : "TEST")).Trim().ToUpperInvariant();

        PaymentProvider? provider = null;
        string providerType = providerCode;
        if (amount > 0)
        {
            var paymentSettings = await db.AppSettings.AsNoTracking()
                .Where(x => x.Key.StartsWith("payment.") && !x.IsDeleted)
                .ToDictionaryAsync(x => x.Key, x => x.Value, ct);
            static bool Enabled(Dictionary<string, string> values, string key, bool fallback = false)
            {
                if (!values.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value)) return fallback;
                return value.Trim().ToLowerInvariant() is "true" or "1" or "yes" or "on" or "enabled";
            }

            var valid = providerCode switch
            {
                "STRIPE" => Enabled(paymentSettings, "payment.stripe_enabled") && paymentSettings.TryGetValue("payment.stripe_publishable_key", out var stripeKey) && !string.IsNullOrWhiteSpace(stripeKey),
                "PAYPAL" => Enabled(paymentSettings, "payment.paypal_enabled") && paymentSettings.TryGetValue("payment.paypal_client_id", out var paypalId) && !string.IsNullOrWhiteSpace(paypalId),
                "MANUAL" or "TEST" => Enabled(paymentSettings, "payment.manual_enabled", true),
                _ => false
            };
            if (!valid)
                return BadRequest(ApiResponse<object>.Fail("PaymentProviderDisabled", $"Payment provider '{providerCode}' is not enabled or configured.", HttpContext.TraceIdentifier));

            provider = await db.PaymentProviders.FirstOrDefaultAsync(x => x.Code == providerCode && !x.IsDeleted, ct);
            providerType = provider?.Type ?? providerCode;
        }

        var paymentStatus = ResolvePaymentStatus(providerType, request.ProviderStatus, request.PaymentToken, amount);
        if (amount > 0 && providerCode == "STRIPE")
            paymentStatus = await VerifyStripePaymentAsync(request.PaymentToken, amount, package?.Currency ?? "USD", ct) ? "Succeeded" : "Failed";
        else if (amount > 0 && providerCode == "PAYPAL")
            paymentStatus = await VerifyPayPalPaymentAsync(request.PaymentToken, amount, package?.Currency ?? "USD", ct) ? "Succeeded" : "Failed";

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
        return Ok(ApiResponse<object>.Ok(new{order,payment,invoice,provider=new { Code = providerCode, Type = providerType, DisplayName = provider?.DisplayName ?? providerCode }, checkoutUrl=$"/billing?order={order.Id}"},HttpContext.TraceIdentifier));
    }


    private async Task<Package?> ResolvePackageAsync(Guid? packageId, string? packageCode, CancellationToken ct)
        => await db.Packages.AsNoTracking().FirstOrDefaultAsync(x => (packageId.HasValue && x.Id == packageId.Value) || (!string.IsNullOrWhiteSpace(packageCode) && x.Code == packageCode), ct);

    private async Task<Dictionary<string, string>> GetPaymentSettingsAsync(CancellationToken ct)
        => await db.AppSettings.AsNoTracking().Where(x => x.Key.StartsWith("payment.") && !x.IsDeleted).ToDictionaryAsync(x => x.Key, x => x.Value, ct);

    private static bool IsEnabled(IReadOnlyDictionary<string, string> settings, string key, bool fallback = false)
        => settings.TryGetValue(key, out var value) ? value.Trim().ToLowerInvariant() is "true" or "1" or "yes" or "on" or "enabled" : fallback;

    private static string Setting(IReadOnlyDictionary<string, string> settings, string key, string fallback = "")
        => settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : fallback;

    private static long DecimalToMinorUnits(decimal amount, string currency)
    {
        var zeroDecimal = currency.ToUpperInvariant() is "JPY" or "KRW" or "VND";
        return checked((long)Math.Round(amount * (zeroDecimal ? 1m : 100m), MidpointRounding.AwayFromZero));
    }

    private static string GatewayError(string json, string fallback)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.TryGetProperty("error", out var error))
            {
                if (error.TryGetProperty("message", out var message)) return message.GetString() ?? fallback;
                if (error.ValueKind == JsonValueKind.String) return error.GetString() ?? fallback;
            }
            if (root.TryGetProperty("message", out var rootMessage)) return rootMessage.GetString() ?? fallback;
            if (root.TryGetProperty("details", out var details) && details.ValueKind == JsonValueKind.Array && details.GetArrayLength() > 0 && details[0].TryGetProperty("description", out var description))
                return description.GetString() ?? fallback;
        }
        catch { }
        return fallback;
    }

    private static string PayPalBaseUrl(IReadOnlyDictionary<string, string> settings)
        => Setting(settings, "payment.paypal_mode", "Sandbox").Equals("Live", StringComparison.OrdinalIgnoreCase)
            ? "https://api-m.paypal.com"
            : "https://api-m.sandbox.paypal.com";

    private async Task<string?> GetPayPalAccessTokenAsync(IReadOnlyDictionary<string, string> settings, CancellationToken ct)
    {
        var clientId = Setting(settings, "payment.paypal_client_id");
        var secret = Setting(settings, "payment.paypal_secret");
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(secret)) return null;

        using var message = new HttpRequestMessage(HttpMethod.Post, $"{PayPalBaseUrl(settings)}/v1/oauth2/token");
        message.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{secret}")));
        message.Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["grant_type"] = "client_credentials" });
        var client = httpClientFactory.CreateClient();
        using var response = await client.SendAsync(message, ct);
        if (!response.IsSuccessStatusCode) return null;
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return document.RootElement.TryGetProperty("access_token", out var token) ? token.GetString() : null;
    }

    private async Task<bool> VerifyStripePaymentAsync(string? paymentIntentId, decimal expectedAmount, string expectedCurrency, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(paymentIntentId)) return false;
        var settings = await GetPaymentSettingsAsync(ct);
        var secret = Setting(settings, "payment.stripe_secret_key");
        if (string.IsNullOrWhiteSpace(secret)) return false;

        using var message = new HttpRequestMessage(HttpMethod.Get, $"https://api.stripe.com/v1/payment_intents/{Uri.EscapeDataString(paymentIntentId)}");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);
        var client = httpClientFactory.CreateClient();
        using var response = await client.SendAsync(message, ct);
        if (!response.IsSuccessStatusCode) return false;
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var root = document.RootElement;
        var status = root.GetProperty("status").GetString();
        var amount = root.GetProperty("amount").GetInt64();
        var currency = root.GetProperty("currency").GetString();
        return status == "succeeded" && amount == DecimalToMinorUnits(expectedAmount, expectedCurrency) && string.Equals(currency, expectedCurrency, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> VerifyPayPalPaymentAsync(string? orderId, decimal expectedAmount, string expectedCurrency, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(orderId)) return false;
        var settings = await GetPaymentSettingsAsync(ct);
        var accessToken = await GetPayPalAccessTokenAsync(settings, ct);
        if (string.IsNullOrWhiteSpace(accessToken)) return false;

        using var message = new HttpRequestMessage(HttpMethod.Get, $"{PayPalBaseUrl(settings)}/v2/checkout/orders/{Uri.EscapeDataString(orderId)}");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var client = httpClientFactory.CreateClient();
        using var response = await client.SendAsync(message, ct);
        if (!response.IsSuccessStatusCode) return false;
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var root = document.RootElement;
        if (!root.TryGetProperty("status", out var status) || status.GetString() != "COMPLETED") return false;
        var unit = root.GetProperty("purchase_units")[0];
        var amountNode = unit.GetProperty("amount");
        var value = decimal.Parse(amountNode.GetProperty("value").GetString() ?? "0", CultureInfo.InvariantCulture);
        var currency = amountNode.GetProperty("currency_code").GetString();
        return value == expectedAmount && string.Equals(currency, expectedCurrency, StringComparison.OrdinalIgnoreCase);
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
        if (providerType.Equals("Stripe", StringComparison.OrdinalIgnoreCase) || providerType.Equals("PayPal", StringComparison.OrdinalIgnoreCase))
        {
            return normalized switch
            {
                "succeeded" or "success" or "paid" => "Succeeded",
                "failed" or "fail" => "Failed",
                _ => "Pending"
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

public sealed record CreateGatewayPaymentRequest(Guid UserId, Guid? PackageId, string? PackageCode);
