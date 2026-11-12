-- EJECUCION MANUAL, despues de aplicar InicialPostgreSql en la base de prueba.
-- Ejecutar como propietario de los objetos/migraciones y con permiso para crear roles.
-- No contiene credenciales ni crea un usuario LOGIN. Revisar antes de ejecutar.
BEGIN;

DO $pna_rol$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'pna_runtime') THEN
        CREATE ROLE pna_runtime NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
    ELSIF EXISTS (
        SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'pna_runtime'
          AND (rolcanlogin OR rolsuper OR rolcreatedb OR rolcreaterole OR rolreplication OR rolbypassrls)
    ) THEN
        RAISE EXCEPTION 'pna_runtime ya existe con atributos diferentes. Revise el rol; no se modificara automaticamente.';
    END IF;
END;
$pna_rol$;

GRANT USAGE ON SCHEMA pna TO pna_runtime;
GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA pna TO pna_runtime;
GRANT USAGE ON ALL SEQUENCES IN SCHEMA pna TO pna_runtime;
GRANT EXECUTE ON FUNCTION pna.generar_version_fila() TO pna_runtime;

-- La API no modifica el historial de migraciones ni elimina/trunca datos.
REVOKE ALL ON TABLE pna."__EFMigrationsHistory" FROM pna_runtime;
REVOKE DELETE, TRUNCATE, REFERENCES, TRIGGER ON ALL TABLES IN SCHEMA pna FROM pna_runtime;
REVOKE CREATE ON SCHEMA pna FROM pna_runtime;

-- Solo afectan a objetos futuros creados por el usuario que ejecuta este script.
ALTER DEFAULT PRIVILEGES IN SCHEMA pna
    GRANT SELECT, INSERT, UPDATE ON TABLES TO pna_runtime;
ALTER DEFAULT PRIVILEGES IN SCHEMA pna
    GRANT USAGE ON SEQUENCES TO pna_runtime;
ALTER DEFAULT PRIVILEGES IN SCHEMA pna REVOKE EXECUTE ON FUNCTIONS FROM PUBLIC;

COMMIT;

-- Paso MANUAL adicional: crear/elegir un usuario LOGIN propio de la API y asociarlo:
-- GRANT pna_runtime TO "TU_USUARIO_DE_CONEXION";
-- No usar anon, authenticated, service_role ni el propietario de migraciones como login de la API.
-- Guardar su password solo en secretos/variables locales. Ver docs/SUPABASE_ETAPA_4.md.
