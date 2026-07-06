using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenMarketplace.Infrastructure.Migrations;

[Migration("202607040001_CleanAdvertisementSeedData")]
public partial class CleanAdvertisementSeedData : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
-- Keep only active placement codes that the customer site actually renders.
DELETE FROM ad_placements
WHERE ""Code"" NOT IN ('HOME_HERO', 'HOME_FEED', 'LISTING_DETAIL', 'SIDEBAR', 'SELLER_PROFILE');

UPDATE ad_placements SET
    ""Name"" = CASE ""Code""
        WHEN 'HOME_HERO' THEN 'Homepage Hero'
        WHEN 'HOME_FEED' THEN 'Homepage Feed Inline'
        WHEN 'LISTING_DETAIL' THEN 'Listing Detail'
        WHEN 'SIDEBAR' THEN 'SIDEBAR'
        WHEN 'SELLER_PROFILE' THEN 'Seller Profile'
        ELSE ""Name""
    END,
    ""InsertEvery"" = CASE ""Code""
        WHEN 'HOME_FEED' THEN 6
        ELSE 1
    END,
    ""IsActive"" = true,
    ""IsDeleted"" = false;

-- Remove old demo creatives from unused placements. DatabaseSeeder will upsert exactly 2 seeded ads per active placement with image URLs.
DELETE FROM ad_creatives
WHERE ""Placement"" NOT IN ('HOME_HERO', 'HOME_FEED', 'LISTING_DETAIL', 'SIDEBAR', 'SELLER_PROFILE')
  AND ""CampaignId"" IN (
      SELECT ""Id"" FROM ad_campaigns
      WHERE ""Name"" IN (
          'Summer Marketplace Deals 2026',
          'South Bay Local Services',
          'Real Estate Weekend',
          'Auto Buyer Week',
          'OpenMarketplace Seller Growth',
          'Safety Trust Campaign',
          'OpenMarketplace Demo Ads'
      )
  );
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
