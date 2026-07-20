# Vunoca eBay Browse + Partner Network integration

## Admin DB settings

Open Admin > Site Settings and configure these keys:

- `external.ebay.enabled`: `true`
- `external.ebay.client_id`: eBay Production Client ID (App ID)
- `external.ebay.client_secret`: eBay Production Client Secret (Cert ID). Secret is preserved when the field is left blank.
- `external.ebay.campaign_id`: eBay Partner Network campaign ID
- `external.ebay.marketplace_id`: `EBAY_US`
- `external.ebay.minimum_local_results`: `10`
- `external.ebay.maximum_results`: `100`
- `external.ebay.cache_minutes`: `30`

## API endpoint

Search by keyword (up to 100 results):

`GET /api/v1/external-listings/ebay/search?q=iphone&postalCode=95051&limit=100`

Preload one category without a keyword:

`GET /api/v1/external-listings/ebay/search?categoryId=9355&postalCode=95051&limit=100&force=true`

The endpoint requires either `q` or `categoryId`. It never requests more than 100 items per call.

Behavior:

1. Counts published Vunoca listings matching the query.
2. If the local count is at least `minimum_local_results`, eBay is not called.
3. Otherwise it requests an eBay application OAuth token, searches Browse API, and returns external listing DTOs.
4. `itemAffiliateWebUrl` is preferred over `itemWebUrl`, so clicks can be credited to the EPN campaign.
5. OAuth tokens and search results are kept in memory cache.
6. eBay listings are not inserted into the Vunoca `listings` table.

For admin testing only, add `force=true` to call eBay even when enough local results exist.

## External provider priority pipeline (v46)

Customer continues to call only `GET /api/v1/listings`. The backend reads enabled providers,
sorts them by `external.{provider}.priority` (ascending), and stops after
`external.maximum_results` items. Local listings are always returned first and providers are
skipped when the local count reaches `external.minimum_local_results`.

Configured provider codes: `ebay`, `amazon`, `walmart`, and `aliexpress`. eBay has the live API
adapter. The other three are represented in Admin/settings and in the priority pipeline so their
API adapters can be added without changing Customer or the listings endpoint.
