using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenMarketplace.Infrastructure.Migrations;

[Migration("202607030008_RepairAdPlacementsKeepData")]
public partial class RepairAdPlacementsKeepData : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
-- Repair ads data without deleting creatives and without renaming ad_placements.Code into duplicate values.
UPDATE ad_creatives SET ""Placement"" = 'HOME_HERO' WHERE ""Placement"" IN ('HomeHero','HOMEHERO','HOME_HERO','HeroSearch','HEROSEARCH','HERO_SEARCH','SearchHero','SEARCHHERO','SEARCH_HERO','HomeSearch','HOMESEARCH','HOME_SEARCH','SEARCH_FEED','SearchFeed');
UPDATE ad_creatives SET ""Placement"" = 'HOME_FEED' WHERE ""Placement"" IN ('HomeFeed','HOMEFEED','HOME_FEED');
UPDATE ad_creatives SET ""Placement"" = 'LISTING_DETAIL' WHERE ""Placement"" IN ('ListingDetail','LISTINGDETAIL','DETAIL','LISTING_DETAIL');
UPDATE ad_creatives SET ""Placement"" = 'SIDEBAR' WHERE ""Placement"" IN ('Sidebar','SIDEBAR');
UPDATE ad_creatives SET ""Placement"" = 'SELLER_PROFILE' WHERE ""Placement"" IN ('SellerProfile','SELLERPROFILE','SELLER_PROFILE');

INSERT INTO ad_placements (""Id"", ""Code"", ""Name"", ""InsertEvery"", ""IsActive"", ""CreatedAt"", ""CreatedBy"", ""UpdatedAt"", ""UpdatedBy"", ""IsDeleted"", ""DeletedAt"", ""DeletedBy"")
VALUES
(gen_random_uuid(), 'HOME_HERO', 'HOME_HERO', 0, true, now(), null, null, null, false, null, null),
(gen_random_uuid(), 'HOME_FEED', 'HOME_FEED', 6, true, now(), null, null, null, false, null, null),
(gen_random_uuid(), 'LISTING_DETAIL', 'LISTING_DETAIL', 0, true, now(), null, null, null, false, null, null),
(gen_random_uuid(), 'SIDEBAR', 'SIDEBAR', 0, true, now(), null, null, null, false, null, null),
(gen_random_uuid(), 'SELLER_PROFILE', 'SELLER_PROFILE', 0, true, now(), null, null, null, false, null, null)
ON CONFLICT (""Code"") DO UPDATE SET ""Name"" = EXCLUDED.""Name"", ""InsertEvery"" = EXCLUDED.""InsertEvery"", ""IsActive"" = true, ""IsDeleted"" = false;

UPDATE ad_placements SET ""IsActive"" = false
WHERE ""Code"" IN (
    'HomeHero','HOMEHERO','HeroSearch','HEROSEARCH','HERO_SEARCH','SearchHero','SEARCHHERO','SEARCH_HERO','HomeSearch','HOMESEARCH','HOME_SEARCH','SEARCH_FEED','SearchFeed',
    'HomeFeed','HOMEFEED',
    'ListingDetail','LISTINGDETAIL','DETAIL',
    'Sidebar',
    'SellerProfile','SELLERPROFILE',
    'HOME_FEATURED','HomeFeatured','FOOTER','Footer','BILLING','Billing','MESSAGES','Messages','NOTIFICATIONS','Notifications'
);
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
