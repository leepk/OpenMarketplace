using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OpenMarketplace.Infrastructure.Persistence;

#nullable disable

namespace OpenMarketplace.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202607030001_EnsureV1Tables")]
public partial class EnsureV1Tables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
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

CREATE TABLE IF NOT EXISTS user_profiles ("Id" uuid PRIMARY KEY, "Name" text NOT NULL, "Email" text NOT NULL, "Phone" text NOT NULL DEFAULT '', "Location" text NOT NULL DEFAULT '', "AvatarUrl" text NOT NULL DEFAULT '', "Role" text NOT NULL DEFAULT 'Customer', "PasswordHash" text NOT NULL DEFAULT '', "EmailVerified" boolean NOT NULL DEFAULT false, "PhoneVerified" boolean NOT NULL DEFAULT false, "IdVerified" boolean NOT NULL DEFAULT false, "BusinessVerified" boolean NOT NULL DEFAULT false, "Rating" numeric NOT NULL DEFAULT 0, "ReviewCount" integer NOT NULL DEFAULT 0, "TrustScore" integer NOT NULL DEFAULT 0, "Status" text NOT NULL DEFAULT 'Active', "CreatedAt" timestamptz NOT NULL DEFAULT now(), "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL DEFAULT false, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_user_profiles_Email" ON user_profiles ("Email");

CREATE TABLE IF NOT EXISTS media_assets ("Id" uuid PRIMARY KEY, "OwnerId" uuid NOT NULL, "ListingId" uuid NULL, "FileName" text NOT NULL, "ContentType" text NOT NULL, "SizeBytes" bigint NOT NULL, "Url" text NOT NULL, "StorageProvider" text NOT NULL DEFAULT 'Local', "IsPrivate" boolean NOT NULL DEFAULT false, "CreatedAt" timestamptz NOT NULL DEFAULT now(), "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL DEFAULT false, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE INDEX IF NOT EXISTS "IX_media_assets_ListingId" ON media_assets ("ListingId");

CREATE TABLE IF NOT EXISTS favorites ("Id" uuid PRIMARY KEY, "UserId" uuid NOT NULL, "ListingId" uuid NOT NULL, "CreatedAt" timestamptz NOT NULL DEFAULT now(), "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL DEFAULT false, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_favorites_UserId_ListingId" ON favorites ("UserId","ListingId");

CREATE TABLE IF NOT EXISTS listing_likes ("Id" uuid PRIMARY KEY, "UserId" uuid NOT NULL, "ListingId" uuid NOT NULL, "CreatedAt" timestamptz NOT NULL DEFAULT now(), "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL DEFAULT false, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_listing_likes_UserId_ListingId" ON listing_likes ("UserId","ListingId");

CREATE TABLE IF NOT EXISTS listing_comments ("Id" uuid PRIMARY KEY, "UserId" uuid NOT NULL, "ListingId" uuid NOT NULL, "Body" text NOT NULL, "Status" text NOT NULL DEFAULT 'Visible', "CreatedAt" timestamptz NOT NULL DEFAULT now(), "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL DEFAULT false, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE INDEX IF NOT EXISTS "IX_listing_comments_ListingId" ON listing_comments ("ListingId");

CREATE TABLE IF NOT EXISTS listing_reviews ("Id" uuid PRIMARY KEY, "ReviewerId" uuid NOT NULL, "SellerId" uuid NOT NULL, "ListingId" uuid NULL, "Rating" integer NOT NULL, "Body" text NOT NULL, "Status" text NOT NULL DEFAULT 'Visible', "CreatedAt" timestamptz NOT NULL DEFAULT now(), "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL DEFAULT false, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE INDEX IF NOT EXISTS "IX_listing_reviews_SellerId" ON listing_reviews ("SellerId");

CREATE TABLE IF NOT EXISTS conversations ("Id" uuid PRIMARY KEY, "ListingId" uuid NOT NULL, "BuyerId" uuid NOT NULL, "SellerId" uuid NOT NULL, "Subject" text NOT NULL, "LastMessageAt" timestamptz NOT NULL DEFAULT now(), "Status" text NOT NULL DEFAULT 'Open', "CreatedAt" timestamptz NOT NULL DEFAULT now(), "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL DEFAULT false, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE INDEX IF NOT EXISTS "IX_conversations_ListingId_BuyerId_SellerId" ON conversations ("ListingId","BuyerId","SellerId");

CREATE TABLE IF NOT EXISTS messages ("Id" uuid PRIMARY KEY, "ConversationId" uuid NOT NULL, "SenderId" uuid NOT NULL, "Body" text NOT NULL, "IsRead" boolean NOT NULL DEFAULT false, "ModerationStatus" text NOT NULL DEFAULT 'Visible', "CreatedAt" timestamptz NOT NULL DEFAULT now(), "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL DEFAULT false, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE INDEX IF NOT EXISTS "IX_messages_ConversationId" ON messages ("ConversationId");

CREATE TABLE IF NOT EXISTS notifications ("Id" uuid PRIMARY KEY, "UserId" uuid NOT NULL, "Type" text NOT NULL, "Title" text NOT NULL, "Body" text NOT NULL, "Url" text NOT NULL DEFAULT '', "IsRead" boolean NOT NULL DEFAULT false, "CreatedAt" timestamptz NOT NULL DEFAULT now(), "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL DEFAULT false, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE INDEX IF NOT EXISTS "IX_notifications_UserId_IsRead_CreatedAt" ON notifications ("UserId","IsRead","CreatedAt");

CREATE TABLE IF NOT EXISTS reports ("Id" uuid PRIMARY KEY, "ReporterId" uuid NOT NULL, "TargetType" text NOT NULL, "TargetId" uuid NOT NULL, "Reason" text NOT NULL, "Details" text NOT NULL DEFAULT '', "Status" text NOT NULL DEFAULT 'Open', "CreatedAt" timestamptz NOT NULL DEFAULT now(), "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL DEFAULT false, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE INDEX IF NOT EXISTS "IX_reports_TargetType_TargetId_Status" ON reports ("TargetType","TargetId","Status");

CREATE TABLE IF NOT EXISTS audit_logs ("Id" uuid PRIMARY KEY, "ActorId" uuid NULL, "Action" text NOT NULL, "EntityType" text NOT NULL, "EntityId" uuid NULL, "Details" text NOT NULL DEFAULT '', "CreatedAt" timestamptz NOT NULL DEFAULT now(), "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL DEFAULT false, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE INDEX IF NOT EXISTS "IX_audit_logs_CreatedAt" ON audit_logs ("CreatedAt");

CREATE TABLE IF NOT EXISTS orders ("Id" uuid PRIMARY KEY, "UserId" uuid NOT NULL, "ListingId" uuid NULL, "OrderNumber" text NOT NULL, "Total" numeric NOT NULL, "Currency" text NOT NULL DEFAULT 'USD', "Status" text NOT NULL DEFAULT 'Pending', "Provider" text NOT NULL DEFAULT 'Demo', "ProviderReference" text NOT NULL DEFAULT '', "CreatedAt" timestamptz NOT NULL DEFAULT now(), "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL DEFAULT false, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_orders_OrderNumber" ON orders ("OrderNumber");

CREATE TABLE IF NOT EXISTS payments ("Id" uuid PRIMARY KEY, "OrderId" uuid NOT NULL, "Amount" numeric NOT NULL, "Currency" text NOT NULL DEFAULT 'USD', "Status" text NOT NULL DEFAULT 'Paid', "Provider" text NOT NULL DEFAULT 'Demo', "ProviderReference" text NOT NULL DEFAULT '', "CreatedAt" timestamptz NOT NULL DEFAULT now(), "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL DEFAULT false, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE INDEX IF NOT EXISTS "IX_payments_OrderId" ON payments ("OrderId");

CREATE TABLE IF NOT EXISTS invoices ("Id" uuid PRIMARY KEY, "OrderId" uuid NOT NULL, "InvoiceNumber" text NOT NULL, "Amount" numeric NOT NULL, "Currency" text NOT NULL DEFAULT 'USD', "Status" text NOT NULL DEFAULT 'Issued', "PdfUrl" text NOT NULL DEFAULT '', "CreatedAt" timestamptz NOT NULL DEFAULT now(), "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL DEFAULT false, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_invoices_InvoiceNumber" ON invoices ("InvoiceNumber");

CREATE TABLE IF NOT EXISTS promotions ("Id" uuid PRIMARY KEY, "ListingId" uuid NOT NULL, "PackageId" uuid NOT NULL, "Type" text NOT NULL, "StartsAt" timestamptz NOT NULL, "EndsAt" timestamptz NOT NULL, "Status" text NOT NULL DEFAULT 'Active', "CreatedAt" timestamptz NOT NULL DEFAULT now(), "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL, "IsDeleted" boolean NOT NULL DEFAULT false, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE INDEX IF NOT EXISTS "IX_promotions_ListingId_Status" ON promotions ("ListingId","Status");
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
DROP TABLE IF EXISTS promotions;
DROP TABLE IF EXISTS invoices;
DROP TABLE IF EXISTS payments;
DROP TABLE IF EXISTS orders;
DROP TABLE IF EXISTS audit_logs;
DROP TABLE IF EXISTS reports;
DROP TABLE IF EXISTS notifications;
DROP TABLE IF EXISTS messages;
DROP TABLE IF EXISTS conversations;
DROP TABLE IF EXISTS listing_reviews;
DROP TABLE IF EXISTS listing_comments;
DROP TABLE IF EXISTS listing_likes;
DROP TABLE IF EXISTS favorites;
DROP TABLE IF EXISTS media_assets;
DROP TABLE IF EXISTS user_profiles;
""");
    }
}
