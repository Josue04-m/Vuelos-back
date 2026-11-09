# Pagos PayPal

**Importes actualizados:** las reservas nuevas guardan subtotal, porcentajeIva (inicialmente 15), montoIva y montoTotal. PayPal cobra el total incluido IVA después de descuentos; no se vuelve a sumar IVA en captura. Los históricos sin desglose conservan su monto. Aplicar manualmente DesgloseIvaReservas antes de esta versión. Ver [IVA_RESERVAS.md](IVA_RESERVAS.md); XML/SOAP/correo todavía no están implementados.

La API reemplaza la compra simulada por PayPal Orders v2: crea una orden, el comprador la aprueba y el backend solicita su captura. Solo una captura `COMPLETED`, con identidad, destinatario, moneda e importe verificados, acredita la reserva. No se reciben tarjetas ni importes del frontend.

## Configuración del servidor

Usar secretos de usuario, el gestor de secretos del despliegue o variables de entorno. No agregar el Client Secret a Git, Swagger, Flutter, archivos `.http` ni mensajes.

| Clave de configuración | Variable de entorno | Valor |
|---|---|---|
| `PayPal:Habilitado` | `PayPal__Habilitado` | `true` para activar; por defecto `false` |
| `PayPal:Entorno` | `PayPal__Entorno` | `Sandbox` inicialmente; `Live` solo con credenciales y cuenta reales |
| `PayPal:ClientId` | `PayPal__ClientId` | Client ID de la aplicación del entorno elegido |
| `PayPal:ClientSecret` | `PayPal__ClientSecret` | Secreto de esa aplicación, solo servidor |
| `PayPal:MerchantId` | `PayPal__MerchantId` | ID de la cuenta vendedora Business, no el Client ID |
| `PayPal:WebhookId` | `PayPal__WebhookId` | ID del webhook registrado en esa aplicación |
| `PayPal:ReturnUrl` | `PayPal__ReturnUrl` | URL del frontend que retoma la reserva tras aprobar |
| `PayPal:CancelUrl` | `PayPal__CancelUrl` | URL del frontend al cancelar el checkout |

Las URLs de retorno son HTTPS; Sandbox permite HTTP únicamente en localhost. Deben ser URLs reales de tu frontend. El frontend conserva el ID de reserva y retoma la sesión para llamar a captura: no se envía el token de sesión en la URL. Cambiar el entorno o el vendedor no convierte pagos históricos de Sandbox en Live.

En el panel PayPal, registrar `https://TU_API/api/pagos/paypal/webhook` en la misma aplicación. Suscribir `CHECKOUT.ORDER.APPROVED`, `PAYMENT.CAPTURE.COMPLETED`, `PAYMENT.CAPTURE.PENDING`, `PAYMENT.CAPTURE.DENIED`, `PAYMENT.CAPTURE.REFUNDED` y `PAYMENT.CAPTURE.REVERSED`. La URL debe ser HTTPS pública; para desarrollo puede usarse un túnel HTTPS que llegue al backend. Guardar su Webhook ID en configuración.

La verificación usa la API oficial `verify-webhook-signature`. Probar con eventos de pagos reales de **Sandbox**: el simulador genérico de webhooks de PayPal no admite esta modalidad de verificación por postback.

## Migración manual

Se preparó `PagosPayPal`, que agrega `dbo.pagos_paypal` y `dbo.eventos_paypal`, con importes decimales, RowVersion e índices únicos por reserva, orden y captura. No modifica ni convierte compras simuladas anteriores. Revisar y aplicar antes de iniciar esta versión:

```powershell
dotnet ef database update --project .\Vuelos_Backend.csproj
```

Este comando es para el equipo; no fue ejecutado automáticamente. No se necesitan paquetes adicionales: se utiliza HttpClient y System.Text.Json del framework.

## Flujo para frontend y Swagger

1. Iniciar sesión y crear una reserva mediante `POST /api/reservas`; guardar `reservaId` y `rowVersion`. Se mantienen descuentos y precios congelados.
2. Llamar `POST /api/reservas/{id}/paypal/orden` con `{"rowVersion":"VERSION_REAL"}`. No enviar monto. Abrir `urlAprobacion` en el navegador y aprobar con una cuenta **Personal Sandbox**, distinta de la vendedora Business Sandbox.
3. Al regresar, llamar `POST /api/reservas/{id}/paypal/captura` con la sesión del propietario, sin cuerpo. La orden se obtiene de la base; el frontend no puede sustituirla por otra.
4. Consultar `GET /api/reservas/{id}/paypal` y `GET /api/reservas/{id}`. Mostrar éxito solo con `estado=completado` y `estadoReserva=pagada`. Con `pendiente` o `revision`, mostrar estado en verificación; no repetir una compra nueva ni liberar el asiento desde el frontend.
5. Con admin, `GET /api/admin/ventas/resumen?entorno=Sandbox`. Devuelve `totalVentas`, `cantidadCompras`, `proveedor=PayPal`, `entorno` y `esSandbox`. `entorno=Live` consulta por separado dinero real. `desde` y `hasta` incluyen ambos días UTC de captura.

`POST /api/reservas/{id}/compra-simulada` fue retirado. El campo `compra` de la reserva se conserva exclusivamente para comprobantes simulados históricos; el pago nuevo se consulta en la ruta PayPal. No inferir pago de `compra=null` ni de una URL de retorno.

Crear y capturar son idempotentes. Se persisten IDs estables de solicitud derivados de PagoId y diferentes por operación. Una reserva admite un único pago PayPal. Una orden fallida no crea automáticamente otro cobro: cancelar/esperar vencimiento y crear una nueva reserva si corresponde. Tras crear una orden no se pueden cambiar asientos/importes; sí cancelar antes de iniciar el cobro.

## Protección y conciliación

Las transacciones cortas usan el mismo bloqueo por vuelo que las reservas. Ninguna llamada a PayPal mantiene una transacción SQL abierta. Antes de capturar se persiste una protección de la reserva; cancelar, vencer o cerrar operativamente el vuelo no libera asientos con una captura incierta o pendiente. El plazo general sigue en `Reservas:DuracionReservaMinutos`, inicialmente 720 minutos.

Un worker consulta pagos protegidos cada 60 segundos, sin iniciar cobros. El GET propio y `POST /api/admin/pagos/paypal/{pagoId}/conciliacion` también concilian. Los webhooks se verifican y deduplican por ID de evento. Si PayPal falla, el evento no se acredita y la entrega debe reintentarse.

Si no se puede demostrar el resultado, el pago permanece en `revision` y conserva sus asientos: requiere revisión del administrador en PayPal. No se cobra de nuevo automáticamente. Una captura llegada para una reserva ya cancelada o liberada se registra para revisión sin reasignar sus asientos. No hay un endpoint que permita afirmar manualmente que un pago incierto falló.

No se implementaron reembolsos automáticos. La cancelación administrativa de vuelos con capturas registradas o pagos protegidos devuelve 409 para evitar cancelar ventas sin resolver sus pagos. Notificaciones de reembolso/reversión conservan el bruto histórico y marcan revisión; el resumen no representa ventas netas ni liquida comisiones.

## Verificación

Pruebas locales de contratos HTTP del cliente, identidad/monto/vendedor, captura repetida, retenciones, timeout y recuperación, eventos repetidos y ventas separadas por entorno. Usan proveedor y repositorios de prueba; no ejecutan cobros ni conectan a SQL Server. Ejecutar el verificador descrito en `Tests/README.md`.

Pendiente de integración del equipo: aplicar migración; configurar claves y URLs; probar creación/aprobación/captura con cuentas Sandbox, entrega de webhook y persistencia en SQL Server, reinicio durante captura y concurrencia real. Después validar Live en un despliegue controlado.

Referencias oficiales: [Orders v2](https://developer.paypal.com/api/orders/v2/), [idempotencia](https://developer.paypal.com/api/rest/reference/idempotency/) y [webhooks](https://developer.paypal.com/api/rest/webhooks/rest/).
