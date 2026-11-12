# Supabase: proveedor y modelo (etapa 2)

Estado original de entrega: cambios de código preparados; compilación y validación bloqueadas por dependencia pendiente. Rama `feature/SupaBase`, 2026-11-10.

**Actualización al implementar la etapa 3:** el equipo instaló/restauró Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3. Se sincronizó la referencia del csproj con esa versión ya presente en assets, sin instalar ni restaurar nada. La API compila y pasan las 258 comprobaciones offline del modelo. Se completó así la validación pendiente de etapa 2; los apartados inferiores describen el bloqueo original. Estado actual: [SUPABASE_ETAPA_3.md](SUPABASE_ETAPA_3.md).

## Cambios preparados

- `Configuration/BaseDatosPostgreSql.cs`: una configuración compartida con `UseNpgsql`, clave `ConnectionStrings:SupabaseConnection` e historial EF en `pna`. Sin fallback a la cadena SQL Server y sin reintentos automáticos sobre pagos.
- `Program.cs`: utiliza la configuración PostgreSQL en la inyección del contexto.
- `Data/AppDbContextFactory.cs`: las herramientas EF pueden construir el contexto sin iniciar la API, validadores de PayPal o workers. Lee appsettings del directorio del ensamblado, configuración por entorno, secretos de usuario, variables de entorno y argumentos, en ese orden de prioridad. No lee `launchSettings.json`.
- `Data/AppDbContext.cs` y los 16 archivos de configuración: esquema `pna`; integer, boolean, numeric, character varying, text, double precision y timestamp with time zone; generación de identidad según la convención de Npgsql a partir de ValueGeneratedOnAdd. Se mantienen tablas, columnas, longitudes, claves, índices, nulabilidad y FK sin cascada física.
- Restricciones CHECK y filtros adaptados a PostgreSQL, incluidas las columnas PascalCase de PayPal/Tokens entre comillas, el IVA, estados y unicidad de asientos activos. Los valores JSON nativos mantienen la validación de objeto/array equivalente a ISJSON sin ampliar el contrato a escalares.
- `datos_origen_json` se mapea a `json`, no jsonb, para conservar el texto usado en las verificaciones de importación.
- `Data/Configurations/VersionesPostgreSqlConfiguration.cs`: RowVersion sigue siendo byte[] y Base64 de 8 bytes; se configura bytea, concurrencia, generación al insertar/actualizar e ignorar escrituras de valor desde EF. Hay CHECK de longitud y declaración de trigger en las 7 tablas afectadas. **La declaración del modelo no crea el trigger físico**: secuencia, función y triggers se incluirán en la migración de etapa 4.
- `Vuelos_Backend.csproj`: excluye las migraciones/snapshot históricos SQL Server del descubrimiento de EF. Se mantienen sus 21 archivos fuente en `Data/Migrations`, sin modificar su contenido; el traslado físico fue rechazado por permisos. `Data/Migrations/SqlServer/README.md` explica esta separación lógica.

No se generó una migración PostgreSQL ni un snapshot nuevo. La carpeta activa futura será `Data/Migrations/PostgreSql`; no usar la raíz histórica para nuevas migraciones.

Se conserva por ahora `Microsoft.EntityFrameworkCore.SqlServer`: los repositorios todavía requieren SqlClient y algunas pruebas todavía usan ese proveedor. Su retiro corresponde a la adaptación posterior, cuando no queden consumidores. No se instala ni actualiza EF Core por esta entrega.

## Dependencia que debe instalar el equipo

Instalar manualmente en `Vuelos_Backend.csproj`:

**Npgsql.EntityFrameworkCore.PostgreSQL**, en una versión elegida por el equipo y compatible con EF Core 10.0.14/.NET 10. Confirmar los requisitos de la versión antes de instalar. El proveedor incorpora el driver Npgsql; no se requiere el SDK de Supabase para este diseño.

El paquete no estaba declarado en el csproj ni restaurado en project.assets.json al realizar esta entrega. No se agregó una referencia sin versión ni se eligió una versión por cuenta propia. La compilación intentada con `--no-restore` falla con CS1061 en UseNpgsql por esa ausencia. No se puede afirmar que el modelo PostgreSQL ya está validado.

La evaluación de MSBuild sí confirmó que los tres archivos nuevos de configuración/contexto están incluidos y que ninguna migración ni snapshot SQL Server se compila. Se comprobó además que los 21 archivos históricos permanecen y no tienen diferencias de contenido en Git. Se validaron la sintaxis del script PowerShell y el JSON de ejemplo; esas verificaciones no sustituyen la compilación con Npgsql.

Después de que el equipo agregue/restaure el paquete manualmente, ejecutar la comprobación de abajo. Si NuGet exige cambiar otras versiones, revisarlo con el equipo antes de actualizar; no forzar una combinación incompatible.

## Configurar la conexión cuando corresponda probarla

Ejemplo sin credenciales reales: [SUPABASE_CONFIGURACION.example.json](SUPABASE_CONFIGURACION.example.json). El ejemplo no se carga automáticamente.

La API usará `ConnectionStrings:SupabaseConnection` o `ConnectionStrings__SupabaseConnection`. Configurar el valor real localmente mediante secretos de usuario o variable de entorno. Los datos salen del panel Connect de Supabase, con conexión directa o Session pooler según la red. TLS debe validar el servidor; configurar el certificado raíz si lo necesita el entorno.

`launchSettings.json` solo proporciona variables cuando se ejecuta el perfil correspondiente. La fábrica de diseño no lo ejecuta; para comandos EF usar secretos de usuario o variables del proceso. No copiar contraseñas a esta guía, al archivo de ejemplo ni a cambios versionados.

Las credenciales existentes de SQL Server y PayPal no se modificaron.

## Comprobación preparada, sin conexión

Desde la raíz del proyecto, después de instalar/restaurar manualmente el proveedor:

```powershell
$script = Get-Content Tests/VerificarModeloPostgreSql.ps1 -Raw -Encoding UTF8
& ([scriptblock]::Create($script)) -ProjectRoot $PWD.Path
```

El script compila con `--no-restore` en `obj/verificacion-supabase` y ejecuta únicamente `Tests/ModeloPostgreSql.cs.txt`. No abre conexión, no inicia workers, no llama PayPal ni ejecuta SQL. Comprueba:

- Modelo de 15 entidades en pna y tipos de fechas UTC.
- FK sin cascada física y ausencia de tipos/expresiones SQL Server en el modelo.
- Las 7 versiones bytea generadas, sus CHECK y el contrato Base64 de 8 bytes.
- JSON textual, IVA histórico nullable y unicidad de asignación activa de asiento.
- Ausencia de migraciones/snapshot SQL Server en el ensamblado.
- Generación del DDL PostgreSQL en memoria, incluida identidad; ese SQL no se guarda ni ejecuta.
- Conexión cerrada al finalizar.

La prueba de DDL es de estructura; no sustituye la prueba real de triggers, escrituras concurrentes o transacciones. Las pruebas anteriores dependientes de SQL Server requieren adaptación y no se presentan como aprobadas para esta etapa.

## Pendientes y límites de operación

1. Instalar el proveedor manualmente y completar compilación/verificación offline. Hasta entonces la etapa 2 no cumple su condición de salida.
2. Etapa 3: repositorios todavía contienen dbo, UPDLOCK/HOLDLOCK, sp_getapplock y traducción SqlException. Autenticación todavía usa SqlDateTime; faltan normalización UTC/microsegundos y revisión de huella de programación/búsquedas. Estos cambios están fuera de esta entrega.
3. Etapa 4: preparar la migración PostgreSQL completa y los triggers, permisos e historial, revisar SQL y aplicarlo manualmente en una base de prueba.
4. Etapas 5–6: concurrencia real, PayPal Sandbox y eventual transferencia/corte.

**No iniciar todavía esta versión para operar reservas/pagos ni ejecutar database update.** Tener el modelo preparado no significa que la persistencia ya esté migrada.

No se modificó ninguna base, no se ejecutaron migraciones y no se instalaron/restauraron paquetes automáticamente.

## Fuentes técnicas

- [Npgsql: configuración del proveedor EF Core](https://www.npgsql.org/efcore/index.html).
- [Npgsql: identidad por defecto](https://www.npgsql.org/efcore/modeling/generated-properties.html).
- [Npgsql: fechas UTC](https://www.npgsql.org/doc/types/datetime.html).
- [EF Core: generación de valores](https://learn.microsoft.com/en-us/ef/core/modeling/generated-properties).

Los contratos y decisiones completas de la transición están en [SUPABASE_ETAPA_1.md](SUPABASE_ETAPA_1.md).
