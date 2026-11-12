-- SOLO LECTURA. Ejecutar manualmente, primero como propietario de migraciones.
-- No constituye una prueba de escrituras ni de concurrencia real (etapa 5).
SELECT current_database() AS base_actual, current_user AS usuario_actual,
       current_setting('server_version') AS version_postgresql;

SELECT "MigrationId", "ProductVersion"
FROM pna."__EFMigrationsHistory" ORDER BY "MigrationId";

-- Esperado: 15 tablas de negocio y 1 tabla de historial EF.
SELECT table_name FROM information_schema.tables
WHERE table_schema = 'pna' AND table_type = 'BASE TABLE' ORDER BY table_name;

-- Esperado: siete triggers habilitados, BEFORE INSERT OR UPDATE, una funcion SECURITY INVOKER.
SELECT c.relname AS tabla, t.tgname AS trigger, t.tgenabled AS habilitado,
       p.prosecdef AS security_definer, pg_catalog.pg_get_triggerdef(t.oid) AS definicion
FROM pg_catalog.pg_trigger t
JOIN pg_catalog.pg_class c ON c.oid = t.tgrelid
JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
JOIN pg_catalog.pg_proc p ON p.oid = t.tgfoid
WHERE n.nspname = 'pna' AND NOT t.tgisinternal ORDER BY c.relname;

-- Esperado: siete columnas bytea NOT NULL; pagos_paypal conserva el nombre RowVersion.
SELECT table_name, column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_schema = 'pna' AND column_name IN ('row_version', 'RowVersion') ORDER BY table_name;

SELECT sequencename, data_type, cycle, cache_size FROM pg_catalog.pg_sequences
WHERE schemaname = 'pna' AND sequencename = 'row_version_seq';

-- Esperado: tres clases activas, con versiones de 8 bytes; IDs generados, no fijados.
SELECT categoria_id, nombre, activo, octet_length(row_version) AS bytes_version
FROM pna.categorias_asiento ORDER BY categoria_id;

-- Esperado: ninguna fila. Conserva IVA, estados, coordenadas y versiones.
SELECT c.relname AS tabla, con.conname AS restriccion
FROM pg_catalog.pg_constraint con
JOIN pg_catalog.pg_class c ON c.oid = con.conrelid
JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
WHERE n.nspname = 'pna' AND NOT con.convalidated;

-- Filtros de asignacion activa y orden/captura no nulas.
SELECT tablename, indexname, indexdef FROM pg_catalog.pg_indexes
WHERE schemaname = 'pna' AND indexdef ILIKE '% WHERE %' ORDER BY tablename, indexname;

-- Esperado: false para los roles Supabase que existan. No usar estos roles para la API propia.
SELECT rolname, has_schema_privilege(oid, 'pna', 'USAGE') AS acceso_pna
FROM pg_catalog.pg_roles WHERE rolname IN ('anon', 'authenticated', 'service_role');

-- Repetir estas consultas conectado como el LOGIN propio de la API, despues de asignar pna_runtime.
-- Se comprueban privilegios efectivos, incluidos los heredados de otros roles.
SELECT has_schema_privilege(current_user, 'pna', 'USAGE') AS puede_usar_esquema,
       has_schema_privilege(current_user, 'pna', 'CREATE') AS puede_crear_objetos;
SELECT has_table_privilege(current_user, 'pna.reservas', 'SELECT') AS puede_consultar,
       has_table_privilege(current_user, 'pna.reservas', 'INSERT') AS puede_insertar,
       has_table_privilege(current_user, 'pna.reservas', 'UPDATE') AS puede_actualizar,
       has_table_privilege(current_user, 'pna.reservas', 'DELETE') AS puede_borrar,
       has_table_privilege(current_user, 'pna.reservas', 'TRUNCATE') AS puede_truncar,
       has_table_privilege(current_user, 'pna."__EFMigrationsHistory"', 'UPDATE') AS puede_alterar_historial;
SELECT has_sequence_privilege(current_user, 'pna.row_version_seq', 'USAGE') AS puede_generar_version,
       has_function_privilege(current_user, 'pna.generar_version_fila()', 'EXECUTE') AS puede_ejecutar_funcion;
