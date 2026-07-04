# Domain Map

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


## Domains

```txt
Identity
Users
Categories
Listings
Media
Geo
Feed
Search
Trust
Engagement
Communication
Commerce
Promotions
Advertising
Moderation
SEO
Email
CMS
Settings
Admin
```

## Domain Boundary Rules

- Listings references Media by media_id.
- Feed consumes Listings, Promotions, Ads, Trust, Engagement through services.
- Commerce activates promotions after payment.
- Moderation can hide or block content from Feed.
- Trust uses verification, reports, reviews, and moderation signals.
- Ads are separate from listing promotions.

## No Direct Random Coupling

Modules should not randomly access other modules' tables. Use services/contracts.
