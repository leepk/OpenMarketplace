using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OpenMarketplace.Infrastructure.Persistence;

#nullable disable

namespace OpenMarketplace.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202607030003_EnsurePaymentProviders")]
public partial class EnsurePaymentProviders : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
CREATE TABLE IF NOT EXISTS payment_providers (
    "Id" uuid PRIMARY KEY,
    "Code" text NOT NULL,
    "Name" text NOT NULL,
    "Type" text NOT NULL DEFAULT 'Test',
    "DisplayName" text NOT NULL DEFAULT '',
    "IsEnabled" boolean NOT NULL DEFAULT true,
    "IsTestMode" boolean NOT NULL DEFAULT true,
    "Currency" text NOT NULL DEFAULT 'USD',
    "SortOrder" integer NOT NULL DEFAULT 0,
    "ConfigJson" text NOT NULL DEFAULT '{}',
    "PublicConfigJson" text NOT NULL DEFAULT '{}',
    "CreatedAt" timestamptz NOT NULL DEFAULT now(),
    "CreatedBy" uuid NULL,
    "UpdatedAt" timestamptz NULL,
    "UpdatedBy" uuid NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamptz NULL,
    "DeletedBy" uuid NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_payment_providers_Code" ON payment_providers ("Code");
ALTER TABLE orders ADD COLUMN IF NOT EXISTS "Provider" text NOT NULL DEFAULT 'Test';
ALTER TABLE orders ADD COLUMN IF NOT EXISTS "ProviderReference" text NOT NULL DEFAULT '';
ALTER TABLE payments ADD COLUMN IF NOT EXISTS "Provider" text NOT NULL DEFAULT 'Test';
ALTER TABLE payments ADD COLUMN IF NOT EXISTS "ProviderReference" text NOT NULL DEFAULT '';
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS payment_providers;");
    }
}
