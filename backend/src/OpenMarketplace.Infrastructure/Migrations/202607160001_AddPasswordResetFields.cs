using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenMarketplace.Infrastructure.Migrations;

public partial class AddPasswordResetFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
ALTER TABLE user_profiles ADD COLUMN IF NOT EXISTS "PasswordResetTokenHash" text NOT NULL DEFAULT '';
ALTER TABLE user_profiles ADD COLUMN IF NOT EXISTS "PasswordResetExpiresAt" timestamptz NULL;
CREATE INDEX IF NOT EXISTS "IX_user_profiles_PasswordResetTokenHash"
    ON user_profiles ("PasswordResetTokenHash");
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
DROP INDEX IF EXISTS "IX_user_profiles_PasswordResetTokenHash";
ALTER TABLE user_profiles DROP COLUMN IF EXISTS "PasswordResetTokenHash";
ALTER TABLE user_profiles DROP COLUMN IF EXISTS "PasswordResetExpiresAt";
""");
    }
}
