using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenMarketplace.Infrastructure.Migrations;

public partial class EnsureUserRoleSourceFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
ALTER TABLE user_profiles ADD COLUMN IF NOT EXISTS "Role" text NOT NULL DEFAULT 'Customer';
ALTER TABLE user_profiles ADD COLUMN IF NOT EXISTS "Source" text NOT NULL DEFAULT 'WebCustomer';
UPDATE user_profiles SET "Role" = 'Customer' WHERE "Role" IS NULL OR trim("Role") = '';
UPDATE user_profiles SET "Source" = CASE
    WHEN "Role" IN ('Admin','SuperAdmin') THEN 'AdminCreated'
    WHEN "Role" = 'System' THEN 'SystemManaged'
    ELSE 'WebCustomer'
END WHERE "Source" IS NULL OR trim("Source") = '';
CREATE INDEX IF NOT EXISTS "IX_user_profiles_Role_Source_Status" ON user_profiles ("Role", "Source", "Status");
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
DROP INDEX IF EXISTS "IX_user_profiles_Role_Source_Status";
ALTER TABLE user_profiles DROP COLUMN IF EXISTS "Source";
""");
    }
}
