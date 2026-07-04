# Media API

## Upload

```txt
POST /api/v1/media/upload
```

Multipart fields:

```txt
file
purpose
entityType optional
entityId optional
```

Purpose:

```txt
ListingImage
UserAvatar
AdCreative
MessageAttachment
InvoicePdf
CmsImage
```

Response:

```json
{
  "id": "uuid",
  "purpose": "ListingImage",
  "url": "...",
  "thumbnailUrl": "...",
  "moderationStatus": "Pending"
}
```

## Listing Images

```txt
POST   /listings/{id}/images
DELETE /listings/{id}/images/{imageId}
PATCH  /listings/{id}/images/reorder
```

Rules:

- Validate file type/size.
- Generate variants.
- Strip EXIF GPS.
- Store checksum.
- Use Media Service for URL generation.
