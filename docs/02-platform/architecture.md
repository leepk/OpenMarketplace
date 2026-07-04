# Architecture

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


## Architecture Style

V1 uses Modular Monolith.

```txt
Customer Web  ┐
Admin Web     ├── ASP.NET Core API ── PostgreSQL
Worker        ┘
```

## Why Modular Monolith

- Easier to build V1.
- Easier to deploy.
- Keeps clean domain boundaries.
- Allows future extraction if scale requires it.

## Core Principles

- Domain-first structure.
- Provider pattern for external integrations.
- Media domain independent from listings.
- Commerce domain independent from Stripe.
- Feed Engine independent from Listing CRUD.
- Moderation before public visibility.
- Admin actions audited.

## Future

- Split AI service
- OpenSearch
- Redis
- Queue service
- Multi-tenant
- Native mobile
