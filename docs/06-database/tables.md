# Important Tables

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


## users

```txt
id uuid
email varchar(255)
email_normalized varchar(255)
phone varchar(50)
password_hash text
status varchar(50)
trust_score int
last_login_at timestamptz
created_at timestamptz
updated_at timestamptz
```

## user_profiles

```txt
id uuid
user_id uuid
display_name varchar(150)
first_name varchar(100)
last_name varchar(100)
avatar_media_id uuid
banner_media_id uuid
avatar_url text
bio text
seller_type varchar(50)
rating numeric(3,2)
review_count int
response_rate numeric(5,2)
member_since timestamptz
created_at timestamptz
updated_at timestamptz
```

## media

```txt
id uuid
owner_user_id uuid
media_type varchar(50)
purpose varchar(100)
file_name text
original_file_name text
mime_type varchar(100)
extension varchar(20)
size_bytes bigint
storage_provider varchar(50)
storage_bucket text
storage_path text
public_url text
private_url text
checksum_sha256 text
width int
height int
moderation_status varchar(50)
visibility varchar(50)
metadata_json jsonb
created_at timestamptz
updated_at timestamptz
is_deleted boolean
```

## listings

```txt
id uuid
listing_type_id uuid
category_id uuid
seller_id uuid
title varchar(200)
slug varchar(250)
description text
price numeric(12,2)
currency varchar(10)
status varchar(50)
condition varchar(50)
expires_at timestamptz
published_at timestamptz
created_at timestamptz
updated_at timestamptz
is_deleted boolean
```

## listing_stats

```txt
id uuid
listing_id uuid
view_count int
favorite_count int
like_count int
comment_count int
message_count int
share_count int
report_count int
last_viewed_at timestamptz
updated_at timestamptz
```

## orders

```txt
id uuid
order_no varchar(50)
user_id uuid
status varchar(50)
subtotal numeric(12,2)
tax numeric(12,2)
discount numeric(12,2)
total numeric(12,2)
currency varchar(10)
created_at timestamptz
paid_at timestamptz
cancelled_at timestamptz
```

## payments

```txt
id uuid
order_id uuid
user_id uuid
provider varchar(50)
provider_payment_id text
provider_session_id text
status varchar(50)
amount numeric(12,2)
currency varchar(10)
paid_at timestamptz
failed_at timestamptz
raw_response jsonb
created_at timestamptz
```

## moderation_cases

```txt
id uuid
content_type varchar(50)
content_id uuid
user_id uuid
status varchar(50)
risk_score int
reason text
assigned_to uuid
created_at timestamptz
resolved_at timestamptz
```
