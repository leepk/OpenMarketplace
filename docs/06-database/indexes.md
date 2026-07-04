# Indexes

Important V1 indexes:

```txt
users(email_normalized)
users(phone)
listings(status, category_id, created_at)
listings(seller_id, status)
listing_locations(latitude, longitude)
listing_promotions(listing_id, status, starts_at, ends_at)
orders(user_id, status, created_at)
payments(provider, provider_payment_id)
notifications(user_id, is_read, created_at)
conversations(buyer_id, last_message_at)
conversations(seller_id, last_message_at)
messages(conversation_id, created_at)
ads(placement_id, status, starts_at, ends_at)
listing_likes(listing_id, user_id) unique
listing_favorites(listing_id, user_id) unique
```

Future geo search:

```txt
PostGIS geography(Point, 4326)
```
