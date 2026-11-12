# Supabase: revisión y preparación (etapa 1)

Fecha: 2026-11-10. Rama observada en `.git/HEAD`: `feature/SupaBase`.

Esta entrega es una revisión del código y una propuesta técnica para las siguientes etapas. El backend continúa utilizando SQL Server. No se cambiaron paquetes, conexión, entidades, consultas, migraciones ni datos. No se probó una conexión con Supabase.

## 1. Estado comprobado

- Un proyecto `Vuelos_Backend.csproj`, dirigido a .NET 10; no se encontró un archivo de solución en la raíz revisada.
- EF Core SQL Server y Design están declarados en versión 10.0.14. No está declarado el proveedor PostgreSQL.
- `Program.cs` configura `UseSqlServer` con `ConnectionStrings:testConnection`.
- `Data/AppDbContext.cs` contiene 15 DbSet; hay 16 archivos de configuración que incluyen 15 declaraciones ToTable, 23 llamadas HasCheckConstraint y 3 HasFilter. Las dos configuraciones genéricas aplican estado y concurrencia a varias entidades.
- Existen 10 migraciones SQL Server y un snapshot, hasta `20261109000554_DesgloseIvaReservas`. Su presencia no demuestra que estén aplicadas en la base.
- No se encontraron archivos `.sql` en `Data` ni archivos en `Data/Scripts`; para este inventario se revisaron el SQL de los repositorios y las migraciones fuente. No se inspeccionó el esquema real de SQL Server.
- Hay cambios previos sin confirmar, incluida la limpieza de catálogos y archivos generados en bin/obj. Esta revisión no los revierte, confirma ni incorpora a otro commit. La rama no aísla por sí sola cambios sin confirmar: registrar una base conocida antes de iniciar cambios de proveedor.
- Las pruebas actuales contienen `UseSqlServer`, comparaciones de SQL con corchetes y expectativas sobre migraciones SQL Server. Deben adaptarse; no bastará con cambiar Program.cs.

## 2. Inventario de archivos afectados

| Archivo o grupo | Dependencia encontrada | Trabajo posterior |
| --- | --- | --- |
| `Vuelos_Backend.csproj` | Paquete EF SQL Server | Incorporar proveedor Npgsql compatible y retirar dependencias SQL Server cuando no queden consumidores. Aislar antes las migraciones antiguas. |
| `Program.cs` | UseSqlServer y testConnection | UseNpgsql y una clave nueva exclusiva de PostgreSQL. |
| `Data/AppDbContext.cs` | Descubrimiento de mapeos; prohibición de borrado físico | Conservar relaciones y prohibición; configurar generación de versiones y esquema destino. |
| `Data/Configurations/**/*.cs` | dbo, tipos SQL Server, UseIdentityColumn, defaults, CHECK, filtros, RowVersion | Adaptar todas las configuraciones y revisar precisión, nulabilidad y comportamiento de cada restricción. |
| `Data/BloqueoAdministrativo.cs` | sp_getapplock y THROW 51001 | Advisory lock de transacción y límite de espera equivalente. |
| `Repositories/Vuelos/VueloAdminRepository.cs` | UPDLOCK/HOLDLOCK, SQL con dbo y SqlException | Bloqueos PostgreSQL, consultas parametrizadas y traducción de SQLSTATE. |
| `Repositories/Reservas/ReservaRepository.cs` | Bloqueos de vuelo/reserva y códigos SQL Server | Mantener serialización por vuelo y errores públicos de disponibilidad/concurrencia. |
| `Repositories/Reservas/CicloReservaRepository.cs` | Deadlock 1205 y dependencia del bloqueo del vuelo | Adaptar errores y conservar expiración transaccional con protección PayPal. |
| `Repositories/Catalogos/CatalogosAdminRepository.cs` | SqlClient, errores de bytes/longitudes, duplicados y FK | Adaptar conflictos y revisar conservación de caracteres y metadatos importados. |
| `Repositories/Catalogos/BusquedaCatalogosRepository.cs` y listados administrativos | Contains, StartsWith y ordenación dependientes de collation | Definir comparación sin distinguir mayúsculas, escapar patrones si se usa ILIKE y probar acentos/Unicode. No prometer equivalencia de acentos sin conocer la collation de origen. |
| `Repositories/Usuarios/UsuarioRepository.cs` | Duplicados 2601/2627 y nombre UQ_usuarios_email | Usar SQLSTATE y ConstraintName; preservar unicidad también ante registros simultáneos. |
| `Services/Autenticacion/AutenticacionService.cs` | System.Data.SqlTypes.SqlDateTime para redondeo DATETIME | Retirar redondeo SQL Server y devolver fechas UTC con la precisión persistida en PostgreSQL. |
| `Services/Vuelos/VueloAdminService.cs` | Versión SHA256 basada en ticks de las fechas | Verificar ida y vuelta de precisión; responder con valores persistidos para evitar versiones inválidas después de crear/editar. |
| `Models/Common/ICatalogoAdministrable.cs`, entidades con RowVersion y `Models/Reservas/DTOs/VersionFilaAttribute.cs` | byte[] y Base64 de exactamente 8 bytes | Mantener contrato; cambiar la generación en la base, no quitar la concurrencia. |
| `Repositories/Pagos/PagoPayPalRepository.cs` | LINQ y SaveChanges compartidos con reservas | No necesita una integración PayPal nueva; probar triggers, unicidad e idempotencia con el nuevo proveedor. |
| `Repositories/Autenticacion/TokenRepository.cs`, lecturas de vuelos y ventas | LINQ | Verificar traducción, comparación de tokens, sumas y fechas; no reescribir sin necesidad. |
| `Data/Migrations/**` | Snapshot, operaciones y SQL específicos de SQL Server | Historial PostgreSQL independiente; nunca aplicar las migraciones SQL Server sobre Supabase. |
| `Tests/*Checks.cs.txt`, `Tests/VerificarVuelosAdmin.ps1` y guías de ejecución | Proveedor SQL Server, SQL esperado y fuente de migraciones | Adaptar verificaciones de proveedor y añadir pruebas reales de transacciones antes de aceptar PostgreSQL. |

Las entidades de aeropuertos, aviones, categorías y asientos físicos siguen siendo necesarias aunque no tengan CRUD manual. Se conservan las consultas y las importaciones; la migración de proveedor no restaura endpoints retirados.

## 3. Decisiones propuestas para implementar

### 3.1. Arquitectura y conexión

- Mantener Flutter → API ASP.NET Core → PostgreSQL de Supabase mediante EF Core/Npgsql.
- Mantener autenticación propia, tokens opacos, roles, propietarios de reservas, Swagger, PayPal y CORS. No adoptar Supabase Auth, REST, Realtime ni el SDK de Supabase en este cambio.
- En esta rama preparar PostgreSQL como proveedor de destino, sin agregar selección de dos proveedores en tiempo de ejecución. La versión SQL Server se conserva en su rama.
- Clave futura: `ConnectionStrings:SupabaseConnection`; variable `ConnectionStrings__SupabaseConnection`. No reutilizar silenciosamente `testConnection` ni guardar contraseñas en archivos versionados.
- Usar la conexión PostgreSQL del panel Connect, no la URL de la Data API ni claves anon/service_role. Para la API persistente: conexión directa si la red lo permite, o Session pooler si necesita IPv4. Confirmar host, usuario y puerto del proyecto; no construirlos por suposición.
- TLS con validación del servidor (`SSL Mode=VerifyFull` y certificado raíz cuando corresponda). Ajustar límites del pool a las conexiones disponibles del proyecto; no fijar un número sin conocer su capacidad.
- Separar usuario de migraciones (DDL) y usuario de ejecución (permisos necesarios sobre tablas/secuencias, sin DDL). No configurar estos usuarios automáticamente en esta etapa.
- Esquema propuesto: `pna`, no expuesto en Data API. Conservar inicialmente nombres explícitos de tablas y columnas, incluidos los identificadores PascalCase de Tokens/PayPal con comillas correctas. No tocar esquemas administrados por Supabase (`auth`, `storage`, etc.). Deshabilitar Data API si el proyecto no la utiliza; si la necesita para otras partes, mantener pna fuera de los esquemas expuestos y revisar grants. Los tokens propios no equivalen a identidades de Supabase RLS.

### 3.2. Tipos y restricciones

| Origen | Destino propuesto |
| --- | --- |
| int / bigint | integer / bigint; IDs autogenerados con identidad BY DEFAULT |
| bit | boolean; reescribir expresiones `= 1` como booleanas |
| varchar/nvarchar con longitud | character varying con la misma longitud; UTF-8 |
| varchar(max)/nvarchar(max) | text, excepto JSON de origen |
| char(3) | Mantener longitud de códigos y probar padding/comparación; preferir varchar(3) si evita padding sin cambiar el contrato |
| decimal(p,s) | numeric(p,s), conservando precio USD, IVA y totales |
| float para coordenadas | double precision, no real |
| Guid / uniqueidentifier | uuid; conservar IDs PayPal generados en la aplicación |
| datetime/datetime2 de instantes operativos | timestamp with time zone, con DateTime UTC |
| GETDATE/GETUTCDATE | Default PostgreSQL de instante actual para registros nuevos; no reinterpretar historia |
| ISJSON(datos_origen_json) | Columna json mapeada como string; valida JSON y conserva el texto para las comparaciones actuales de importación |
| rowversion | bytea de 8 bytes generado por trigger (ver siguiente sección) |

Reescribir corchetes y calificadores SQL Server; no basta con reemplazar nombres de tipos. Revisar los 23 CHECK y los 3 filtros de índices, incluidas las asignaciones activas de asiento y órdenes/capturas PayPal no nulas. Preservar FK sin cascada física, nulabilidad, estados y valores por defecto. Mantener los tres campos IVA históricos en NULL y no recalcular pagos.

Registro/login ya normalizan el correo a minúsculas; revisar los datos anteriores antes de imponer unicidad equivalente. Códigos e identificadores comerciales requieren normalización coherente en consultas y restricciones, no solo en el formulario. Tokens de sesión e identificadores PayPal requieren comparación exacta, sin convertirlos en búsquedas insensibles a mayúsculas.

PostgreSQL conserva microsegundos frente a los ticks de 100 ns de .NET. Revisar retorno de creación/edición de vuelos, huella Version y expiración del login usando el valor guardado o una normalización común. Las fechas anteriores generadas con GETDATE no garantizan UTC: si se transfieren datos, determinar su zona antes de convertirlos.

### 3.3. Concurrencia sin cambiar Flutter

La opción recomendada para este proyecto es conservar `byte[] RowVersion` y el JSON Base64 de 8 bytes. No utilizar directamente xmin: Npgsql lo representa como uint y requeriría adaptar la representación existente.

Diseño a materializar y probar en etapas 2–4:

1. Columna bytea NOT NULL con restricción octet_length = 8 en los cuatro catálogos, reservas, asientos_vuelo y pagos_paypal (7 tablas).
2. Una secuencia bigint compartida, NO CYCLE, y trigger BEFORE INSERT OR UPDATE que asigna un nuevo valor de exactamente 8 bytes. Conversión binaria determinista de la secuencia; no timestamp ni contador basado en MAX(). Permitir saltos de secuencia por rollback.
3. Configurar en EF como token de concurrencia generado al insertar/actualizar, sin enviar el valor de la aplicación. Comprobar que RETURNING recupera la nueva versión y que los UPDATE siguen comparando la versión anterior.
4. El trigger debe cubrir escrituras de EF, ExecuteUpdate y SQL directo. Las migraciones deben incluir función, secuencia, triggers y permisos; no solo crear columnas.
5. Probar dos escrituras con la misma versión: solo una debe prosperar; el cliente recibe un conflicto y obtiene una versión nueva mediante GET. Conservar OriginalValue y actualizaciones forzadas existentes.
6. Si se trasladan registros, generar versiones nuevas, no copiar versiones SQL Server como si fueran intercambiables. El cliente debe volver a consultar los recursos después del cambio de base.

La versión SHA256 de programación de vuelos es otro mecanismo y se conserva; no sustituirla por RowVersion accidentalmente.

### 3.4. Bloqueos y errores

- Sustituir el bloqueo administrativo por `pg_advisory_xact_lock` con clave fija/documentada y límite de espera local a la transacción. No usar locks de sesión ni GetHashCode para calcular claves.
- Para administración: bloqueo administrativo → avión → vuelo → reservas/asientos en orden estable. Para reservas/pagos/expiración: vuelo → reserva → asientos en orden estable. Mantener todos los bloqueos hasta commit/rollback y revisar todos los participantes del protocolo.
- Obtener bloqueos de filas mediante consultas parametrizadas `FOR UPDATE`; para lecturas de asientos físicos decidir el bloqueo compartido necesario dentro del mismo protocolo administrativo. No tratar HOLDLOCK como una simple sustitución de palabras: las filas inexistentes y los rangos no quedan protegidos igual. El bloqueo de avión existente serializa las altas y la comprobación de horarios superpuestos.
- Bloquear la tabla/alias objetivo antes de cargar relaciones; evitar FOR UPDATE sobre el lado nullable de un LEFT JOIN generado por Include. Validar el SQL real que compone EF.
- Conservar aislamiento ReadCommitted con el protocolo de bloqueos explícitos y restricciones únicas. No subir automáticamente todo a Serializable ni agregar reintentos globales.
- Mapear PostgresException.SqlState y ConstraintName: 23505 (único), 23503 (FK), 23514 (CHECK), 22001 (longitud), 40P01 (deadlock), 40001 (serialización), 55P03 (lock no disponible/timeout). Diferenciar cancelación/timeout y no traducir cualquier fallo a un conflicto genérico.
- Mantener los códigos públicos actuales cuando representan la misma causa, incluidos correo duplicado, asiento ocupado y versión desactualizada; no analizar mensajes localizados para identificar constraints.
- PayPal mantiene sus fases: confirmar protección en base → HTTP sin transacción abierta → confirmar resultado. Un error de red/base o de commit no debe iniciar una segunda captura. Conciliar mediante consulta oficial y conservar operaciones inciertas para revisión.

## 4. Migraciones y conservación de datos

1. Conservar los archivos SQL Server como referencia en esta rama y el historial original en la rama SQL Server. Antes de usar comandos EF para PostgreSQL, archivar las migraciones y snapshot SQL Server bajo `Data/Migrations/SqlServer` y excluir sus fuentes de compilación/descubrimiento. No mantener dos snapshots activos del mismo contexto.
2. Crear el conjunto PostgreSQL en `Data/Migrations/PostgreSql`, con un único snapshot activo de AppDbContext y el historial EF en el esquema pna. Una base Supabase nueva comienza con su propia migración inicial completa; no se copia __EFMigrationsHistory de SQL Server.
3. La primera migración PostgreSQL debe incluir el modelo final actual, las 7 versiones generadas, índices, constraints, identidad, permisos necesarios y reversión revisada. No reproducir pasos históricos de renombrado que no corresponden a una base nueva.
4. Separar datos iniciales necesarios para clases de asiento de usuarios de laboratorio. No ejecutar ni trasladar automáticamente la migración histórica de usuarios de prueba; preparar una forma explícita de habilitarlos si se necesitan.
5. Si Supabase ya contiene tablas de negocio, inspeccionar su esquema e historial antes de crear o declarar una línea base. No marcar como aplicado un esquema que no se haya comprobado.
6. La etapa 6 decidirá base vacía o transferencia. En una transferencia conservar IDs, FK, hashes de contraseña, precios congelados, IVA, reservas, historial y referencias PayPal. Ajustar las secuencias de identidad después de cargar IDs explícitos. Resolver fechas y normalizaciones antes del corte.
7. Preparar reinicio de sesiones y recarga de versiones en el corte, sin reutilizar tokens/versiones de dos bases diferentes. No operar reservas y pagos simultáneamente contra ambas bases. Volver a SQL Server después de aceptar escrituras en Supabase requiere reconciliar esas escrituras; cambiar de rama no copia datos hacia atrás.

## 5. Orden de las próximas entregas

| Etapa | Entrega concreta | Condición de salida |
| --- | --- | --- |
| 2. Proveedor y modelo | Paquete aprobado por el equipo, UseNpgsql, configuración ejemplo sin secretos, archivo de contexto de diseño si hace falta, mapeos y archivo histórico SQL Server fuera del descubrimiento activo | Compilación y validación del modelo PostgreSQL sin abrir una conexión ni generar todavía la migración final. La API no se declara lista para operar: aún faltan bloqueos/repositorios. |
| 3. Persistencia | SQL parametrizado PostgreSQL, locks, SQLSTATE, autenticación UTC, consultas/comparaciones y manejo de versiones | Consultas traducibles y comportamiento de errores documentado; ninguna captura nueva por reintentos de base. |
| 4. Base de prueba | Migración inicial PostgreSQL completa, generación de versiones, SQL revisable y guía manual | Tú aplicas la migración en una base de prueba; verificar esquema, triggers, permisos e historial antes de habilitar los workers. |
| 5. Validación | Pruebas por bloques y concurrencia real con conexiones independientes | Registro/login; imports repetidos; vuelos superpuestos; dos reservas del mismo asiento; RowVersion; expiración/cancelación/captura simultáneas; webhook repetido; IVA y ventas. PayPal Sandbox, sin dinero real. |
| 6. Corte | Decisión de transferencia, validación de recuentos/importes/FK, documentación Flutter y retorno | Aprobación del equipo con datos verificados y procedimiento que considere escrituras posteriores al corte. |

Al cambiar de etapa conservar las comprobaciones de negocio independientes del proveedor. Reemplazar únicamente las expectativas específicas de SQL Server; no borrar pruebas de seguridad o concurrencia para hacer pasar la suite.

## 6. Dependencias y datos pendientes

- Instalación manual futura: `Npgsql.EntityFrameworkCore.PostgreSQL`, versión elegida por el equipo que sea compatible con EF Core 10.0.14 y .NET 10; revisar sus requisitos reales antes de instalar. No se eligió ni instaló una versión en esta entrega.
- Conservar EF Core Design. El driver Npgsql llega con el proveedor; no agregar SDK Supabase ni paquetes adicionales por defecto.
- Falta confirmar la versión PostgreSQL del proyecto Supabase, endpoint/modo de conexión, capacidad de conexiones y certificados. No se necesitan secretos para completar esta revisión; deben configurarse localmente cuando se pruebe conexión.
- Falta decidir si se trasladan datos existentes. Tampoco se conoce el historial de migraciones realmente aplicado en SQL Server.
- Las skills disponibles se evaluaron: esta etapa no añade endpoints, manipula archivos Office ni cambia arquitectura de capas; no se requiere una skill especializada para modificar esos elementos ni instalar skills.

## 7. Verificación de esta entrega

- Lectura de AGENTS.md.txt, PROYECTO_API.md, csproj, contexto, mapeos, repositorios afectados, servicios de autenticación/programación, contratos de versión y pruebas.
- Confirmación de rama por .git/HEAD y de cambios previos mediante git status; no se realizó checkout, commit, limpieza ni reversión.
- `git diff --check --ignore-submodules` detectó una línea vacía final preexistente en `Models/Catalogos/DTOs/CatalogosAdminDTOs.cs:35`; no se corrigió en esta entrega documental. La revisión del documento nuevo se realiza por separado.
- Revisión de documentación primaria enlazada a continuación. Las decisiones de implementación son propuestas del proyecto, no pruebas de comportamiento ya implementado.
- Único archivo agregado en esta etapa: este documento. Sin cambio de ejecución, no se volvió a compilar ni ejecutar la suite; los resultados anteriores no se presentan como nuevas pruebas PostgreSQL.
- Sin conexiones a bases, ejecución de SQL/migraciones, restauración/instalación de paquetes ni llamadas de pago.

## Referencias técnicas

- [Supabase: conexiones PostgreSQL y poolers](https://supabase.com/docs/guides/database/connecting-to-postgres).
- [Supabase: proteger o deshabilitar Data API](https://supabase.com/docs/guides/api/securing-your-api).
- [Npgsql: proveedor EF Core](https://www.npgsql.org/efcore/index.html).
- [Npgsql: tokens de concurrencia y xmin](https://www.npgsql.org/efcore/modeling/concurrency.html).
- [Npgsql: fechas UTC y precisión](https://www.npgsql.org/doc/types/datetime.html).
- [Npgsql: identidad y datos precargados](https://www.npgsql.org/efcore/modeling/generated-properties.html).
- [PostgreSQL: bloqueos de filas y advisory locks](https://www.postgresql.org/docs/18/explicit-locking.html).
- [PostgreSQL: secuencias atómicas](https://www.postgresql.org/docs/18/functions-sequence.html).
- [PostgreSQL: triggers de datos](https://www.postgresql.org/docs/18/plpgsql-trigger.html).
- [PostgreSQL: conservación textual de json frente a jsonb](https://www.postgresql.org/docs/18/datatype-json.html).
- [EF Core: migraciones por proveedor](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers).
