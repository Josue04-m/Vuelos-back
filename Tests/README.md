# Comprobaciones de vuelos administrativos

`ImportacionAvionesChecks.cs.txt` verifica vista previa sin escrituras, CSV/UTF-8/comillas/BOM, límites, clases, matrículas duplicadas e inactivas, reimportación con mensaje, conflicto parcial, reversión del lote ante fallo en el segundo avión, revalidación tras alta manual, serialización con repositorio en memoria y permisos/multipart/Swagger por HTTP. Valida el CSV del proyecto sin importarlo en SQL Server. La concurrencia real entre procesos y la persistencia requieren prueba manual con SQL Server; no se instala ni migra nada al ejecutar estas comprobaciones.

`SwaggerFlujoChecks.cs.txt` usa la configuración real de Swagger, comprueba todos los pasos principales, el grupo de ventas, el esquema Bearer, ejemplos de clases/precios/compra, la guía y que Swagger UI se sirva. No arranca el worker ni conecta a SQL Server.

La suite también ejecuta `ReservasTemporalesChecks.cs.txt`: usa reloj controlado y transacciones en memoria para comprobar retenciones, compra simulada, expiración, cancelación, idempotencia, propiedad, restricciones de entorno, conservación del legado y carrera compra/expiración. Estos tests no sustituyen las pruebas de SQL Server ni de tokens persistidos. Instrucciones de migración y pruebas manuales en `Data/RESERVAS_TEMPORALES.md`.

Las pruebas de compra concurrente usan un bloqueo en memoria que imita la serialización por vuelo. Deben repetirse contra SQL Server desde procesos distintos para comprobar sus bloqueos e índices reales.

Se comprueba también distribución por clases que suma la capacidad, rechazo de clases duplicadas, tarifas completas, precio compartido por categoría y cambio de avión con distinta capacidad. El cambio rechaza reservas incluso canceladas y conserva/reutiliza los asientos anteriores. HTTP verifica permisos y el nuevo PATCH de avión. No se prueba concurrencia real con SQL Server.

La creación de aviones incluye pruebas del loop de generación exacta de asientos, letras configurables, última fila parcial, numeración consecutiva, categoría requerida, límite técnico de generación y rollback simulado ante un fallo parcial. No se ejecutan operaciones contra SQL Server.

El mismo comando ejecuta `CatalogosChecks.cs.txt`: permisos en los 8 endpoints de consulta de catálogos y rechazo del alta individual, edición y desactivación manuales de aviones, asientos físicos y categorías, creación, edición, versión antigua, desactivación, dependencias, capacidad, referencias inactivas, Swagger, traducción SQL y migración incremental de 12 columnas. Usa repositorios en memoria para los servicios; la concurrencia y persistencia reales siguen pendientes de SQL Server. Instrucciones: `Data/CATALOGOS_ADMIN.md`.

Las regresiones incluyen el rechazo de `embarcando → retrasado`, el cierre de reservas con horas reales, fechas futuras o improcedentes, finalización con llegada estrictamente posterior a la salida y conflictos de datos históricos sin sobrescritura. Se verifica el SQL generado por la consulta de historial para asegurar que no filtre al actor eliminado; la persistencia con SQL Server queda pendiente de prueba manual.

Desde la raíz del proyecto, en PowerShell:

```powershell
$script = Get-Content Tests/VerificarVuelosAdmin.ps1 -Raw -Encoding UTF8
& ([scriptblock]::Create($script)) -ProjectRoot $PWD.Path
```

Usa el SDK .NET 10 y los paquetes ya restaurados; no instala ni restaura dependencias. Compila en `obj/verificacion-vuelos` para evitar interferir con una API abierta.

Comprueba los endpoints administrativos con HTTP real local y autenticación simulada: 401 sin sesión, 403 para clientes, acceso admin, 201 con Location, 200, 400, 404 y 409:
- `GET /api/admin/vuelos` (listado con filtros y paginación)
- `GET /api/admin/vuelos/{id}` (detalle y versión para edición)
- `POST /api/admin/vuelos` (creación con asientos y precios USD)
- `PUT /api/admin/vuelos/{id}` (edición de programación)
- `POST /api/admin/vuelos/{id}/estado` (cambios de estado operativo con transiciones y cancelación transaccional)
- `GET /api/admin/vuelos/{id}/historial` (historial de estados operativos y auditoría)
- `GET /api/admin/vuelos/{id}/asientos` (listado de asientos, categoría, precios y estado)
- `PATCH /api/admin/vuelos/{id}/asientos/{asientoVueloId}` (actualización de precio o bloqueo con RowVersion)
- `GET /api/admin/vuelos/{id}/reservas` (consulta de reservas y pasajeros asignados)

Ejecuta el servicio real con un repositorio en memoria para validar UTC, precios, asientos, reservas vigentes, versión de programación, solapamientos, transiciones de estado, liberación transaccional de reservas en cancelación, concurrencia en asientos con RowVersion y confirmación de transacciones. También verifica Swagger y que el modelo EF coincida con el snapshot sin abrir una conexión SQL.

No sustituye las pruebas de autenticación con tokens persistidos ni de persistencia y concurrencia en SQL Server. Pendientes de ejecución manual con `VuelosAdmin.http` o Swagger:

1. Iniciar sesión como administrador y crear un vuelo con todos los asientos de un avión configurado. Verificar vuelo, asientos e historial inicial.
2. Intentar programar dos vuelos superpuestos simultáneamente para el mismo avión: solo uno debe completarse.
3. Editar simultáneamente el mismo vuelo con la misma versión: la segunda edición con datos anteriores debe devolver 409.
4. Competir una reserva y una edición del mismo vuelo: si la reserva confirma primero, la edición debe rechazarse; si la edición confirma primero, la reserva debe validar la nueva programación.
5. Transicionar el vuelo a través de sus estados operativos (`programado` -> `embarcando` -> `en vuelo` -> `finalizado`), verificando horarios reales y registro en el historial.
6. Cancelar un vuelo con reservas confirmadas y verificar que las reservas pasen a `cancelada`, los detalles a `liberado`, los asientos a `disponible` y se genere el historial de cancelación en una sola transacción.
7. Bloquear un asiento disponible o cambiar su precio con su `RowVersion` en Base64, comprobando que se rechacen cambios sobre asientos con reservas activas o con versiones desactualizadas.

No ejecutar escrituras de prueba sobre datos que deban conservarse. Esta etapa no requiere una migración ni dependencias nuevas.

## PayPal

El verificador incluye contratos HTTP del cliente PayPal con HttpMessageHandler de prueba y el servicio de pagos con repositorios/proveedor de prueba: identidad e importe, captura idempotente, PENDING, timeout y conciliación, cancelación y captura tardía, webhook firmado/duplicado, ventas separadas Sandbox/Live y descuentos. No realiza cobros ni abre conexiones SQL Server.

Para integración real del Sandbox y migración manual seguir PAYPAL.md. La cobertura local no demuestra persistencia, bloqueos distribuidos ni entrega real de webhooks. No usar el simulador genérico de eventos para verificar el postback de firmas.

## Importación de aeropuertos

ImportacionAeropuertosChecks.cs.txt cubre el CSV original con comas/UTF-8/BOM/comillas, filtros, omisiones explícitas, ciudades faltantes, duplicados e inactivos, paginación solo de vista previa, confirmación completa, reintentos, rollback, revalidación y serialización en memoria. Verifica permisos, multipart, Swagger y retiro del CRUD manual por HTTP. Lee airports.csv sin cargarlo en SQL Server. Comprueba además que la verificación de lectura detecte sustitución de caracteres. La persistencia real, codificación/collation y concurrencia entre procesos requieren pruebas manuales con SQL Server.
## Metadatos completos de aeropuertos

AeropuertosDatosCompletos añade 16 columnas nullable y cambia nombre/ciudad/pais a nvarchar, sin borrar datos ni alterar PK/FK/IATA. Las comprobaciones validan las 19 columnas del CSV, JSON de originales y columnas futuras, coordenadas/servicio/ID inválidos, Unicode, persistencia en memoria, GET administrativo, snapshot y SQL generado (no ejecutado). También se comprueba el bloqueo de Down ante posible pérdida de metadatos/Unicode. Aplicar la migración manualmente antes de iniciar esta versión; registros existentes no se completan por reimportación. No se conecta a SQL Server durante estas comprobaciones.
## Metadatos de aviones

AvionesDatosCompletos añade siete columnas nullable y cambia modelo a nvarchar. Las comprobaciones validan persistencia en memoria/GET de los nueve valores del CSV, JSON original, códigos compartidos por varias aeronaves, códigos vacíos, cantidades cero, letras y generación de asientos. Se revisan snapshot, SQL generado sin ejecutarlo, ausencia de claves únicas para IATA/ICAO de modelo y guardia del Down que protege datos. No se ejecuta la migración ni se conecta a SQL Server.
## Flutter web / CORS

CorsChecks.cs.txt arranca HTTP local usando la política y el middleware reales de Configuration/CorsFlutter.cs. Verifica OPTIONS sin login, Authorization/Content-Type, origen exacto permitido, puertos/hosts/orígenes denegados, GET autenticado, 401/403 legibles desde el frontend, configuración alternativa y ausencia de origen local por defecto en producción. No utiliza el navegador del usuario ni SQL Server.
## Primera integración IVA / PayPal

IvaReservasChecks.cs.txt comprueba subtotal/IVA/total, ejemplo 350+52.50=402.50 con descuentos reales, tasa configurable congelada, cambio de asientos, históricos sin desglose, redondeo, importes fuera de rango, rollback, captura y acumulado bruto, HTTP sin aceptar impuestos del cliente y migración incremental sin recalcular datos anteriores. Las regresiones previas de asientos y vencimiento usan tasa 0 explícita para aislar esas reglas; esta suite y DescuentosPasajerosChecks prueban 15% y las tarifas reales. No ejecutan cobros ni abren SQL Server. XML/SOAP/correo pendientes.