# API Documentation

## Status

Reviewed Draft

## Scope Format

Each document separates:

```txt
Long-term Vision
V1 Scope
Future Scope
Not in V1
```


## Base URL

```txt
/api/v1
```

## API Groups

```txt
/auth
/me
/users
/categories
/listings
/media
/feed
/search
/map
/messages
/notifications
/trust
/engagement
/packages
/checkout
/orders
/payments
/invoices
/promotions
/ads
/reports
/cms
/seo
/admin
/webhooks/payments/{provider}
```

## Response Format

```json
{
  "success": true,
  "data": {},
  "error": null,
  "traceId": "string"
}
```
