# Listings API

## Create Listing

```txt
POST /api/v1/listings
```

Permission:

```txt
Authenticated
PhoneVerified
```

Request:

```json
{
  "listingType": "Item",
  "categoryId": "uuid",
  "title": "iPhone 15 Pro Max 256GB",
  "description": "Excellent condition, unlocked.",
  "price": 750,
  "currency": "USD",
  "condition": "Used",
  "location": {
    "city": "San Jose",
    "state": "CA",
    "zipCode": "95112",
    "latitude": 37.3382,
    "longitude": -121.8863,
    "showExactLocation": false
  },
  "attributes": {
    "brand": "Apple",
    "model": "iPhone 15 Pro Max",
    "color": "Blue"
  }
}
```

Response:

```json
{
  "id": "uuid",
  "status": "Draft",
  "nextStep": "UploadImages"
}
```

Business rules:

- Phone verification required.
- Listing starts as Draft.
- Public visibility requires submit + moderation.
- Location can be approximate.

## Submit Listing

```txt
POST /api/v1/listings/{id}/submit
```

Request:

```json
{
  "packageId": "uuid",
  "selectedBoosts": ["Urgent", "Highlighted"]
}
```

Response:

```json
{
  "listingId": "uuid",
  "status": "PendingReview",
  "requiresPayment": false
}
```

If payment required:

```json
{
  "listingId": "uuid",
  "status": "PendingPayment",
  "orderId": "uuid",
  "checkoutUrl": "https://checkout.stripe.com/..."
}
```

## Listing Detail

```txt
GET /api/v1/listings/{id}
```

Response includes:

```json
{
  "id": "uuid",
  "title": "iPhone 15 Pro Max",
  "price": 750,
  "currency": "USD",
  "status": "Published",
  "images": [],
  "seller": {
    "id": "uuid",
    "displayName": "John",
    "avatarUrl": "...",
    "badges": ["PhoneVerified"],
    "rating": 4.8,
    "reviewCount": 12
  },
  "stats": {
    "views": 230,
    "likes": 12,
    "comments": 4
  }
}
```

## Actions

```txt
PUT    /listings/{id}
DELETE /listings/{id}
POST   /listings/{id}/pause
POST   /listings/{id}/resume
POST   /listings/{id}/mark-sold
POST   /listings/{id}/favorite
DELETE /listings/{id}/favorite
POST   /listings/{id}/like
DELETE /listings/{id}/like
GET    /listings/{id}/comments
POST   /listings/{id}/comments
POST   /listings/{id}/report
```
