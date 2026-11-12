# Supabase: repositorios, bloqueos y fechas (etapa 3)

Rama `feature/SupaBase`. Revisión y comprobaciones: 2026-11-10.

Actualización de 2026-11-11: la migración, snapshot y triggers de etapa 4 están preparados, pendientes de aplicación manual. Ver [SUPABASE_ETAPA_4.md](SUPABASE_ETAPA_4.md). Los pendientes y cifras inferiores registran la entrega original de etapa 3.

## Resultado y alcance

Adaptados los repositorios y servicios dependientes de SQL Server a PostgreSQL, conservando endpoints, autenticación propia, autorización y flujo PayPal. El proveedor y el modelo pendientes de validación de etapa 2 también se comprobaron.

**Todavía no operar reservas ni pagos con esta versión**: falta la etapa 4 (migración PostgreSQL, función/secuencia/triggers de versión, permisos y aplicación manual en una base de prueba) y la prueba real de concurrencia. No hay una migración PostgreSQL creada en esta etapa.

## Dependencia comprobada

`obj/project.assets.json` ya contenía la dependencia directa Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3 y Npgsql 10.0.3, instaladas por el equipo. El csproj en disco todavía no declaraba esa referencia; se sincronizó con la versión instalada. No se eligió otra versión ni se ejecutó instalación/restauración.

La referencia SQL Server sigue presente exclusivamente por las pruebas históricas pendientes de adaptación. Los repositorios/servicios activos revisados ya no contienen SqlClient, SqlDateTime, dbo, UPDLOCK/HOLDLOCK ni sp_getapplock. No se retira el paquete hasta adaptar los consumidores de pruebas, evitando mezclar un cambio de dependencias con esa tarea pendiente.

## Archivos y cambios

| Archivo/grupo | Cambio |
| --- | --- |
| `Data/ConsultasBloqueoPostgreSql.cs` | Consultas parametrizadas de avión, vuelo, reserva propia y asiento con FOR UPDATE; asientos físicos con FOR SHARE y orden estable. El lock se aplica dentro de la subconsulta antes de los Include/joins exteriores. |
| `Data/BloqueoAdministrativo.cs` | Advisory lock de transacción con clave fija `(1347305793, 1)`, equivalente al recurso administrativo compartido. Se libera por commit/rollback. |
| Transacciones de reservas y administración | ReadCommitted, límite local lock_timeout de 10 s y mismos protocolos de orden. Las escrituras/bloqueos protegidos rechazan llamadas sin transacción. No se agregan reintentos. |
| `Repositories/Vuelos/VueloAdminRepository.cs` | Nuevas consultas de bloqueo; conserva serialización por avión antes de comprobar solapamientos, vuelos/asientos y restricciones de pagos. |
| `Repositories/Reservas/ReservaRepository.cs` | Bloqueo por vuelo y propietario, actualización condicional de asiento con versión/estado/avión y errores PostgreSQL por constraint. Conserva OriginalValue de la versión y actualizaciones forzadas. |
| `Repositories/Reservas/CicloReservaRepository.cs` | Manejo de concurrencia PostgreSQL durante expiración; conserva protección de pagos pendientes y liberación transaccional. |
| `Data/ErroresPostgreSql.cs` | Extrae PostgresException incluso dentro de DbUpdateException y usa SqlState/ConstraintName, sin analizar texto localizado. |
| `Repositories/Usuarios/UsuarioRepository.cs` | Correo duplicado por UQ_usuarios_email/23505; búsquedas normalizadas sin ocultar la unicidad de cuentas eliminadas lógicamente. |
| Repositorios de catálogos | ILIKE para consultas sin distinguir mayúsculas y escape literal de `%`, `_` y `!`; conserva filtros, límites y proyecciones ligeras. Comprobación de matrículas/códigos existentes normalizada para importaciones. |
| `Data/FechasPostgreSql.cs`, `Data/AppDbContext.cs` | Nuevos/modificados instantes deben ser UTC y se truncan a microsegundos antes de SaveChanges/SaveChangesAsync. No se reinterpretan fechas históricas no modificadas. |
| `Services/Autenticacion/AutenticacionService.cs` | Retirado SqlDateTime; login calcula y devuelve expiración con precisión PostgreSQL. |
| `Services/Vuelos/VueloAdminService.cs` | Normaliza programación y horas reales, valida intervalos con precisión persistida y mantiene estable la huella Version después de crear/editar. |
| Pruebas y guías | Comprobación de persistencia independiente con fixtures de negocio existentes, y actualización del estado de la transición. |

## Errores y contratos

- 23505 con ConstraintName del asiento activo: `asiento_no_disponible`; código de reserva: `codigo_reserva_ocupado`; correo: CorreoDuplicadoException/409.
- 40P01 (deadlock), 40001 (serialización) y 55P03 (bloqueo no disponible/timeout): conflicto concurrente. Las transacciones administrativas conservan `conflicto_vuelo` o `conflicto_catalogo`.
- Integridad FK/CHECK/exclusión y otros únicos: conflicto según el módulo. La longitud de aeropuerto (22001) conserva el error de solicitud inválida de importación.
- Cancelación, errores de red y otros fallos de infraestructura no se convierten en conflicto de asiento ni se reintentan automáticamente. El middleware conserva sus respuestas genéricas para errores inesperados, sin exponer SQL o detalles de conexión.
- RowVersion continúa siendo Base64 de 8 bytes. PostgreSQL deberá generarlo físicamente mediante los triggers de la etapa 4; la aplicación no lo reemplaza con fechas ni elimina su verificación.
- Se mantiene la huella de programación de vuelos independiente de RowVersion.
- Se mantienen porcentajes de pasajeros, IVA congelado y total bruto PayPal. No se cambian precios ni datos históricos.

Los correos nuevos siguen normalizados por el servicio y los códigos comerciales por la importación. Antes de trasladar datos se deben revisar duplicados previos que solo difieran en mayúsculas y la collation de origen; no se promete equivalencia de acentos. Los tokens de sesión y los identificadores PayPal conservan comparación exacta, sin ILIKE.

## Protección de pagos

No se modificó la máquina de estados ni el cliente PayPal. Se mantiene:

1. Transacción corta para proteger reserva/asientos.
2. Petición PayPal fuera de la transacción.
3. Nueva transacción para guardar el resultado.
4. Si la captura es incierta, protección durable y consulta/conciliación sin iniciar otro cobro.

Los conflictos/errores PostgreSQL no añaden políticas globales de reintentos ni vuelven a ejecutar capturas. Las regresiones en memoria incluyen captura concurrente única, pago pendiente protegido, timeout recuperado por GET, propietario y cancelación antes de cobrar. Esto no prueba todavía un commit incierto o bloqueos reales contra Supabase.

## Verificación ejecutada

- `dotnet build --no-restore`, salida aislada en `obj/verificacion-supabase`: correcto, sin errores ni advertencias de compilación de la API.
- **258 comprobaciones del modelo PostgreSQL**, sin abrir conexión: tipos, tablas, restricciones, JSON, IVA, versiones, generación de DDL en memoria y aislamiento del historial SQL Server.
- **67 comprobaciones de persistencia/fechas/pagos simulados**: SQL parametrizado con locks, propietario, versión, patrones literales, SQLSTATE, guardas de transacción, fechas UTC, rechazo de fecha modificada sin zona, historia no reinterpretada, login y huellas de vuelo, captura/conciliación simuladas.
- Regresiones reutilizadas: **21 descuentos, 14 CORS HTTP y 22 Swagger HTTP**. Total ejecutado en estas dos comprobaciones: **382**.
- No se ejecutó el Main completo de la suite SQL Server: contiene expectativas del historial/migraciones anterior. Se conserva para adaptación en etapa 5.
- El compilador Roslyn utilizado por los scripts muestra CS1702 al unificar las referencias EF Core 10.0.4 del proveedor con las 10.0.14 instaladas. La compilación normal de la API no muestra esa advertencia y las pruebas se ejecutaron correctamente. No se modificaron paquetes para ocultarla.
- Sin conexiones a SQL Server/Supabase, SQL ejecutado, creación de migraciones, cobros reales o modificaciones de datos. En las pruebas de SaveChanges se intercepta el guardado antes de llegar a la base; los locks solo se comprueban mediante SQL generado y guardas.

Para repetir desde la raíz:

```powershell
$script = Get-Content Tests/VerificarModeloPostgreSql.ps1 -Raw -Encoding UTF8
& ([scriptblock]::Create($script)) -ProjectRoot $PWD.Path -Comprobacion Modelo
& ([scriptblock]::Create($script)) -ProjectRoot $PWD.Path -Comprobacion Persistencia
```

El script no restaura paquetes. Compila las pruebas en una salida aislada y solo ejecuta los puntos de entrada elegidos.

## Pendiente para la etapa 4

- Preparar la migración inicial PostgreSQL en `Data/Migrations/PostgreSql`, con un solo snapshot activo.
- Incorporar secuencia, función y siete triggers que generan bytea de 8 bytes; revisar permisos, identidad y datos iniciales necesarios.
- Revisar el SQL y aplicarlo manualmente en una base de prueba. Antes, confirmar si la base destino está vacía y qué versión PostgreSQL usa el proyecto.
- Verificar manualmente schema e historial, y después realizar las pruebas reales de transacciones, concurrencia, importaciones y PayPal Sandbox de etapa 5.

No hay nuevas dependencias que instalar para los cambios de esta etapa: se utiliza Npgsql ya instalado por el equipo.

## Referencias

- [PostgreSQL: bloqueos explícitos y advisory locks](https://www.postgresql.org/docs/18/explicit-locking.html).
- [Npgsql: excepciones y SQLSTATE](https://www.npgsql.org/doc/diagnostics/exceptions_notices.html).
- [Npgsql: traducción ILIKE y funciones](https://www.npgsql.org/efcore/mapping/translations.html).
- [Npgsql: fechas UTC y precisión](https://www.npgsql.org/doc/types/datetime.html).
