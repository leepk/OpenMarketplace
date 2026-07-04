# Database Conventions

## Naming

- Tables: snake_case plural
- Columns: snake_case
- Primary key: id uuid

## Common Fields

```txt
created_at
created_by
updated_at
updated_by
is_deleted
deleted_at
deleted_by
```

## Rules

- Use soft delete for business records.
- Use audit logs for admin/sensitive changes.
- Use media_id instead of raw image URL as source of truth.
- Use provider event table for webhooks.
- Use indexes for search/filter fields.
