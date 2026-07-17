using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OpenMarketplace.Infrastructure.Migrations;

public partial class AddCaliforniaLocalities : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
CREATE TABLE IF NOT EXISTS localities (
    "Id" uuid NOT NULL PRIMARY KEY,
    "Name" text NOT NULL DEFAULT '',
    "StateCode" text NOT NULL DEFAULT 'CA',
    "CountryCode" text NOT NULL DEFAULT 'US',
    "GeoId" text NOT NULL DEFAULT '',
    "Latitude" numeric NULL,
    "Longitude" numeric NULL,
    "SortOrder" integer NOT NULL DEFAULT 0,
    "SelectionCount" bigint NOT NULL DEFAULT 0,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamptz NOT NULL DEFAULT now(),
    "CreatedBy" uuid NULL,
    "UpdatedAt" timestamptz NULL,
    "UpdatedBy" uuid NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamptz NULL,
    "DeletedBy" uuid NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_localities_StateCode_Name" ON localities ("StateCode", "Name");
CREATE INDEX IF NOT EXISTS "IX_localities_IsActive_SortOrder_SelectionCount" ON localities ("IsActive", "SortOrder", "SelectionCount");
""");
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "localities");
}
