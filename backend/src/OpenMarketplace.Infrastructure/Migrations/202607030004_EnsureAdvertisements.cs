using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OpenMarketplace.Infrastructure.Persistence;

#nullable disable

namespace OpenMarketplace.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202607030004_EnsureAdvertisements")]
public partial class EnsureAdvertisements : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
CREATE TABLE IF NOT EXISTS ad_campaigns (
    "Id" uuid PRIMARY KEY,
    "Name" text NOT NULL DEFAULT '',
    "Status" text NOT NULL DEFAULT 'Active',
    "StartDate" timestamptz NOT NULL DEFAULT now(),
    "EndDate" timestamptz NOT NULL DEFAULT now() + interval '30 days',
    "Priority" integer NOT NULL DEFAULT 100,
    "Budget" numeric NULL,
    "CreatedAt" timestamptz NOT NULL DEFAULT now(),
    "CreatedBy" uuid NULL,
    "UpdatedAt" timestamptz NULL,
    "UpdatedBy" uuid NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamptz NULL,
    "DeletedBy" uuid NULL
);

CREATE TABLE IF NOT EXISTS ad_creatives (
    "Id" uuid PRIMARY KEY,
    "CampaignId" uuid NOT NULL,
    "Placement" text NOT NULL DEFAULT 'HomeHero',
    "Title" text NOT NULL DEFAULT '',
    "Description" text NOT NULL DEFAULT '',
    "DesktopImageUrl" text NOT NULL DEFAULT '',
    "MobileImageUrl" text NOT NULL DEFAULT '',
    "TargetUrl" text NOT NULL DEFAULT '',
    "OpenInNewTab" boolean NOT NULL DEFAULT true,
    "SortOrder" integer NOT NULL DEFAULT 0,
    "Status" text NOT NULL DEFAULT 'Active',
    "MaxImpressions" integer NOT NULL DEFAULT 0,
    "CurrentImpressions" integer NOT NULL DEFAULT 0,
    "MaxClicks" integer NOT NULL DEFAULT 0,
    "CurrentClicks" integer NOT NULL DEFAULT 0,
    "CreatedAt" timestamptz NOT NULL DEFAULT now(),
    "CreatedBy" uuid NULL,
    "UpdatedAt" timestamptz NULL,
    "UpdatedBy" uuid NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamptz NULL,
    "DeletedBy" uuid NULL
);

CREATE TABLE IF NOT EXISTS ad_statistics (
    "Id" uuid PRIMARY KEY,
    "CreativeId" uuid NOT NULL,
    "Date" date NOT NULL DEFAULT CURRENT_DATE,
    "Impressions" integer NOT NULL DEFAULT 0,
    "Clicks" integer NOT NULL DEFAULT 0,
    "CreatedAt" timestamptz NOT NULL DEFAULT now(),
    "CreatedBy" uuid NULL,
    "UpdatedAt" timestamptz NULL,
    "UpdatedBy" uuid NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamptz NULL,
    "DeletedBy" uuid NULL
);

CREATE INDEX IF NOT EXISTS "IX_ad_campaigns_Status_StartDate_EndDate" ON ad_campaigns ("Status", "StartDate", "EndDate");
CREATE INDEX IF NOT EXISTS "IX_ad_creatives_Placement_Status_SortOrder" ON ad_creatives ("Placement", "Status", "SortOrder");
CREATE INDEX IF NOT EXISTS "IX_ad_statistics_CreativeId_Date" ON ad_statistics ("CreativeId", "Date");
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS ad_statistics; DROP TABLE IF EXISTS ad_creatives; DROP TABLE IF EXISTS ad_campaigns;");
    }
}
