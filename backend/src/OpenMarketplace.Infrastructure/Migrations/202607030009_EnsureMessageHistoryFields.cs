using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenMarketplace.Infrastructure.Migrations;

public partial class EnsureMessageHistoryFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Keep this migration DDL-only. Some existing databases had a partially-applied
        // message schema, so backfill is handled by the startup schema repair after all
        // columns are guaranteed to exist.
        migrationBuilder.Sql("""
ALTER TABLE conversations ADD COLUMN IF NOT EXISTS "LastMessageId" uuid NULL;
ALTER TABLE conversations ADD COLUMN IF NOT EXISTS "LastMessagePreview" text NOT NULL DEFAULT '';

ALTER TABLE messages ADD COLUMN IF NOT EXISTS "ReceiverId" uuid NULL;
ALTER TABLE messages ADD COLUMN IF NOT EXISTS "MessageType" text NOT NULL DEFAULT 'Text';
ALTER TABLE messages ADD COLUMN IF NOT EXISTS "ReadAt" timestamptz NULL;
ALTER TABLE messages ADD COLUMN IF NOT EXISTS "MetadataJson" text NOT NULL DEFAULT '{}';

CREATE INDEX IF NOT EXISTS "IX_messages_ConversationId_CreatedAt" ON messages ("ConversationId", "CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_messages_ReceiverId" ON messages ("ReceiverId");
CREATE INDEX IF NOT EXISTS "IX_conversations_LastMessageAt" ON conversations ("LastMessageAt");
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
