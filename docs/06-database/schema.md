# Database Schema

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


## Core Table Groups

### Identity

```txt
users
roles
permissions
user_roles
role_permissions
refresh_tokens
login_history
```

### Users / Trust

```txt
user_profiles
user_addresses
user_settings
user_verifications
user_badges
user_trust_scores
user_devices
```

### Media

```txt
media
media_variants
user_avatars
seller_store_media
category_media
listing_images
message_attachments
verification_documents
invoice_files
ad_creatives
```

### Listings

```txt
categories
category_attributes
listing_types
listings
listing_attributes
listing_locations
listing_stats
listing_views
listing_favorites
listing_likes
listing_comments
listing_reports
```

### Reviews

```txt
reviews
review_replies
review_reports
```

### Communication

```txt
conversations
conversation_members
messages
message_reads
notifications
notification_templates
notification_deliveries
```

### Commerce

```txt
packages
package_features
orders
order_items
payments
payment_provider_events
payment_events
invoices
invoice_items
refunds
coupons
coupon_redemptions
billing_profiles
```

### Promotions / Ads

```txt
promotion_types
listing_promotions
promotion_history
ad_campaigns
ad_placements
ads
ad_impressions
ad_clicks
```

### Moderation / Reports

```txt
moderation_cases
moderation_rules
moderation_actions
moderation_ai_scores
content_flags
reports
```

### SEO / CMS / Email / Settings

```txt
seo_metadata
redirect_rules
cms_pages
email_templates
email_queue
email_logs
app_settings
feature_flags
audit_logs
background_jobs_log
```
