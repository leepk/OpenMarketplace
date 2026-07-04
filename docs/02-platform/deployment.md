# Deployment

## V1

- Docker Compose
- Nginx reverse proxy
- PostgreSQL
- API container
- Customer Web container
- Admin Web container
- Worker optional
- Mounted volume for local media
- HTTPS via Certbot

## Health

- API health
- Database health
- Worker health
- Payment webhook health
- Email queue health

## Backup

- PostgreSQL backup
- Media volume backup
- Environment/secrets backup
