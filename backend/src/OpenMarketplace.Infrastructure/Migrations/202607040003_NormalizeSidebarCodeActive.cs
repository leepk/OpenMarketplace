using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenMarketplace.Infrastructure.Migrations;

[Migration("202607040003_NormalizeSidebarCodeActive")]
public partial class NormalizeSidebarCodeActive : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
UPDATE ad_creatives
SET "Placement" = 'SIDEBAR',
    "IsActive" = true,
    "IsDeleted" = false,
    "UpdatedAt" = now()
WHERE "Placement" IN ('Sidebar', 'sidebar', 'SIDEBAR');

UPDATE ad_placements
SET "Code" = 'SIDEBAR',
    "Name" = 'SIDEBAR',
    "IsActive" = true,
    "IsDeleted" = false,
    "UpdatedAt" = now()
WHERE "Code" IN ('Sidebar', 'sidebar', 'SIDEBAR')
   OR "Name" IN ('Sidebar', 'sidebar', 'SIDEBAR');

DELETE FROM ad_placements a
USING ad_placements b
WHERE a."Code" = b."Code"
  AND a."CreatedAt" > b."CreatedAt"
  AND a."Code" = 'SIDEBAR';

UPDATE ad_placements
SET "Name" = 'SIDEBAR',
    "IsActive" = true,
    "IsDeleted" = false,
    "UpdatedAt" = now()
WHERE "Code" = 'SIDEBAR';
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
