CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106144559_InitialConfiguration') THEN
    CREATE TABLE "AuditEntries" (
        "Id" uuid NOT NULL,
        "Action" text NOT NULL,
        "Key" text NOT NULL,
        "Before" text,
        "After" text,
        "Actor" text,
        "TenantId" uuid,
        "CorrelationId" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AuditEntries" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106144559_InitialConfiguration') THEN
    CREATE TABLE "OutboxEvents" (
        "Id" uuid NOT NULL,
        "EventType" text NOT NULL,
        "Payload" text NOT NULL,
        "CorrelationId" text NOT NULL,
        "OccurredAt" timestamp with time zone NOT NULL,
        "Dispatched" boolean NOT NULL,
        "DispatchedAt" timestamp with time zone,
        "Attempts" integer NOT NULL,
        CONSTRAINT "PK_OutboxEvents" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106144559_InitialConfiguration') THEN
    CREATE INDEX "IX_OutboxEvents_Dispatched" ON "OutboxEvents" ("Dispatched");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106144559_InitialConfiguration') THEN
    CREATE INDEX "IX_OutboxEvents_OccurredAt" ON "OutboxEvents" ("OccurredAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260106144559_InitialConfiguration') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260106144559_InitialConfiguration', '8.0.4');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260109182107_AddBaseEntityColumnsToOutboxEvents') THEN
    DROP TABLE IF EXISTS "AuditEntries";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260109182107_AddBaseEntityColumnsToOutboxEvents') THEN
    DO $$
    BEGIN
        IF EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_name='ConfigurationItems' AND column_name='CreatedBy'
        ) THEN
            EXECUTE 'ALTER TABLE "ConfigurationItems" RENAME COLUMN "CreatedBy" TO "SourceSystem"';
        END IF;
    END$$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260109182107_AddBaseEntityColumnsToOutboxEvents') THEN
    ALTER TABLE "OutboxEvents" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260109182107_AddBaseEntityColumnsToOutboxEvents') THEN
    ALTER TABLE "OutboxEvents" ADD "CreatedAt" timestamp with time zone NOT NULL DEFAULT TIMESTAMPTZ '-infinity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260109182107_AddBaseEntityColumnsToOutboxEvents') THEN
    ALTER TABLE "OutboxEvents" ADD "CreatorId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260109182107_AddBaseEntityColumnsToOutboxEvents') THEN
    ALTER TABLE "OutboxEvents" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260109182107_AddBaseEntityColumnsToOutboxEvents') THEN
    ALTER TABLE "OutboxEvents" ADD "DeleterId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260109182107_AddBaseEntityColumnsToOutboxEvents') THEN
    ALTER TABLE "OutboxEvents" ADD "EffectiveDate" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260109182107_AddBaseEntityColumnsToOutboxEvents') THEN
    ALTER TABLE "OutboxEvents" ADD "ExternalRef" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260109182107_AddBaseEntityColumnsToOutboxEvents') THEN
    ALTER TABLE "OutboxEvents" ADD "ImportBatchId" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260109182107_AddBaseEntityColumnsToOutboxEvents') THEN
    ALTER TABLE "OutboxEvents" ADD "Notes" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260109182107_AddBaseEntityColumnsToOutboxEvents') THEN
    ALTER TABLE "OutboxEvents" ADD "SourceSystem" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260109182107_AddBaseEntityColumnsToOutboxEvents') THEN
    ALTER TABLE "OutboxEvents" ADD "Status" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260109182107_AddBaseEntityColumnsToOutboxEvents') THEN
    ALTER TABLE "OutboxEvents" ADD "TenantId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260109182107_AddBaseEntityColumnsToOutboxEvents') THEN
    ALTER TABLE "OutboxEvents" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260109182107_AddBaseEntityColumnsToOutboxEvents') THEN
    ALTER TABLE "OutboxEvents" ADD "UpdaterId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260109182107_AddBaseEntityColumnsToOutboxEvents') THEN
    ALTER TABLE "OutboxEvents" ADD "Version" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260109182107_AddBaseEntityColumnsToOutboxEvents') THEN
    ALTER TABLE "ConfigurationItems" ALTER COLUMN "Status" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260109182107_AddBaseEntityColumnsToOutboxEvents') THEN
    ALTER TABLE "ConfigurationItems" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260109182107_AddBaseEntityColumnsToOutboxEvents') THEN
    ALTER TABLE "ConfigurationItems" ADD "CreatorId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260109182107_AddBaseEntityColumnsToOutboxEvents') THEN
    ALTER TABLE "ConfigurationItems" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260109182107_AddBaseEntityColumnsToOutboxEvents') THEN
    ALTER TABLE "ConfigurationItems" ADD "DeleterId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260109182107_AddBaseEntityColumnsToOutboxEvents') THEN
    ALTER TABLE "ConfigurationItems" ADD "EffectiveDate" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260109182107_AddBaseEntityColumnsToOutboxEvents') THEN
    ALTER TABLE "ConfigurationItems" ADD "ExternalRef" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260109182107_AddBaseEntityColumnsToOutboxEvents') THEN
    ALTER TABLE "ConfigurationItems" ADD "ImportBatchId" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260109182107_AddBaseEntityColumnsToOutboxEvents') THEN
    ALTER TABLE "ConfigurationItems" ADD "Notes" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260109182107_AddBaseEntityColumnsToOutboxEvents') THEN
    ALTER TABLE "ConfigurationItems" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260109182107_AddBaseEntityColumnsToOutboxEvents') THEN
    ALTER TABLE "ConfigurationItems" ADD "UpdaterId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260109182107_AddBaseEntityColumnsToOutboxEvents') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260109182107_AddBaseEntityColumnsToOutboxEvents', '8.0.4');
    END IF;
END $EF$;
COMMIT;

