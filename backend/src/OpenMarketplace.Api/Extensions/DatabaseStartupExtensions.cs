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

        logger.LogInformation("Seeding database automatically...");
        await seeder.SeedAsync(ct);

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
}
