using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OpenMarketplace.Infrastructure.Migrations;

public partial class AddPhoneVerificationFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
ALTER TABLE user_profiles ADD COLUMN IF NOT EXISTS "PhoneVerificationCodeHash" text NOT NULL DEFAULT '';
ALTER TABLE user_profiles ADD COLUMN IF NOT EXISTS "PhoneVerificationExpiresAt" timestamptz NULL;
ALTER TABLE user_profiles ADD COLUMN IF NOT EXISTS "PhoneVerificationSentAt" timestamptz NULL;
ALTER TABLE user_profiles ADD COLUMN IF NOT EXISTS "PhoneVerificationAttempts" integer NOT NULL DEFAULT 0;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
ALTER TABLE user_profiles DROP COLUMN IF EXISTS "PhoneVerificationCodeHash";
ALTER TABLE user_profiles DROP COLUMN IF EXISTS "PhoneVerificationExpiresAt";
ALTER TABLE user_profiles DROP COLUMN IF EXISTS "PhoneVerificationSentAt";
ALTER TABLE user_profiles DROP COLUMN IF EXISTS "PhoneVerificationAttempts";
""");
    }
}
