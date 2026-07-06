using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenMarketplace.Infrastructure.Migrations;

[Migration("202607050001_EnsurePackageSortOrder")]
public partial class EnsurePackageSortOrder : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
ALTER TABLE packages ADD COLUMN IF NOT EXISTS ""SortOrder"" integer NOT NULL DEFAULT 0;
UPDATE packages SET ""SortOrder"" = CASE UPPER(""Code"")
    WHEN 'FREE' THEN 10
    WHEN 'BASIC' THEN 20
    WHEN 'URGENT' THEN 30
    WHEN 'FEATURED' THEN 40
    WHEN 'PREMIUM' THEN 50
    WHEN 'CREDITS100' THEN 60
    ELSE COALESCE(NULLIF(""SortOrder"", 0), 100)
END;
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
