using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenMarketplace.Infrastructure.Migrations;

[Migration("202607040002_CleanAdSeedRealImages")]
public partial class CleanAdSeedRealImages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
UPDATE ad_placements
SET ""IsActive"" = true,
    ""Name"" = CASE ""Code""
        WHEN 'HOME_HERO' THEN 'Homepage Hero'
        WHEN 'HOME_FEED' THEN 'Homepage Feed Bottom'
        WHEN 'SIDEBAR' THEN 'SIDEBAR'
        ELSE ""Name""
    END,
    ""InsertEvery"" = CASE ""Code""
        WHEN 'HOME_FEED' THEN 6
        ELSE 1
    END,
    ""IsDeleted"" = false,
    ""UpdatedAt"" = now()
WHERE ""Code"" IN ('HOME_HERO', 'HOME_FEED', 'SIDEBAR');

DELETE FROM ad_placements
WHERE ""Code"" NOT IN ('HOME_HERO', 'HOME_FEED', 'SIDEBAR');

DELETE FROM ad_creatives
WHERE ""Placement"" NOT IN ('HOME_HERO', 'HOME_FEED', 'SIDEBAR')
   OR ""Title"" NOT IN (
        'Local Marketplace Deals',
        'Trusted Local Services',
        'Weekend Moving Help',
        'Electronics Sale',
        'Best Electronics Picks',
        'Home Service Pros'
   );

UPDATE ad_creatives
SET ""DesktopImageUrl"" = CASE ""Title""
    WHEN 'Local Marketplace Deals' THEN 'https://images.unsplash.com/photo-1556742049-0cfed4f6a45d?auto=format&fit=crop&w=1600&q=80'
    WHEN 'Trusted Local Services' THEN 'https://images.unsplash.com/photo-1521791136064-7986c2920216?auto=format&fit=crop&w=1600&q=80'
    WHEN 'Weekend Moving Help' THEN 'https://images.unsplash.com/photo-1600585154340-be6161a56a0c?auto=format&fit=crop&w=1400&q=80'
    WHEN 'Electronics Sale' THEN 'https://images.unsplash.com/photo-1516321318423-f06f85e504b3?auto=format&fit=crop&w=1400&q=80'
    WHEN 'Best Electronics Picks' THEN 'https://images.unsplash.com/photo-1517336714731-489689fd1ca8?auto=format&fit=crop&w=900&q=80'
    WHEN 'Home Service Pros' THEN 'https://images.unsplash.com/photo-1581578731548-c64695cc6952?auto=format&fit=crop&w=900&q=80'
    ELSE ""DesktopImageUrl""
END,
""MobileImageUrl"" = CASE ""Title""
    WHEN 'Local Marketplace Deals' THEN 'https://images.unsplash.com/photo-1556742049-0cfed4f6a45d?auto=format&fit=crop&w=1600&q=80'
    WHEN 'Trusted Local Services' THEN 'https://images.unsplash.com/photo-1521791136064-7986c2920216?auto=format&fit=crop&w=1600&q=80'
    WHEN 'Weekend Moving Help' THEN 'https://images.unsplash.com/photo-1600585154340-be6161a56a0c?auto=format&fit=crop&w=1400&q=80'
    WHEN 'Electronics Sale' THEN 'https://images.unsplash.com/photo-1516321318423-f06f85e504b3?auto=format&fit=crop&w=1400&q=80'
    WHEN 'Best Electronics Picks' THEN 'https://images.unsplash.com/photo-1517336714731-489689fd1ca8?auto=format&fit=crop&w=900&q=80'
    WHEN 'Home Service Pros' THEN 'https://images.unsplash.com/photo-1581578731548-c64695cc6952?auto=format&fit=crop&w=900&q=80'
    ELSE ""MobileImageUrl""
END,
""UpdatedAt"" = now()
WHERE ""Title"" IN (
    'Local Marketplace Deals',
    'Trusted Local Services',
    'Weekend Moving Help',
    'Electronics Sale',
    'Best Electronics Picks',
    'Home Service Pros'
);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
