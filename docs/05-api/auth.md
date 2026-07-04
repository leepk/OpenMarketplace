# Auth API

## Register

```txt
POST /api/v1/auth/register
```

Request:

```json
{
  "displayName": "John Smith",
  "email": "john@example.com",
  "phone": "+14085551234",
  "password": "P@ssword123",
  "confirmPassword": "P@ssword123"
}
```

Response:

```json
{
  "id": "uuid",
  "email": "john@example.com",
  "status": "PendingEmail"
}
```

## Login

```txt
POST /api/v1/auth/login
```

Request:

```json
{
  "email": "john@example.com",
  "password": "P@ssword123"
}
```

Response:

```json
{
  "accessToken": "jwt",
  "refreshToken": "token",
  "expiresIn": 3600,
  "user": {
    "id": "uuid",
    "displayName": "John Smith",
    "roles": ["Customer"]
  }
}
```

## Other Endpoints

```txt
POST /auth/logout
POST /auth/refresh-token
POST /auth/verify-email
POST /auth/send-phone-otp
POST /auth/verify-phone-otp
POST /auth/forgot-password
POST /auth/reset-password
```

## Rules

- Email verification required.
- Phone verification required for posting.
- Refresh token can be revoked.
