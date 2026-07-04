# Security

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


## V1 Security

- JWT authentication
- Refresh token
- Role/permission tables
- Owner checks
- Admin permission checks
- Basic rate limiting
- Audit logs
- Login history
- File validation
- Stripe webhook signature validation
- Private media protection

## Sensitive Data

Do not store:

- Full card number
- CVC
- Raw card data

Private files:

- Verification documents
- Internal admin evidence
- Invoice source if private

## Future

- MFA
- Captcha
- IP reputation
- SSO
- Privacy export/delete
