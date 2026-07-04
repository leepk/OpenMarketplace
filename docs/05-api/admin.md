# Admin API

## Dashboard

```txt
GET /admin/dashboard
GET /admin/dashboard/summary
```

## Listings

```txt
GET  /admin/listings
GET  /admin/listings/{id}
POST /admin/listings/{id}/approve
POST /admin/listings/{id}/reject
POST /admin/listings/{id}/request-edit
POST /admin/listings/{id}/remove
```

Reject request:

```json
{
  "reason": "Image violates marketplace policy.",
  "notifyUser": true
}
```

## Users

```txt
GET  /admin/users
GET  /admin/users/{id}
POST /admin/users/{id}/suspend
POST /admin/users/{id}/ban
POST /admin/users/{id}/restore
```

## Moderation

```txt
GET  /admin/moderation/cases
GET  /admin/moderation/cases/{id}
POST /admin/moderation/cases/{id}/approve
POST /admin/moderation/cases/{id}/reject
POST /admin/moderation/cases/{id}/escalate
```

## Comments / Reviews / Trust

```txt
GET  /admin/comments
POST /admin/comments/{id}/hide
POST /admin/comments/{id}/remove

GET  /admin/reviews
POST /admin/reviews/{id}/approve
POST /admin/reviews/{id}/remove

GET  /admin/users/{id}/trust-score
POST /admin/users/{id}/badges
DELETE /admin/users/{id}/badges/{badgeId}
```

## Commerce / Ads

```txt
GET /admin/packages
POST /admin/packages
PUT /admin/packages/{id}

GET /admin/orders
GET /admin/payments
GET /admin/invoices

GET /admin/ads
POST /admin/ads
PUT /admin/ads/{id}
```
