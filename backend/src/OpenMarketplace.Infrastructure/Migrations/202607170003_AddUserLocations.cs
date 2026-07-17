using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OpenMarketplace.Infrastructure.Migrations;
public partial class AddUserLocations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
CREATE TABLE IF NOT EXISTS user_locations (
 "Id" uuid NOT NULL PRIMARY KEY, "UserId" uuid NOT NULL, "Label" text NOT NULL DEFAULT '', "AddressLine" text NOT NULL DEFAULT '',
 "City" text NOT NULL DEFAULT '', "State" text NOT NULL DEFAULT '', "PostalCode" text NOT NULL DEFAULT '', "Country" text NOT NULL DEFAULT 'US',
 "Latitude" numeric NULL, "Longitude" numeric NULL, "UseCount" integer NOT NULL DEFAULT 0, "LastUsedAt" timestamptz NULL, "IsDefault" boolean NOT NULL DEFAULT false,
 "CreatedAt" timestamptz NOT NULL DEFAULT now(), "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL DEFAULT false, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE INDEX IF NOT EXISTS "IX_user_locations_UserId_LastUsedAt" ON user_locations ("UserId", "LastUsedAt" DESC);
CREATE INDEX IF NOT EXISTS "IX_user_locations_UserId_IsDefault" ON user_locations ("UserId", "IsDefault");
""");
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("DROP TABLE IF EXISTS user_locations;");
}
