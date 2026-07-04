# Feed API

Feed API returns mixed feed items.

## Home Feed

```txt
GET /api/v1/feed/home
```

Query:

```txt
page
pageSize
lat
lng
radiusMiles
categoryId
```

Response:

```json
{
  "items": [
    {
      "type": "listing",
      "data": {
        "id": "uuid",
        "title": "iPhone 15",
        "price": 650
      }
    },
    {
      "type": "ad",
      "data": {
        "id": "uuid",
        "label": "Sponsored",
        "title": "Promote your listing"
      }
    }
  ],
  "page": 1,
  "pageSize": 25,
  "totalItems": 100
}
```

## Other Feed Endpoints

```txt
GET /feed/search
GET /feed/category/{slug}
GET /feed/profile/{sellerId}
```

## Feed Rules

- Server injects ads.
- Server applies promotions.
- Remove duplicates.
- Sponsored items must be labeled.
- Moderation-hidden content is excluded.
