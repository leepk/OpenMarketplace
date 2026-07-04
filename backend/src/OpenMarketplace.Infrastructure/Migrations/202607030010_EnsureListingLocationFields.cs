using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenMarketplace.Infrastructure.Migrations;

public partial class EnsureListingLocationFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
ALTER TABLE listings ADD COLUMN IF NOT EXISTS "AddressLine" text NOT NULL DEFAULT '';
ALTER TABLE listings ADD COLUMN IF NOT EXISTS "City" text NOT NULL DEFAULT '';
ALTER TABLE listings ADD COLUMN IF NOT EXISTS "State" text NOT NULL DEFAULT '';
ALTER TABLE listings ADD COLUMN IF NOT EXISTS "PostalCode" text NOT NULL DEFAULT '';
ALTER TABLE listings ADD COLUMN IF NOT EXISTS "Country" text NOT NULL DEFAULT 'US';
ALTER TABLE listings ADD COLUMN IF NOT EXISTS "LocationSource" text NOT NULL DEFAULT 'Manual';
ALTER TABLE listings ADD COLUMN IF NOT EXISTS "LocationPrecision" text NOT NULL DEFAULT 'ApproximateCity';
ALTER TABLE listings ADD COLUMN IF NOT EXISTS "HideExactLocation" boolean NOT NULL DEFAULT true;

UPDATE listings
SET "City" = CASE WHEN COALESCE("City", '') = '' THEN COALESCE("Location", '') ELSE "City" END,
    "LocationPrecision" = CASE WHEN "Latitude" IS NOT NULL AND "Longitude" IS NOT NULL THEN "ApproximateCity" ELSE "LocationPrecision" END
WHERE COALESCE("Location", '') <> '';

CREATE INDEX IF NOT EXISTS "IX_listings_Latitude_Longitude" ON listings ("Latitude", "Longitude");
CREATE INDEX IF NOT EXISTS "IX_listings_City_State" ON listings ("City", "State");
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
DROP INDEX IF EXISTS "IX_listings_City_State";
DROP INDEX IF EXISTS "IX_listings_Latitude_Longitude";
ALTER TABLE listings DROP COLUMN IF EXISTS "HideExactLocation";
ALTER TABLE listings DROP COLUMN IF EXISTS "LocationPrecision";
ALTER TABLE listings DROP COLUMN IF EXISTS "LocationSource";
ALTER TABLE listings DROP COLUMN IF EXISTS "Country";
ALTER TABLE listings DROP COLUMN IF EXISTS "PostalCode";
ALTER TABLE listings DROP COLUMN IF EXISTS "State";
ALTER TABLE listings DROP COLUMN IF EXISTS "City";
ALTER TABLE listings DROP COLUMN IF EXISTS "AddressLine";
""");
    }
}
