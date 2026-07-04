# Business Rules

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


## Listing Rules

- Phone verification is required before posting.
- Listing is not public until moderation allows it.
- Expired listing is removed from public feed.
- Rejected listing can show reason and allow edit if configured.
- Sold listing should not be promoted.
- Public listing location may be approximate.

## Commerce Rules

- Payment success URL is display-only.
- Webhook is source of truth.
- Every paid action creates order, payment, invoice.
- Packages and features are configurable.
- Promotions are stored separately from listings.
- Trust badges cannot be purchased.

## Ads Rules

- Sponsored content must be labeled.
- Ads cannot look like user messages.
- Ad placement is configurable.
- Feed ad injection is server-controlled.
- Impression/click must be logged.

## Media Rules

- All uploads go through Media Service.
- Private files use private/signed access.
- Listing feed loads thumbnails, not originals.
- EXIF GPS metadata is stripped.

## Moderation Rules

- V1 requires basic spam/violation checks.
- Reported content creates review queue.
- Admin moderation actions are audited.

## Trust Rules

- Verified badge means verification.
- Rating/review count should be shown near seller.
- Internal risk score should not be exposed raw.
