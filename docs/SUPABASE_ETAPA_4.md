# Supabase: migración inicial PostgreSQL (etapa 4)

Fecha: 2026-11-11. Rama: `feature/SupaBase`.

Actualización de pruebas: ver [SUPABASE_ETAPA_5.md](SUPABASE_ETAPA_5.md). La suite offline completa ya está adaptada; aplicación y verificación contra Supabase siguen sin confirmarse en esta sesión.

**Preparada, no aplicada.** No se abrió una conexión a Supabase/SQL Server, no se ejecutó SQL contra una base y no se instalaron/restauraron paquetes. Los pasos de aplicación que siguen los ejecuta el equipo manualmente en una base de prueba.

## Archivos entregados

| Archivo | Uso |
| --- | --- |
| `Data/Migrations/PostgreSql/20261111050328_InicialPostgreSql.cs` | Migración inicial del modelo actual, controles de instalación, versiones y catálogo inicial. |
| `Data/Migrations/PostgreSql/20261111050328_InicialPostgreSql.Designer.cs` | Metadatos de EF para la migración. |
| `Data/Migrations/PostgreSql/AppDbContextModelSnapshot.cs` | Único snapshot activo, exclusivo de PostgreSQL. |
| `Data/Scripts/Supabase_InicialPostgreSql.sql` | SQL idempotente generado desde la migración compilada, para revisión/aplicación manual alternativa a EF. |
| `Data/Scripts/Supabase_PermisosRuntime.sql` | Configuración manual del grupo de permisos de la API, sin usuarios LOGIN ni passwords. |
| `Data/Scripts/Supabase_VerificarInstalacion.sql` | Consultas de solo lectura para verificar instalación y privilegios efectivos. |
| `Tests/MigracionPostgreSql.cs.txt` | Verificaciones offline de migración, restricciones, orden de triggers/seed, Down y correspondencia del SQL entregado. |

Los 21 archivos históricos SQL Server siguen en su ubicación y con su contenido original, excluidos de compilación. La herramienta EF escribió inicialmente el nuevo snapshot en la ruta histórica; se copió el nuevo a la carpeta PostgreSql y se restauró el histórico comprobando Git. El ensamblado solo descubre InicialPostgreSql y su snapshot PostgreSQL. Al generar futuras migraciones, revisar siempre la ubicación del snapshot y el diff del histórico; no aceptar que EF sobrescriba esos archivos.

## Qué crea la migración

- Esquema privado `pna`, 15 tablas de negocio y el historial EF `pna."__EFMigrationsHistory"`.
- Claves, relaciones sin cascada física, identidad de IDs, tipos PostgreSQL, longitudes, índices y restricciones del modelo completo actual. Incluye datos completos de aeropuertos/aviones, tokens propios, reservas, precios congelados, IVA nullable histórico, compras simuladas históricas y pagos/eventos PayPal.
- Un índice único de asignación activa de asiento y unicidad de órdenes/capturas PayPal.
- Una secuencia bigint `pna.row_version_seq`, sin ciclo, compartida por las siete tablas con versión.
- Una función `pna.generar_version_fila()` SECURITY INVOKER con search_path restringido a pg_catalog y siete triggers BEFORE INSERT OR UPDATE.
- Cada trigger reemplaza la versión entrante por un valor de secuencia codificado en exactamente 8 bytes. Mantiene Base64 en la API, cubre SaveChanges, ExecuteUpdate y SQL directo, y conserva el nombre `RowVersion` de pagos_paypal. Los saltos de secuencia por rollback son normales; nunca se reinicia para reutilizar versiones.
- Catálogo inicial: Primera Clase, Segunda Clase y Tercera Clase, activas. Se insertan después de crear los triggers; ID y versión los genera la base. Consultar los IDs reales mediante los GET de catálogo.

**No crea usuarios, contraseñas, vuelos, aviones, aeropuertos, reservas ni pagos de ejemplo. No traslada datos de SQL Server.** Las categorías son el único catálogo precargado necesario para la importación de aviones.

La representación binaria es de longitud fija; no debe interpretarse como fecha ni incrementarse en Flutter. La función cambia únicamente la columna de versión indicada por el trigger, preservando los demás valores de la fila.

## Protección de instalación e historial

La migración inicial comprueba que `pna` no contenga tablas/relaciones, funciones o tipos de negocio anteriores. Tolera únicamente la estructura de historial EF creada por las herramientas antes del Up. Si encuentra un esquema ocupado, falla sin adoptar datos ni sobrescribir objetos. Si ya existe otra instalación o un historial diferente, detenerse y preparar una estrategia específica; no borrar el historial para forzar la ejecución.

El SQL idempotente consulta el identificador `20261111050328_InicialPostgreSql` y registra la aplicación solo al final. Con historial íntegro, volver a ejecutar ese SQL no repite tablas, triggers ni categorías. No equivale a validar una instalación cuyo historial haya sido manipulado.

El DDL de negocio y los datos iniciales se ejecutan en transacción. EF puede crear antes el esquema/tabla de historial: ante un fallo pueden quedar vacíos, y eso no significa que la migración se haya aplicado.

**Down está bloqueado con RAISE EXCEPTION y no contiene DROP.** No se admite un rollback destructivo de esta migración inicial ni se retira su registro del historial al fallar. Para corregir una instalación con datos, preparar una migración hacia adelante. Volver a una rama de código no traslada escrituras entre bases.

## Aplicación manual en una base de prueba

### 1. Preparar el destino

1. Detener la API/workers antes de empezar y mantener una copia de la versión SQL Server.
2. Confirmar el proyecto Supabase y su versión PostgreSQL. Esta entrega no comprobó la versión ni si el destino está vacío.
3. Usar una base de prueba y un esquema `pna` nuevo/sin datos de negocio. Las tablas administradas por Supabase en auth/storage y otros esquemas se conservan; no necesitan estar vacías.
4. Configurar localmente `ConnectionStrings:SupabaseConnection` o `ConnectionStrings__SupabaseConnection` con el usuario propietario de migraciones. Usar la conexión PostgreSQL del panel Connect, directa o Session pooler según la red, con TLS y validación de certificado. No usar una URL REST ni claves anon/service_role.
5. Las herramientas EF usan `AppDbContextFactory`, no ejecutan launchSettings. Si la conexión solo está en ese perfil, configurarla también en secretos de usuario o variables del proceso antes de ejecutar EF. No publicar credenciales en archivos, scripts ni capturas.
6. Revisar el SQL de `Data/Scripts/Supabase_InicialPostgreSql.sql`.

### 2. Elegir un mecanismo de aplicación

**Opción A: EF desde la raíz del proyecto.** Los siguientes comandos son manuales; no se ejecutaron en esta entrega:

```powershell
dotnet build .\Vuelos_Backend.csproj --no-restore
dotnet ef database update InicialPostgreSql --project .\Vuelos_Backend.csproj --no-build
```

La compilación previa es necesaria: la verificación automática de esta entrega usa `obj/verificacion-supabase`, mientras EF manual normalmente lee `bin/Debug/net10.0`. No ejecutar --no-build contra binarios antiguos.

**Opción B: script.** Abrir `Data/Scripts/Supabase_InicialPostgreSql.sql` en el editor SQL de la base de prueba y ejecutar el archivo completo, conservando su transacción, guardas e historial. No sustituirlo por GenerateCreateScript del contexto: ese DDL de modelo no incluye la función, triggers, seed ni protecciones añadidos a esta migración.

Elegir una opción. No ejecutar scripts SQL Server ni copiar su __EFMigrationsHistory. Ante un error, guardar el mensaje completo sin credenciales y revisar antes de continuar; no aplicar solo fragmentos saltándose la guarda.

### 3. Separar permisos de migraciones y ejecución

La migración retira permisos PUBLIC y los de anon/authenticated/service_role sobre los objetos de pna cuando esos roles existen. No cambia otros esquemas. Mantener pna fuera de los esquemas expuestos en Data API; si el proyecto no utiliza Data API, puede deshabilitarse desde el panel.

Ejecutar manualmente `Data/Scripts/Supabase_PermisosRuntime.sql` como propietario de los objetos y con permisos de administración de roles. Crea un grupo `pna_runtime` NOLOGIN, con:

- USAGE del esquema/secuencias y EXECUTE de la función de versión.
- SELECT, INSERT y UPDATE de tablas de negocio.
- Sin CREATE de objetos, DELETE/TRUNCATE ni modificación del historial EF.

Crear/elegir por separado un usuario LOGIN propio para conectar la API, asociarlo al grupo mediante el GRANT indicado en el script y configurar su password solo localmente. Revisar que no herede permisos elevados de otros roles y comprobar los permisos efectivos. No usar como runtime el propietario de migraciones. Los privilegios por defecto del script solo afectan objetos futuros creados por el usuario que lo ejecuta; repetir la revisión si cambia el propietario de migraciones.

`pna_runtime` es un rol PostgreSQL, **no** el rol admin de la aplicación. No sustituye la autorización de endpoints ni crea usuarios administradores. Para iniciar el laboratorio con una base vacía: registrar una cuenta por el flujo existente y asignarle el rol de aplicación admin de forma manual y controlada en la base de prueba, verificando su identificador. No se añade un endpoint público para elevar privilegios.

### 4. Verificar antes de operar

Ejecutar manualmente `Data/Scripts/Supabase_VerificarInstalacion.sql`. Primero como propietario para revisar estructura; luego repetir las consultas finales con el LOGIN de la API para verificar los permisos efectivos.

Esperado: una migración PostgreSQL, 15 tablas de negocio más el historial, siete triggers habilitados, siete columnas de versión bytea NOT NULL, secuencia sin ciclo y tres categorías activas con versiones de 8 bytes. El usuario runtime debe leer/insertar/actualizar, generar versiones y no poder borrar/truncar/crear objetos ni actualizar el historial.

La fase de verificación de escrituras, retorno de RowVersion, bloqueos entre conexiones, importaciones repetidas y PayPal Sandbox corresponde a la etapa 5. No declarar el backend operativo solo por ver las tablas.

## Comprobaciones locales

Se preparó la migración con EF, se compiló y se generó el SQL idempotente offline usando una conexión ficticia no abierta. Los tests no ejecutan DDL ni PL/pgSQL en un servidor; por tanto, su aceptación por PostgreSQL y su comportamiento con el rol real requieren la aplicación/pruebas manuales.

```powershell
$script = Get-Content Tests/VerificarModeloPostgreSql.ps1 -Raw -Encoding UTF8
& ([scriptblock]::Create($script)) -ProjectRoot $PWD.Path -Comprobacion Modelo
& ([scriptblock]::Create($script)) -ProjectRoot $PWD.Path -Comprobacion Migracion
& ([scriptblock]::Create($script)) -ProjectRoot $PWD.Path -Comprobacion Persistencia
```

Resultado ejecutado: **466 comprobaciones locales aprobadas** (259 de modelo, 83 de migración/SQL, 67 de persistencia/fechas/pagos simulados y 57 regresiones de descuentos/CORS/Swagger). La prueba de migración comprueba que el SQL entregado coincide exactamente con la migración compilada, que el snapshot no tiene cambios pendientes y que la función/triggers se crean antes del seed. La compilación normal de la API no tiene errores ni advertencias; los compiladores Roslyn de las pruebas conservan la advertencia CS1702 ya documentada sobre referencias EF, sin cambiar paquetes.

No hay nuevas dependencias de aplicación que instalar. Se utilizaron .NET/EF tools 10.0.14 y Npgsql 10.0.3 ya instalados por el equipo; no se actualizaron.

## Referencias

- [PostgreSQL: funciones trigger y NEW](https://www.postgresql.org/docs/18/plpgsql-trigger.html).
- [PostgreSQL: jsonb_populate_record y jsonb_build_object](https://www.postgresql.org/docs/18/functions-json.html).
- [PostgreSQL: secuencias y nextval atómico](https://www.postgresql.org/docs/18/functions-sequence.html).
- [Supabase: roles PostgreSQL](https://supabase.com/docs/guides/database/postgres/roles).
