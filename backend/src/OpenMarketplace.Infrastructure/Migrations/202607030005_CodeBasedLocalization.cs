using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OpenMarketplace.Infrastructure.Persistence;

#nullable disable

namespace OpenMarketplace.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202607030005_CodeBasedLocalization")]
public partial class CodeBasedLocalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
ALTER TABLE categories ADD COLUMN IF NOT EXISTS "Code" text NOT NULL DEFAULT '';
ALTER TABLE categories ADD COLUMN IF NOT EXISTS "ParentCode" text NULL;
ALTER TABLE categories ADD COLUMN IF NOT EXISTS "IconKey" text NOT NULL DEFAULT 'category';

UPDATE categories
SET "Code" = CASE
    WHEN "Slug" = 'property-rentals' THEN 'property_rentals'
    WHEN "Slug" = 'for-sale' THEN 'for_sale'
    WHEN "Slug" = 'home-garden' THEN 'home_garden'
    WHEN "Slug" = 'sports-outdoors' THEN 'sports_outdoors'
    WHEN COALESCE("Code", '') = '' THEN replace(lower("Slug"), '-', '_')
    ELSE "Code"
END,
"IconKey" = CASE
    WHEN "Slug" = 'vehicles' OR "Code" = 'vehicles' THEN 'vehicle'
    WHEN "Slug" = 'property-rentals' OR "Code" = 'property_rentals' THEN 'rental'
    WHEN "Slug" = 'for-sale' OR "Code" = 'for_sale' THEN 'sale'
    WHEN "Slug" = 'jobs' OR "Code" = 'jobs' THEN 'jobs'
    WHEN "Slug" = 'services' OR "Code" = 'services' THEN 'services'
    WHEN "Slug" = 'electronics' OR "Code" = 'electronics' THEN 'electronics'
    WHEN "Slug" = 'home-garden' OR "Code" = 'home_garden' THEN 'garden'
    WHEN "Slug" = 'community' OR "Code" = 'community' THEN 'community'
    WHEN "Slug" = 'pets' OR "Code" = 'pets' THEN 'pets'
    WHEN "Slug" = 'sports-outdoors' OR "Code" = 'sports_outdoors' THEN 'sports'
    ELSE "IconKey"
END,
"Name" = CASE WHEN COALESCE("Code", '') <> '' THEN "Code" ELSE "Name" END,
"Slug" = CASE
    WHEN "Slug" = 'property_rentals' THEN 'property-rentals'
    WHEN "Slug" = 'for_sale' THEN 'for-sale'
    WHEN "Slug" = 'home_garden' THEN 'home-garden'
    WHEN "Slug" = 'sports_outdoors' THEN 'sports-outdoors'
    ELSE "Slug"
END;

CREATE UNIQUE INDEX IF NOT EXISTS "IX_categories_Code" ON categories ("Code");
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
DROP INDEX IF EXISTS "IX_categories_Code";
ALTER TABLE categories DROP COLUMN IF EXISTS "ParentCode";
ALTER TABLE categories DROP COLUMN IF EXISTS "IconKey";
ALTER TABLE categories DROP COLUMN IF EXISTS "Code";
""");
    }
}
