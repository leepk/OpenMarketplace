using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenMarketplace.Infrastructure.Migrations;

[Migration("202607060001_AddOpenAiModeration")]
public partial class AddOpenAiModeration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
CREATE TABLE IF NOT EXISTS blocked_words (
    "Id" uuid NOT NULL PRIMARY KEY,
    "Word" text NOT NULL DEFAULT '',
    "NormalizedWord" text NOT NULL DEFAULT '',
    "Language" text NOT NULL DEFAULT 'Any',
    "Severity" text NOT NULL DEFAULT 'Medium',
    "MatchType" text NOT NULL DEFAULT 'Contains',
    "Category" text NOT NULL DEFAULT 'General',
    "IsActive" boolean NOT NULL DEFAULT true,
    "Notes" text NOT NULL DEFAULT '',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "UpdatedAt" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS "IX_blocked_words_IsActive_NormalizedWord" ON blocked_words ("IsActive", "NormalizedWord");

CREATE TABLE IF NOT EXISTS listing_moderation_results (
    "Id" uuid NOT NULL PRIMARY KEY,
    "ListingId" uuid NOT NULL,
    "Source" text NOT NULL DEFAULT 'OpenAI',
    "TargetType" text NOT NULL DEFAULT 'Text',
    "Status" text NOT NULL DEFAULT 'Safe',
    "Reason" text NOT NULL DEFAULT '',
    "Categories" text NOT NULL DEFAULT '',
    "MaxScore" numeric NOT NULL DEFAULT 0,
    "RawResponse" text NOT NULL DEFAULT '',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "UpdatedAt" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS "IX_listing_moderation_results_ListingId" ON listing_moderation_results ("ListingId");
CREATE INDEX IF NOT EXISTS "IX_listing_moderation_results_ListingId_TargetType_CreatedAt" ON listing_moderation_results ("ListingId", "TargetType", "CreatedAt");

INSERT INTO app_settings ("Id", "Key", "Value", "ValueType", "IsPublic", "CreatedAt", "IsDeleted")
SELECT gen_random_uuid(), key, value, value_type, false, now(), false
FROM (VALUES
    ('moderation.ai_enabled','true','Boolean'),
    ('moderation.auto_approve_safe','true','Boolean'),
    ('moderation.review_threshold','0.45','String'),
    ('moderation.reject_threshold','0.85','String')
) AS v(key,value,value_type)
WHERE NOT EXISTS (SELECT 1 FROM app_settings s WHERE s."Key" = v.key);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
DROP TABLE IF EXISTS listing_moderation_results;
DROP TABLE IF EXISTS blocked_words;
DELETE FROM app_settings WHERE "Key" IN ('moderation.ai_enabled','moderation.auto_approve_safe','moderation.review_threshold','moderation.reject_threshold');
""");
    }
}
