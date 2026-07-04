# Communication API

## Conversations

```txt
GET  /api/v1/messages/conversations
POST /api/v1/messages/conversations
GET  /api/v1/messages/conversations/{id}
```

Create conversation request:

```json
{
  "listingId": "uuid",
  "sellerId": "uuid",
  "initialMessage": "Is this available?"
}
```

## Messages

```txt
POST /messages/conversations/{id}/messages
POST /messages/conversations/{id}/read
POST /messages/conversations/{id}/report
```

Message request:

```json
{
  "body": "Can I pick it up today?",
  "attachments": []
}
```

## Notifications

```txt
GET   /notifications
GET   /notifications/unread-count
PATCH /notifications/{id}/read
PATCH /notifications/read-all
DELETE /notifications/{id}
```
