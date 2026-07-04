using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OpenMarketplace.Infrastructure.Persistence;

#nullable disable

namespace OpenMarketplace.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202607030006_EnsureListingPackageFields")]
public partial class EnsureListingPackageFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
ALTER TABLE listings ADD COLUMN IF NOT EXISTS ""PackageCode"" text NOT NULL DEFAULT 'FREE';
ALTER TABLE listings ADD COLUMN IF NOT EXISTS ""PackageStatus"" text NOT NULL DEFAULT 'Active';
ALTER TABLE listings ADD COLUMN IF NOT EXISTS ""PackageStartsAt"" timestamptz NULL;
ALTER TABLE listings ADD COLUMN IF NOT EXISTS ""PackageEndsAt"" timestamptz NULL;
UPDATE listings SET ""PackageCode"" = CASE
    WHEN COALESCE(""IsPinned"", false) = true THEN 'PREMIUM'
    WHEN COALESCE(""IsUrgent"", false) = true THEN 'URGENT'
    WHEN COALESCE(""IsFeatured"", false) = true THEN 'FEATURED'
    ELSE COALESCE(NULLIF(""PackageCode"", ''), 'FREE')
END;
UPDATE listings SET ""PackageStatus"" = COALESCE(NULLIF(""PackageStatus"", ''), 'Active');
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
