# Commerce API

## Packages

```txt
GET /api/v1/packages
GET /api/v1/packages/{id}
```

## Checkout Package

```txt
POST /api/v1/checkout/package
```

Request:

```json
{
  "listingId": "uuid",
  "packageId": "uuid",
  "boosts": ["Urgent"],
  "couponCode": "WELCOME10",
  "successUrl": "https://site.com/checkout/success",
  "cancelUrl": "https://site.com/checkout/cancel"
}
```

Response:

```json
{
  "orderId": "uuid",
  "provider": "Stripe",
  "checkoutSessionId": "cs_xxx",
  "checkoutUrl": "https://checkout.stripe.com/..."
}
```

## Webhook

```txt
POST /api/v1/webhooks/payments/{provider}
```

Rules:

- Verify signature.
- Store raw provider event.
- Idempotent processing.
- Activate package/promotion only after webhook.
- Generate invoice after payment.

## Orders / Payments / Invoices

```txt
GET /me/orders
GET /me/orders/{id}
GET /me/payments
GET /me/invoices
GET /me/invoices/{id}
GET /me/invoices/{id}/download

GET /admin/orders
GET /admin/payments
GET /admin/invoices
POST /admin/payments/{id}/refund
```
