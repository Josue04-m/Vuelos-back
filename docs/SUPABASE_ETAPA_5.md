# Supabase: verificación y pruebas (etapa 5)

Estado: **suite offline completa aprobada; integración contra Supabase y PayPal Sandbox real pendientes**. Rama `feature/SupaBase`, revisión 2026-11-11.

## Evidencia obtenida

Se adaptó toda la suite anterior a PostgreSQL, manteniendo comprobaciones de negocio, HTTP local, roles, propiedad, importación, precios, IVA y pagos. Las pruebas de estructura ahora inspeccionan el modelo/migración inicial PostgreSQL, no intentan aplicar las antiguas migraciones SQL Server. Se conservan sus propósitos: nulabilidad histórica, Unicode, restricciones, precios y protección frente a pérdida de datos.

Resultado offline: **924 comprobaciones aprobadas**, con compilación normal de la API sin errores ni advertencias:

| Bloque | Comprobaciones |
| --- | ---: |
| Catálogos y HTTP administrativo | 77 |
| CORS por HTTP local | 14 |
| Importación de aviones | 63 |
| Importación de aeropuertos | 72 |
| Reservas temporales e historial | 22 |
| Ventas | 16 |
| Swagger | 22 |
| PayPal con proveedor simulado | 45 |
| Descuentos | 21 |
| IVA y total PayPal | 39 |
| Vuelos administrativos | 124 |
| Modelo PostgreSQL | 259 |
| Migración y SQL preparado | 83 |
| Persistencia/fechas/pagos simulados | 67 |

Estas comprobaciones **no prueban una escritura o bloqueo real en Supabase**. Las conexiones ficticias de los tests offline permanecen cerradas; sus endpoints HTTP usan repositorios de prueba y el cliente PayPal usa respuestas simuladas. Persiste la advertencia CS1702 del compilador Roslyn de pruebas sobre referencias EF ya documentada; no se modificaron versiones para ocultarla.

Se revisó la skill `postman:api-testing`: conservar solicitudes/contratos y distinguir evidencia real de simulada. No hay colección local ni CLI Postman disponible, por lo que no se instaló nada; se reutilizaron las pruebas HTTP Kestrel y los contratos existentes.

## Qué cambió

- Los antiguos `*Checks.cs.txt` usan Npgsql y las restricciones/tipos de la instalación PostgreSQL actual. No se quitaron las regresiones funcionales para hacer pasar la suite.
- `Tests/SoportePostgreSql.cs.txt` centraliza la inspección de la migración inicial y las rutas de fixtures; las pruebas ya no dependen del directorio desde el que se lance PowerShell.
- `Tests/CompletaPostgreSql.cs.txt` ejecuta todos los bloques offline, sin repetir las regresiones auxiliares.
- `Tests/VerificarVuelosAdmin.ps1` conserva el comando anterior como alias de la suite completa PostgreSQL.
- `Tests/IntegracionPostgreSql.cs.txt` prepara un ensayo opt-in con repositorios/servicios reales contra una base de pruebas y proveedor de pago simulado. Se compiló; no se ejecutó contra una base.
- `Tests/VerificarModeloPostgreSql.ps1` añade `Completa`, `Integracion` y `-SoloCompilar`. No instala/restaura paquetes.

No se cambiaron endpoints, reglas de negocio, migraciones aplicadas ni datos del equipo en esta etapa. Se conserva por ahora el paquete SQL Server declarado: no se modifican dependencias automáticamente. Su posible retirada se puede revisar aparte; las fuentes de pruebas activas ya no requieren UseSqlServer.

## Repetir las pruebas offline

Desde la raíz:

```powershell
$script = Get-Content Tests/VerificarModeloPostgreSql.ps1 -Raw -Encoding UTF8
& ([scriptblock]::Create($script)) -ProjectRoot $PWD.Path -Comprobacion Completa
```

El comando anterior sigue disponible mediante `Tests/VerificarVuelosAdmin.ps1`. Ejecuta pruebas HTTP locales y simulaciones, sin conectarse a bases ni realizar pagos.

## Ensayo con Supabase real preparado

### Requisitos

1. Aplicar manualmente `InicialPostgreSql` y configurar los permisos de etapa 4. Este ensayo **no** ejecuta Migrate, EnsureCreated, scripts de instalación ni DDL.
2. Usar una base exclusiva de pruebas y un LOGIN runtime restringido. Debe poder consultar/insertar/actualizar y generar versiones, pero no crear objetos, borrar/truncar ni modificar el historial EF. No usar al propietario de migraciones.
3. Detener otros consumidores de esa base mientras se prueba. El ensayo utiliza un reloj controlado que avanza horas para comprobar expiración; solo actúa sobre los vuelos que él crea.
4. Configurar localmente `PNA_TEST_CONNECTION` con la conexión runtime de pruebas y TLS VerifyFull. Es independiente de la conexión normal de la API; no se copian credenciales a archivos del repositorio ni a informes.
5. Establecer `PNA_TESTS_CONFIRM=BASE_DE_PRUEBAS` para confirmar el destino. La confirmación es una barrera operativa, no convierte una base de producción en una base segura para pruebas.

No se encontró SupabaseConnection en los archivos/profiles/variables accesibles durante esta entrega. No se pudo consultar el almacén de secretos por permisos del entorno; por eso no se concluye que ese almacén esté vacío. Tampoco se confirmó todavía la aplicación real de la migración.

### Qué hace el ensayo

Primero realiza consultas de metadatos y permisos. Se detiene antes de crear fixtures si no están las 15 tablas, los siete triggers habilitados o los permisos runtime correctos. No necesita SELECT sobre __EFMigrationsHistory: la comprobación de su contenido corresponde al propietario y al script manual de etapa 4.

Después crea datos nuevos, identificados por `LAB-<ejecución>` o por ese identificador en sus nombres:

- Dos aeropuertos con códigos libres, un avión de tres asientos y distribución por clase; vuelve a importar ambos CSV para comprobar que no duplican registros.
- Una cuenta administrativa de laboratorio y dos clientes, con contraseñas aleatorias en memoria y hashes persistidos; sus credenciales no se imprimen ni se guardan en el informe.
- Vuelos propios para probar generación de asientos, versiones y solapamiento simultáneo.
- Reservas/pagos/eventos propios para probar disponibilidad y persistencia.

Comprueba con conexiones independientes:

- Imports reales, conservación Unicode, búsquedas sin distinguir mayúsculas y comodines literales.
- Login, persistencia del token, rol cliente, formato exacto y revocación.
- Retorno de RowVersion después de UPDATE y rechazo de una versión obsoleta.
- Dos altas simultáneas del mismo avión en el mismo intervalo: solo una debe prosperar.
- Dos clientes reservando el mismo asiento: una sola reserva activa, IVA/total guardados y propietario protegido.
- Restricción única de asignación activa, mediante un intento duplicado dentro de una transacción revertida.
- Cancelación y expiración de una reserva propia, sin liberar pagos protegidos.
- Captura concurrente idempotente y conciliación sin recobro, **con el proveedor externo simulado**.
- Persistencia idempotente de un evento de prueba; esto no verifica la firma real de un webhook PayPal.

**Los registros de laboratorio se conservan.** No se borran físicamente ni se limpian datos anteriores. Se crean estados de pago simulados en el ledger Sandbox de esa base: no utilizar el ensayo en producción ni mezclar esas ventas con dinero real. Si falla a mitad, revisar el identificador de ejecución antes de repetir. Los usuarios tienen correos example.invalid y no se envía correo.

### Ejecución manual

Una vez configurada localmente `PNA_TEST_CONNECTION` con la conexión real de pruebas:

```powershell
$env:PNA_TESTS_CONFIRM = 'BASE_DE_PRUEBAS'
$script = Get-Content Tests/VerificarModeloPostgreSql.ps1 -Raw -Encoding UTF8
& ([scriptblock]::Create($script)) -ProjectRoot $PWD.Path -Comprobacion Integracion
```

Para comprobar solo la compilación, sin abrir conexión:

```powershell
& ([scriptblock]::Create($script)) -ProjectRoot $PWD.Path -Comprobacion Integracion -SoloCompilar
```

El resultado queda en `obj/verificacion-supabase/IntegracionPostgreSql.resultado.json`, con estado, contador, identificador y error acotado, sin passwords, tokens ni conexión. La ejecución devuelve 0 al aprobar, 1 al fallar y 2 si falta conexión/confirmación. Un estado NO_EJECUTADA **no es una prueba aprobada**.

Se compiló este ensayo y se comprobó su barrera sin variables: devolvió NO_EJECUTADA, contador 0 y baseReal=false. No se han probado todavía en un servidor sus escrituras, triggers ni carreras.

## Validación HTTP y PayPal Sandbox reales: pendiente

Después de las pruebas de base, levantar la API con el LOGIN runtime y comprobar en Swagger usando cuentas de prueba propias:

| Caso | Resultado requerido |
| --- | --- |
| Login y logout | Token persistido válido; el token revocado ya no permite acceder. |
| Sin token / cliente en rutas admin | 401 / 403. Las consultas admin funcionan con su rol. |
| Catálogos | GET e importación autorizada; altas/ediciones/desactivaciones retiradas siguen sin estar disponibles. |
| Imports repetidos | Mensaje de ya importados; sin duplicados ni cambios de registros existentes. |
| Reserva y propiedad | Una sola asignación activa por asiento; una cuenta no consulta ni modifica reservas ajenas. |
| Cambio/cancelación | Exige versión vigente y libera solo los asientos correspondientes; el historial permanece. |
| Expiración | Libera una reserva sin pago al vencer su plazo; nunca un pago protegido o pagado. Para una prueba rápida puede configurarse manualmente un plazo corto antes de crear reservas nuevas. |
| PayPal Sandbox real | Crear orden → abrir UrlAprobacion con comprador Sandbox → aprobar → capturar → consultar pago. Solo COMPLETED marca pagada. |
| Reintentos / ventas | Una captura y un incremento por el total USD; repetir la consulta/captura no duplica ventas. Comparar el acumulado antes/después, no con cero si ya hay otras pruebas. |
| Webhook real | Firma verificada; un evento repetido no genera otra venta. El ensayo con proveedor simulado no cubre esta integración. |

La autorización HTTP completa con la base real, la aceptación del SQL por PostgreSQL y el flujo contra servidores PayPal Sandbox deben quedar registrados por el equipo. No se realizaron cobros ni llamadas al proveedor real en esta entrega.

## Criterio de cierre

La etapa 5 no se considera cerrada hasta tener el ensayo contra Supabase aprobado y evidencia del flujo HTTP/PayPal Sandbox real. No iniciar el traslado/corte de etapa 6 solo con la suite offline.

No hay dependencias adicionales que instalar para las herramientas entregadas.
