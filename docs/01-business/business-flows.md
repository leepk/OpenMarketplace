# Business Flows

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


## Guest Browse Flow

```txt
Open site
Search/browse categories
View feed/list
Switch map if needed
Open listing detail
Prompt login when saving/messaging/reporting
```

## Seller Listing Flow

```txt
Register
Verify email
Verify phone
Complete profile
Post listing
Choose type/category
Enter details
Upload images
Choose package
Checkout if paid
Submit
Moderation
Publish / pending review / reject
Notify seller
```

## Buyer Messaging Flow

```txt
Open listing
Click Message Seller
Login required
Create/open conversation
Send message
Message scanned for spam
Seller notified via SignalR/notification
```

## Commerce Flow

```txt
Choose package/boost
Create order
Create order items
Apply coupon/tax
Create payment provider checkout
User pays
Webhook confirms payment
Activate package/promotion
Generate invoice
Notify user
```

## Feed Flow

```txt
Home/Search/Category requests feed
Feed Engine loads eligible listings
Applies filters/ranking
Injects featured listings
Injects ads
Removes duplicates
Returns FeedItem[]
```

## Moderation Flow

```txt
New content
Rule scan
Image/file validation
Risk score
Auto approve / human review / reject
Audit decision
Notify user
```

## Trust & Engagement Flow

```txt
User views listing
User likes/comments/favorites/messages
Stats updated
Reports affect trust/risk
Reviews improve seller reputation
Feed may use trust and engagement signals
```
