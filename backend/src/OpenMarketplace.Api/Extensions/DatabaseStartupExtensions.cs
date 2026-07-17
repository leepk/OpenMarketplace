using Microsoft.EntityFrameworkCore;
using OpenMarketplace.Infrastructure.Persistence;

namespace OpenMarketplace.Api.Extensions;

public static class DatabaseStartupExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app, CancellationToken ct = default)
    {
        await using var scope = app.Services.CreateAsyncScope();

        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseStartup");
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();

        logger.LogInformation("Database initialization started");

        var pendingMigrations = (await db.Database.GetPendingMigrationsAsync(ct)).ToArray();
        logger.LogInformation("Pending EF migrations: {Migrations}", pendingMigrations.Length == 0 ? "none" : string.Join(", ", pendingMigrations));

        logger.LogInformation("Applying EF Core migrations automatically...");
        await db.Database.MigrateAsync(ct);

        logger.LogInformation("Ensuring message/chat schema repair...");
        await EnsureMessageChatSchemaAsync(db, ct);

        logger.LogInformation("Ensuring listing location schema repair...");
        await EnsureListingLocationSchemaAsync(db, ct);

        logger.LogInformation("Ensuring notification schema repair...");
        await EnsureNotificationSchemaAsync(db, ct);

        logger.LogInformation("Ensuring user role/source schema repair...");
        await EnsureUserRoleSourceSchemaAsync(db, ct);

        logger.LogInformation("Ensuring package sort order schema repair...");
        await EnsurePackageSortOrderSchemaAsync(db, ct);

        logger.LogInformation("Ensuring authentication schema repair...");
        await EnsureAuthenticationSchemaAsync(db, ct);

        logger.LogInformation("Ensuring moderation schema repair...");
        await EnsureModerationSchemaAsync(db, ct);

        logger.LogInformation("Ensuring user location schema repair...");
        await EnsureUserLocationSchemaAsync(db, ct);

        logger.LogInformation("Ensuring locality schema and California city data...");
        await EnsureLocalitySchemaAndSeedAsync(db, ct);

        logger.LogInformation("Ensuring seed history schema...");
        await EnsureSeedHistorySchemaAsync(db, ct);

        const string initialSeedVersion = "InitialMarketplaceDataV1";
        if (await HasSeedVersionAsync(db, initialSeedVersion, ct))
        {
            logger.LogInformation("Seed version {SeedVersion} already applied. Skipping seed data.", initialSeedVersion);
        }
        else if (await HasExistingInitialSeedDataAsync(db, ct))
        {
            // Older databases were created before seed history existed. Mark them as seeded
            // without running the upsert seeder again, so admin-edited categories, packages,
            // providers, advertisements and settings are not overwritten on upgrade.
            await RecordSeedVersionAsync(db, initialSeedVersion, ct);
            logger.LogInformation("Existing marketplace data detected. Marked seed version {SeedVersion} as applied without reseeding.", initialSeedVersion);
        }
        else
        {
            logger.LogInformation("Applying seed version {SeedVersion}...", initialSeedVersion);
            await seeder.SeedAsync(ct);
            await RecordSeedVersionAsync(db, initialSeedVersion, ct);
            logger.LogInformation("Seed version {SeedVersion} applied successfully.", initialSeedVersion);
        }

        logger.LogInformation("Database initialization completed");
    }

    private static async Task EnsureMessageChatSchemaAsync(AppDbContext db, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync("""
ALTER TABLE conversations ADD COLUMN IF NOT EXISTS "LastMessageId" uuid NULL;
ALTER TABLE conversations ADD COLUMN IF NOT EXISTS "LastMessagePreview" text NOT NULL DEFAULT '';

ALTER TABLE messages ADD COLUMN IF NOT EXISTS "ReceiverId" uuid NULL;
ALTER TABLE messages ADD COLUMN IF NOT EXISTS "MessageType" text NOT NULL DEFAULT 'Text';
ALTER TABLE messages ADD COLUMN IF NOT EXISTS "ReadAt" timestamptz NULL;
ALTER TABLE messages ADD COLUMN IF NOT EXISTS "MetadataJson" text NOT NULL DEFAULT '{{}}';

UPDATE messages m
SET "ReceiverId" = CASE
    WHEN m."SenderId" = c."BuyerId" THEN c."SellerId"
    ELSE c."BuyerId"
END
FROM conversations c
WHERE m."ConversationId" = c."Id" AND m."ReceiverId" IS NULL;

UPDATE conversations c
SET "LastMessageId" = latest."Id",
    "LastMessagePreview" = LEFT(COALESCE(latest."Body", ''), 160),
    "LastMessageAt" = latest."CreatedAt"
FROM (
    SELECT DISTINCT ON ("ConversationId") "ConversationId", "Id", "Body", "CreatedAt"
    FROM messages
    WHERE COALESCE("IsDeleted", false) = false
    ORDER BY "ConversationId", "CreatedAt" DESC
) latest
WHERE c."Id" = latest."ConversationId"
  AND (c."LastMessageId" IS NULL OR c."LastMessageAt" IS NULL OR COALESCE(c."LastMessagePreview", '') = '');

CREATE INDEX IF NOT EXISTS "IX_messages_ConversationId_CreatedAt" ON messages ("ConversationId", "CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_messages_ReceiverId" ON messages ("ReceiverId");
CREATE INDEX IF NOT EXISTS "IX_conversations_LastMessageAt" ON conversations ("LastMessageAt");
""", ct);
    }


    private static async Task EnsureListingLocationSchemaAsync(AppDbContext db, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync("""
ALTER TABLE listings ADD COLUMN IF NOT EXISTS "AddressLine" text NOT NULL DEFAULT '';
ALTER TABLE listings ADD COLUMN IF NOT EXISTS "City" text NOT NULL DEFAULT '';
ALTER TABLE listings ADD COLUMN IF NOT EXISTS "State" text NOT NULL DEFAULT '';
ALTER TABLE listings ADD COLUMN IF NOT EXISTS "PostalCode" text NOT NULL DEFAULT '';
ALTER TABLE listings ADD COLUMN IF NOT EXISTS "Country" text NOT NULL DEFAULT 'US';
ALTER TABLE listings ADD COLUMN IF NOT EXISTS "LocationSource" text NOT NULL DEFAULT 'Manual';
ALTER TABLE listings ADD COLUMN IF NOT EXISTS "LocationPrecision" text NOT NULL DEFAULT 'ApproximateCity';
ALTER TABLE listings ADD COLUMN IF NOT EXISTS "HideExactLocation" boolean NOT NULL DEFAULT true;
CREATE INDEX IF NOT EXISTS "IX_listings_Latitude_Longitude" ON listings ("Latitude", "Longitude");
CREATE INDEX IF NOT EXISTS "IX_listings_City_State" ON listings ("City", "State");
""", ct);
    }


    private static async Task EnsureNotificationSchemaAsync(AppDbContext db, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync("""
ALTER TABLE notifications ADD COLUMN IF NOT EXISTS "EntityType" text NOT NULL DEFAULT '';
ALTER TABLE notifications ADD COLUMN IF NOT EXISTS "EntityId" uuid NULL;
ALTER TABLE notifications ADD COLUMN IF NOT EXISTS "ImageUrl" text NOT NULL DEFAULT '';
ALTER TABLE notifications ADD COLUMN IF NOT EXISTS "ReadAt" timestamptz NULL;

UPDATE notifications
SET "EntityType" = CASE
    WHEN "Type" = 'Message' THEN 'Conversation'
    WHEN "Type" = 'Listing' THEN 'Listing'
    WHEN "Type" = 'Payment' THEN 'Payment'
    ELSE COALESCE(NULLIF("EntityType", ''), 'System')
END
WHERE COALESCE("EntityType", '') = '';

CREATE INDEX IF NOT EXISTS "IX_notifications_UserId_IsRead_CreatedAt" ON notifications ("UserId", "IsRead", "CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_notifications_UserId_Type_CreatedAt" ON notifications ("UserId", "Type", "CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_notifications_EntityType_EntityId" ON notifications ("EntityType", "EntityId");
""", ct);
    }
    private static async Task EnsureUserRoleSourceSchemaAsync(AppDbContext db, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync("""
ALTER TABLE user_profiles ADD COLUMN IF NOT EXISTS "Role" text NOT NULL DEFAULT 'Customer';
ALTER TABLE user_profiles ADD COLUMN IF NOT EXISTS "Source" text NOT NULL DEFAULT 'WebCustomer';
UPDATE user_profiles SET "Role" = 'Customer' WHERE "Role" IS NULL OR trim("Role") = '';
UPDATE user_profiles SET "Source" = CASE
    WHEN "Role" IN ('Admin','SuperAdmin') THEN 'AdminCreated'
    WHEN "Role" = 'System' THEN 'SystemManaged'
    ELSE 'WebCustomer'
END WHERE "Source" IS NULL OR trim("Source") = '';
CREATE INDEX IF NOT EXISTS "IX_user_profiles_Role_Source_Status" ON user_profiles ("Role", "Source", "Status");
""", ct);
    }

    private static async Task EnsurePackageSortOrderSchemaAsync(AppDbContext db, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync("""
ALTER TABLE packages ADD COLUMN IF NOT EXISTS "SortOrder" integer NOT NULL DEFAULT 0;
UPDATE packages
SET "SortOrder" = CASE UPPER("Code")
    WHEN 'FREE' THEN 10
    WHEN 'BASIC' THEN 20
    WHEN 'URGENT' THEN 30
    WHEN 'FEATURED' THEN 40
    WHEN 'PREMIUM' THEN 50
    WHEN 'CREDITS100' THEN 60
    ELSE COALESCE(NULLIF("SortOrder", 0), 100)
END
WHERE "SortOrder" = 0 OR "SortOrder" IS NULL;
""", ct);
    }


    private static async Task EnsureAuthenticationSchemaAsync(AppDbContext db, CancellationToken ct)
    {
        // Keep upgrades compatible with databases created before password reset and
        // phone verification were introduced. This repair intentionally runs before
        // DatabaseSeeder because EF includes every mapped property in user queries.
        await db.Database.ExecuteSqlRawAsync("""
ALTER TABLE user_profiles ADD COLUMN IF NOT EXISTS "PasswordResetTokenHash" text NOT NULL DEFAULT '';
ALTER TABLE user_profiles ADD COLUMN IF NOT EXISTS "PasswordResetExpiresAt" timestamptz NULL;

ALTER TABLE user_profiles ADD COLUMN IF NOT EXISTS "PhoneVerificationCodeHash" text NOT NULL DEFAULT '';
ALTER TABLE user_profiles ADD COLUMN IF NOT EXISTS "PhoneVerificationExpiresAt" timestamptz NULL;
ALTER TABLE user_profiles ADD COLUMN IF NOT EXISTS "PhoneVerificationSentAt" timestamptz NULL;
ALTER TABLE user_profiles ADD COLUMN IF NOT EXISTS "PhoneVerificationAttempts" integer NOT NULL DEFAULT 0;

CREATE INDEX IF NOT EXISTS "IX_user_profiles_PasswordResetTokenHash"
    ON user_profiles ("PasswordResetTokenHash");
""", ct);
    }


    private static async Task EnsureModerationSchemaAsync(AppDbContext db, CancellationToken ct)
    {
        // Some existing databases may contain the EF migration history entry while the
        // moderation tables were removed or never created. Keep startup self-healing.
        await db.Database.ExecuteSqlRawAsync("""
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
    "CreatedAt" timestamptz NOT NULL DEFAULT now(),
    "CreatedBy" uuid NULL,
    "UpdatedAt" timestamptz NULL,
    "UpdatedBy" uuid NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamptz NULL,
    "DeletedBy" uuid NULL
);

ALTER TABLE blocked_words ADD COLUMN IF NOT EXISTS "CreatedBy" uuid NULL;
ALTER TABLE blocked_words ADD COLUMN IF NOT EXISTS "UpdatedBy" uuid NULL;
ALTER TABLE blocked_words ADD COLUMN IF NOT EXISTS "DeletedAt" timestamptz NULL;
ALTER TABLE blocked_words ADD COLUMN IF NOT EXISTS "DeletedBy" uuid NULL;

CREATE INDEX IF NOT EXISTS "IX_blocked_words_IsActive_NormalizedWord"
    ON blocked_words ("IsActive", "NormalizedWord");

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
    "CreatedAt" timestamptz NOT NULL DEFAULT now(),
    "CreatedBy" uuid NULL,
    "UpdatedAt" timestamptz NULL,
    "UpdatedBy" uuid NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamptz NULL,
    "DeletedBy" uuid NULL
);

ALTER TABLE listing_moderation_results ADD COLUMN IF NOT EXISTS "CreatedBy" uuid NULL;
ALTER TABLE listing_moderation_results ADD COLUMN IF NOT EXISTS "UpdatedBy" uuid NULL;
ALTER TABLE listing_moderation_results ADD COLUMN IF NOT EXISTS "DeletedAt" timestamptz NULL;
ALTER TABLE listing_moderation_results ADD COLUMN IF NOT EXISTS "DeletedBy" uuid NULL;

CREATE INDEX IF NOT EXISTS "IX_listing_moderation_results_ListingId"
    ON listing_moderation_results ("ListingId");
CREATE INDEX IF NOT EXISTS "IX_listing_moderation_results_ListingId_TargetType_CreatedAt"
    ON listing_moderation_results ("ListingId", "TargetType", "CreatedAt");

INSERT INTO app_settings ("Id", "Key", "Value", "ValueType", "IsPublic", "CreatedAt", "IsDeleted")
SELECT gen_random_uuid(), key, value, value_type, false, now(), false
FROM (VALUES
    ('moderation.ai_enabled','true','Boolean'),
    ('moderation.auto_approve_safe','true','Boolean'),
    ('moderation.review_threshold','0.45','String'),
    ('moderation.reject_threshold','0.85','String')
) AS v(key,value,value_type)
WHERE NOT EXISTS (SELECT 1 FROM app_settings s WHERE s."Key" = v.key);

INSERT INTO app_settings ("Id", "Key", "Value", "ValueType", "IsPublic", "CreatedAt", "IsDeleted")
SELECT gen_random_uuid(), key, value, value_type, true, now(), false
FROM (VALUES
    ('auth.email_enabled','true','Boolean'),
    ('auth.google_enabled','false','Boolean'),
    ('auth.google_client_id','','String'),
    ('auth.google_client_secret','','Secret'),
    ('auth.facebook_enabled','false','Boolean'),
    ('auth.facebook_app_id','','String'),
    ('auth.facebook_app_secret','','Secret'),
    ('auth.auto_create_user','true','Boolean')
) AS v(key,value,value_type)
WHERE NOT EXISTS (SELECT 1 FROM app_settings s WHERE s."Key" = v.key);

UPDATE app_settings
SET "IsPublic" = true
WHERE "Key" IN ('auth.email_enabled','auth.google_enabled','auth.google_client_id','auth.facebook_enabled','auth.facebook_app_id','auth.auto_create_user');
""", ct);
    }


    private static async Task EnsureSeedHistorySchemaAsync(AppDbContext db, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync("""
CREATE TABLE IF NOT EXISTS system_seed_history (
    "Version" text NOT NULL PRIMARY KEY,
    "AppliedAt" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP
);
""", ct);
    }

    private static async Task<bool> HasSeedVersionAsync(AppDbContext db, string version, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS (SELECT 1 FROM system_seed_history WHERE \"Version\" = @version);";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "version";
        parameter.Value = version;
        command.Parameters.Add(parameter);

        return Convert.ToBoolean(await command.ExecuteScalarAsync(ct));
    }

    private static async Task<bool> HasExistingInitialSeedDataAsync(AppDbContext db, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT
    EXISTS (SELECT 1 FROM categories WHERE lower("Code") = 'vehicles')
    AND EXISTS (SELECT 1 FROM packages WHERE upper("Code") = 'FREE')
    AND EXISTS (SELECT 1 FROM user_profiles);
""";

        return Convert.ToBoolean(await command.ExecuteScalarAsync(ct));
    }

    private static async Task RecordSeedVersionAsync(AppDbContext db, string version, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandText = """
INSERT INTO system_seed_history ("Version", "AppliedAt")
VALUES (@version, CURRENT_TIMESTAMP)
ON CONFLICT ("Version") DO NOTHING;
""";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "version";
        parameter.Value = version;
        command.Parameters.Add(parameter);
        await command.ExecuteNonQueryAsync(ct);
    }

    public static async Task InitializeDatabaseWithRetryAsync(this WebApplication app, CancellationToken ct = default)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseStartup");
        const int maxAttempts = 10;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                logger.LogInformation("Auto database migration/seed attempt {Attempt}/{MaxAttempts}", attempt, maxAttempts);
                await app.InitializeDatabaseAsync(ct);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts && !ct.IsCancellationRequested)
            {
                var delaySeconds = Math.Min(30, attempt * 3);
                logger.LogWarning(ex, "Database is not ready or migration failed. Retrying in {DelaySeconds} seconds...", delaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), ct);
            }
        }

        // Last attempt without swallowing the exception. If migration still fails, the app should not accept API requests
        // against a partially-created schema.
        await app.InitializeDatabaseAsync(ct);
    }

    public static void StartDatabaseInitialization(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseStartup");

        app.Lifetime.ApplicationStarted.Register(() =>
        {
            _ = Task.Run(async () =>
            {
                const int maxAttempts = 10;

                for (var attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        logger.LogInformation("Auto database migration/seed attempt {Attempt}/{MaxAttempts}", attempt, maxAttempts);
                        await app.InitializeDatabaseAsync();
                        return;
                    }
                    catch (Exception ex) when (attempt < maxAttempts)
                    {
                        var delaySeconds = Math.Min(30, attempt * 3);
                        logger.LogWarning(ex, "Database is not ready or migration failed. Retrying in {DelaySeconds} seconds...", delaySeconds);
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Database initialization failed after {MaxAttempts} attempts. API and Swagger remain running.", maxAttempts);
                    }
                }
            });
        });
    }

    private static async Task EnsureUserLocationSchemaAsync(AppDbContext db, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync("""
CREATE TABLE IF NOT EXISTS user_locations (
    "Id" uuid NOT NULL PRIMARY KEY,
    "UserId" uuid NOT NULL,
    "Label" text NOT NULL DEFAULT '',
    "AddressLine" text NOT NULL DEFAULT '',
    "City" text NOT NULL DEFAULT '',
    "State" text NOT NULL DEFAULT '',
    "PostalCode" text NOT NULL DEFAULT '',
    "Country" text NOT NULL DEFAULT 'US',
    "Latitude" numeric NULL,
    "Longitude" numeric NULL,
    "UseCount" integer NOT NULL DEFAULT 0,
    "LastUsedAt" timestamptz NULL,
    "IsDefault" boolean NOT NULL DEFAULT false,
    "CreatedAt" timestamptz NOT NULL DEFAULT now(),
    "CreatedBy" uuid NULL,
    "UpdatedAt" timestamptz NULL,
    "UpdatedBy" uuid NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamptz NULL,
    "DeletedBy" uuid NULL
);
CREATE INDEX IF NOT EXISTS "IX_user_locations_UserId_LastUsedAt" ON user_locations ("UserId", "LastUsedAt" DESC);
CREATE INDEX IF NOT EXISTS "IX_user_locations_UserId_IsDefault" ON user_locations ("UserId", "IsDefault");
""", ct);
    }

    private static async Task EnsureLocalitySchemaAndSeedAsync(AppDbContext db, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync("""
CREATE TABLE IF NOT EXISTS localities (
    "Id" uuid NOT NULL PRIMARY KEY, "Name" text NOT NULL DEFAULT '', "StateCode" text NOT NULL DEFAULT 'CA',
    "CountryCode" text NOT NULL DEFAULT 'US', "GeoId" text NOT NULL DEFAULT '', "Latitude" numeric NULL, "Longitude" numeric NULL,
    "SortOrder" integer NOT NULL DEFAULT 0, "SelectionCount" bigint NOT NULL DEFAULT 0, "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamptz NOT NULL DEFAULT now(), "CreatedBy" uuid NULL, "UpdatedAt" timestamptz NULL, "UpdatedBy" uuid NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false, "DeletedAt" timestamptz NULL, "DeletedBy" uuid NULL);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_localities_StateCode_Name" ON localities ("StateCode", "Name");
CREATE INDEX IF NOT EXISTS "IX_localities_IsActive_SortOrder_SelectionCount" ON localities ("IsActive", "SortOrder", "SelectionCount");
""", ct);

        foreach (var item in CaliforniaLocalitySeed.Items)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
INSERT INTO localities ("Id", "Name", "StateCode", "CountryCode", "GeoId", "Latitude", "Longitude", "SortOrder", "SelectionCount", "IsActive", "CreatedAt", "IsDeleted")
VALUES ({Guid.CreateVersion7()}, {item.Name}, {"CA"}, {"US"}, {item.GeoId}, {item.Latitude}, {item.Longitude}, 0, 0, true, now(), false)
ON CONFLICT ("StateCode", "Name") DO UPDATE SET "GeoId"=EXCLUDED."GeoId", "Latitude"=EXCLUDED."Latitude", "Longitude"=EXCLUDED."Longitude";
""", ct);
        }

        await db.Database.ExecuteSqlRawAsync("""
UPDATE localities l SET "SelectionCount" = x.cnt
FROM (SELECT LOWER(TRIM("City")) city, COUNT(*)::bigint cnt FROM listings WHERE COALESCE(TRIM("City"),'') <> '' GROUP BY LOWER(TRIM("City"))) x
WHERE LOWER(l."Name") = x.city AND l."StateCode" = 'CA';
""", ct);
    }

}
