# API Conventions

Base path:

```txt
/api/v1
```

Response:

```json
{
  "success": true,
  "data": {},
  "error": null,
  "traceId": "string"
}
```

Paged response:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 25,
  "totalItems": 100,
  "totalPages": 4
}
```

Rules:

- Use pagination for list endpoints.
- Use idempotency for commerce/webhook endpoints.
- Admin endpoints require permissions.
- Owner endpoints check ownership.
- Validation errors identify fields.
