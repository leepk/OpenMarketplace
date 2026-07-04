using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OpenMarketplace.Infrastructure.Persistence;

#nullable disable

namespace OpenMarketplace.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202606280001_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
CREATE TABLE IF NOT EXISTS categories ("Id" uuid PRIMARY KEY, "ParentId" uuid NULL, "Name" text NOT NULL, "Slug" text NOT NULL, "SortOrder" integer NOT NULL, "IsActive" boolean NOT NULL, "CreatedAt" timestamptz NOT NULL, "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_categories_Slug" ON categories ("Slug");
CREATE TABLE IF NOT EXISTS packages ("Id" uuid PRIMARY KEY, "Code" text NOT NULL, "Name" text NOT NULL, "Price" numeric NOT NULL, "Currency" text NOT NULL, "DurationDays" integer NOT NULL, "IsActive" boolean NOT NULL, "CreatedAt" timestamptz NOT NULL, "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_packages_Code" ON packages ("Code");
CREATE TABLE IF NOT EXISTS listings ("Id" uuid PRIMARY KEY, "SellerId" uuid NOT NULL, "CategoryId" uuid NOT NULL, "Title" text NOT NULL, "Slug" text NOT NULL, "Description" text NOT NULL, "Price" numeric NULL, "Currency" text NOT NULL, "Status" text NOT NULL, "PublishedAt" timestamptz NULL, "CreatedAt" timestamptz NOT NULL, "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE INDEX IF NOT EXISTS "IX_listings_Status_CategoryId_CreatedAt" ON listings ("Status","CategoryId","CreatedAt");
CREATE TABLE IF NOT EXISTS ad_placements ("Id" uuid PRIMARY KEY, "Code" text NOT NULL, "Name" text NOT NULL, "InsertEvery" integer NOT NULL, "IsActive" boolean NOT NULL, "CreatedAt" timestamptz NOT NULL, "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_ad_placements_Code" ON ad_placements ("Code");
CREATE TABLE IF NOT EXISTS cms_pages ("Id" uuid PRIMARY KEY, "Slug" text NOT NULL, "Title" text NOT NULL, "ContentMd" text NOT NULL, "Status" text NOT NULL, "CreatedAt" timestamptz NOT NULL, "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_cms_pages_Slug" ON cms_pages ("Slug");
CREATE TABLE IF NOT EXISTS app_settings ("Id" uuid PRIMARY KEY, "Key" text NOT NULL, "Value" text NOT NULL, "ValueType" text NOT NULL, "IsPublic" boolean NOT NULL, "CreatedAt" timestamptz NOT NULL, "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_app_settings_Key" ON app_settings ("Key");

ALTER TABLE listings ADD COLUMN IF NOT EXISTS "ModerationStatus" text NOT NULL DEFAULT 'Pending';
ALTER TABLE listings ADD COLUMN IF NOT EXISTS "ModerationReason" text NOT NULL DEFAULT '';
ALTER TABLE listings ADD COLUMN IF NOT EXISTS "Location" text NOT NULL DEFAULT '';
ALTER TABLE listings ADD COLUMN IF NOT EXISTS "Latitude" numeric NULL;
ALTER TABLE listings ADD COLUMN IF NOT EXISTS "Longitude" numeric NULL;
ALTER TABLE listings ADD COLUMN IF NOT EXISTS "IsFeatured" boolean NOT NULL DEFAULT false;
ALTER TABLE listings ADD COLUMN IF NOT EXISTS "IsUrgent" boolean NOT NULL DEFAULT false;
ALTER TABLE listings ADD COLUMN IF NOT EXISTS "IsPinned" boolean NOT NULL DEFAULT false;
ALTER TABLE listings ADD COLUMN IF NOT EXISTS "IsSold" boolean NOT NULL DEFAULT false;
ALTER TABLE listings ADD COLUMN IF NOT EXISTS "ViewCount" integer NOT NULL DEFAULT 0;
ALTER TABLE listings ADD COLUMN IF NOT EXISTS "FavoriteCount" integer NOT NULL DEFAULT 0;
ALTER TABLE listings ADD COLUMN IF NOT EXISTS "LikeCount" integer NOT NULL DEFAULT 0;
ALTER TABLE listings ADD COLUMN IF NOT EXISTS "CommentCount" integer NOT NULL DEFAULT 0;
ALTER TABLE listings ADD COLUMN IF NOT EXISTS "ExpiresAt" timestamptz NULL;
CREATE TABLE IF NOT EXISTS user_profiles ("Id" uuid PRIMARY KEY, "Name" text NOT NULL, "Email" text NOT NULL, "Phone" text NOT NULL, "Location" text NOT NULL, "AvatarUrl" text NOT NULL, "Role" text NOT NULL, "PasswordHash" text NOT NULL DEFAULT '', "EmailVerified" boolean NOT NULL, "PhoneVerified" boolean NOT NULL, "IdVerified" boolean NOT NULL, "BusinessVerified" boolean NOT NULL, "Rating" numeric NOT NULL, "ReviewCount" integer NOT NULL, "TrustScore" integer NOT NULL, "Status" text NOT NULL, "CreatedAt" timestamptz NOT NULL, "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_user_profiles_Email" ON user_profiles ("Email");
CREATE TABLE IF NOT EXISTS media_assets ("Id" uuid PRIMARY KEY, "OwnerId" uuid NOT NULL, "ListingId" uuid NULL, "FileName" text NOT NULL, "ContentType" text NOT NULL, "SizeBytes" bigint NOT NULL, "Url" text NOT NULL, "StorageProvider" text NOT NULL, "IsPrivate" boolean NOT NULL, "CreatedAt" timestamptz NOT NULL, "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE TABLE IF NOT EXISTS favorites ("Id" uuid PRIMARY KEY, "UserId" uuid NOT NULL, "ListingId" uuid NOT NULL, "CreatedAt" timestamptz NOT NULL, "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_favorites_UserId_ListingId" ON favorites ("UserId","ListingId");
CREATE TABLE IF NOT EXISTS listing_likes ("Id" uuid PRIMARY KEY, "UserId" uuid NOT NULL, "ListingId" uuid NOT NULL, "CreatedAt" timestamptz NOT NULL, "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_listing_likes_UserId_ListingId" ON listing_likes ("UserId","ListingId");
CREATE TABLE IF NOT EXISTS listing_comments ("Id" uuid PRIMARY KEY, "UserId" uuid NOT NULL, "ListingId" uuid NOT NULL, "Body" text NOT NULL, "Status" text NOT NULL, "CreatedAt" timestamptz NOT NULL, "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE TABLE IF NOT EXISTS listing_reviews ("Id" uuid PRIMARY KEY, "ReviewerId" uuid NOT NULL, "SellerId" uuid NOT NULL, "ListingId" uuid NULL, "Rating" integer NOT NULL, "Body" text NOT NULL, "Status" text NOT NULL, "CreatedAt" timestamptz NOT NULL, "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE TABLE IF NOT EXISTS conversations ("Id" uuid PRIMARY KEY, "ListingId" uuid NOT NULL, "BuyerId" uuid NOT NULL, "SellerId" uuid NOT NULL, "Subject" text NOT NULL, "LastMessageAt" timestamptz NOT NULL, "Status" text NOT NULL, "CreatedAt" timestamptz NOT NULL, "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE TABLE IF NOT EXISTS messages ("Id" uuid PRIMARY KEY, "ConversationId" uuid NOT NULL, "SenderId" uuid NOT NULL, "Body" text NOT NULL, "IsRead" boolean NOT NULL, "ModerationStatus" text NOT NULL, "CreatedAt" timestamptz NOT NULL, "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE TABLE IF NOT EXISTS notifications ("Id" uuid PRIMARY KEY, "UserId" uuid NOT NULL, "Type" text NOT NULL, "Title" text NOT NULL, "Body" text NOT NULL, "Url" text NOT NULL, "IsRead" boolean NOT NULL, "CreatedAt" timestamptz NOT NULL, "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE TABLE IF NOT EXISTS reports ("Id" uuid PRIMARY KEY, "ReporterId" uuid NOT NULL, "TargetType" text NOT NULL, "TargetId" uuid NOT NULL, "Reason" text NOT NULL, "Details" text NOT NULL, "Status" text NOT NULL, "CreatedAt" timestamptz NOT NULL, "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE TABLE IF NOT EXISTS audit_logs ("Id" uuid PRIMARY KEY, "ActorId" uuid NULL, "Action" text NOT NULL, "EntityType" text NOT NULL, "EntityId" uuid NULL, "Details" text NOT NULL, "CreatedAt" timestamptz NOT NULL, "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE TABLE IF NOT EXISTS orders ("Id" uuid PRIMARY KEY, "UserId" uuid NOT NULL, "ListingId" uuid NULL, "OrderNumber" text NOT NULL, "Total" numeric NOT NULL, "Currency" text NOT NULL, "Status" text NOT NULL, "Provider" text NOT NULL, "ProviderReference" text NOT NULL, "CreatedAt" timestamptz NOT NULL, "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_orders_OrderNumber" ON orders ("OrderNumber");
CREATE TABLE IF NOT EXISTS payments ("Id" uuid PRIMARY KEY, "OrderId" uuid NOT NULL, "Amount" numeric NOT NULL, "Currency" text NOT NULL, "Status" text NOT NULL, "Provider" text NOT NULL, "ProviderReference" text NOT NULL, "CreatedAt" timestamptz NOT NULL, "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE TABLE IF NOT EXISTS invoices ("Id" uuid PRIMARY KEY, "OrderId" uuid NOT NULL, "InvoiceNumber" text NOT NULL, "Amount" numeric NOT NULL, "Currency" text NOT NULL, "Status" text NOT NULL, "PdfUrl" text NOT NULL, "CreatedAt" timestamptz NOT NULL, "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_invoices_InvoiceNumber" ON invoices ("InvoiceNumber");
CREATE TABLE IF NOT EXISTS promotions ("Id" uuid PRIMARY KEY, "ListingId" uuid NOT NULL, "PackageId" uuid NOT NULL, "Type" text NOT NULL, "StartsAt" timestamptz NOT NULL, "EndsAt" timestamptz NOT NULL, "Status" text NOT NULL, "CreatedAt" timestamptz NOT NULL, "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
DROP TABLE IF EXISTS app_settings;
DROP TABLE IF EXISTS cms_pages;
DROP TABLE IF EXISTS ad_placements;
DROP TABLE IF EXISTS listings;
DROP TABLE IF EXISTS packages;
DROP TABLE IF EXISTS categories;
""");
    }
}
