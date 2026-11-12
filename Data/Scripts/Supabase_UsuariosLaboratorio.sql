-- SOLO LABORATORIO. Ejecucion manual en SQL Editor despues de InicialPostgreSql.
-- Conserva las dos cuentas de ejemplo del proyecto SQL Server y sus hashes Identity.
-- No reemplaza cuentas existentes, no cambia roles/passwords ni reactiva usuarios.
BEGIN;

-- Serializa dos ejecuciones de esta carga; no cambia permisos ni crea roles PostgreSQL.
SELECT pg_advisory_xact_lock(1347305793, 2);

INSERT INTO pna.usuarios
    (nombre, apellido, email, password_hash, rol, fecha_creacion, eliminado, fecha_eliminacion)
SELECT 'Administrador', 'Nexus', 'admin@pacificnexus.test',
       'AQAAAAIAAYagAAAAEFU7mwItGWNLpJqEAQkzmTX/4/mVeb0EUCH+9VALyv72rAoSbFSryWD8aqGr/1sfAw==',
       'admin', CURRENT_TIMESTAMP, FALSE, NULL
WHERE NOT EXISTS (
    SELECT 1 FROM pna.usuarios WHERE lower(btrim(email)) = 'admin@pacificnexus.test'
)
ON CONFLICT (email) DO NOTHING;

INSERT INTO pna.usuarios
    (nombre, apellido, email, password_hash, rol, fecha_creacion, eliminado, fecha_eliminacion)
SELECT 'Josue', 'Laboratorio', 'josue@pacificnexus.test',
       'AQAAAAIAAYagAAAAEKHJq6eC3ePmaGDplniohxlMcjr9hR9WD7Dlw2BL/TYMxZ/reQzGCuDazfdLXxdEJg==',
       'cliente', CURRENT_TIMESTAMP, FALSE, NULL
WHERE NOT EXISTS (
    SELECT 1 FROM pna.usuarios WHERE lower(btrim(email)) = 'josue@pacificnexus.test'
)
ON CONFLICT (email) DO NOTHING;

COMMIT;

-- Mostrar solo datos de las cuentas; nunca hashes o tokens.
SELECT usuario_id, nombre, apellido, email, rol, NOT eliminado AS activo
FROM pna.usuarios
WHERE lower(btrim(email)) IN ('admin@pacificnexus.test', 'josue@pacificnexus.test')
ORDER BY email;
