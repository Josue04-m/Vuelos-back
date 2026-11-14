START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pna."__EFMigrationsHistory" WHERE "MigrationId" = '20261113000000_CiudadAeropuertoOpcional') THEN
    ALTER TABLE pna.aeropuertos ALTER COLUMN ciudad DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pna."__EFMigrationsHistory" WHERE "MigrationId" = '20261113000000_CiudadAeropuertoOpcional') THEN
    INSERT INTO pna."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261113000000_CiudadAeropuertoOpcional', '10.0.14');
    END IF;
END $EF$;
COMMIT;

