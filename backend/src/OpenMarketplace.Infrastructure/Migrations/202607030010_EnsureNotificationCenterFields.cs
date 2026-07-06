using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenMarketplace.Infrastructure.Migrations;

public partial class EnsureNotificationCenterFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
ALTER TABLE notifications ADD COLUMN IF NOT EXISTS "EntityType" text NOT NULL DEFAULT '';
ALTER TABLE notifications ADD COLUMN IF NOT EXISTS "EntityId" uuid NULL;
ALTER TABLE notifications ADD COLUMN IF NOT EXISTS "ImageUrl" text NOT NULL DEFAULT '';
ALTER TABLE notifications ADD COLUMN IF NOT EXISTS "ReadAt" timestamptz NULL;
CREATE INDEX IF NOT EXISTS "IX_notifications_UserId_Type_CreatedAt" ON notifications ("UserId", "Type", "CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_notifications_EntityType_EntityId" ON notifications ("EntityType", "EntityId");
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
DROP INDEX IF EXISTS "IX_notifications_EntityType_EntityId";
DROP INDEX IF EXISTS "IX_notifications_UserId_Type_CreatedAt";
ALTER TABLE notifications DROP COLUMN IF EXISTS "ReadAt";
ALTER TABLE notifications DROP COLUMN IF EXISTS "ImageUrl";
ALTER TABLE notifications DROP COLUMN IF EXISTS "EntityId";
ALTER TABLE notifications DROP COLUMN IF EXISTS "EntityType";
""");
    }
}
