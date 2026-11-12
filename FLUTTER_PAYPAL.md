# Flutter: sustituir la compra simulada por PayPal

**Primera etapa de facturación del laboratorio:** actualizar el resumen con subtotal, porcentajeIva y montoIva nullable de la reserva; montoTotal ya es el precio final que cobra PayPal. TotalEsperado debe incluir IVA para reservas nuevas. No sumar impuesto otra vez ni aplicarlo retroactivamente a históricos NULL. Contrato y ejemplo: [IVA_RESERVAS.md](IVA_RESERVAS.md). La factura XML, SOAP y correo siguen pendientes.

Revisión: 7 de noviembre de 2026. Contrato contrastado con los Controllers, DTOs y servicios actuales de la API. El equipo backend informó que el flujo PayPal ya funciona en su entorno; esta revisión no ejecutó otro cobro ni probó Flutter.

## Alcance de la entrega

PayPal será el **único método de pago disponible para compras nuevas**. Retirar botones de compra simulada, selectores de otros medios de pago y cualquier confirmación local de compra. Conservar el historial existente y mostrar los comprobantes antiguos como simulados.

No llamar a `POST /api/reservas/{id}/compra-simulada`: esa ruta fue retirada. El campo `compra` de una reserva contiene únicamente un comprobante simulado histórico; `compra=null` no demuestra que una reserva esté sin pagar. Los pagos nuevos se consultan en las rutas PayPal.

El frontend consume nuestra API. No configura Client Secret, Merchant ID ni Webhook ID, no llama directamente a captura de PayPal y no decide el importe final.

## Endpoints que debe consumir Flutter

La URL base actual de desarrollo comunicada es `http://192.168.100.38:5236`; confirmar que siga siendo accesible desde el dispositivo. Las siguientes rutas ya incluyen `/api`.

Todas las operaciones propias requieren `Authorization: Bearer TOKEN` del usuario que creó la reserva, incluso cuando la cuenta es admin. No enviar el token en URLs, enlaces PayPal ni parámetros de retorno.

| Método y ruta | Cuerpo | Uso |
|---|---|---|
| `POST /api/reservas` | DTO actual de reserva | Crear la retención de asientos; guardar reservaId y rowVersion |
| `GET /api/reservas/{id}` | Sin cuerpo | Actualizar reserva, total, vencimiento, historial y versión |
| `POST /api/reservas/{id}/paypal/orden` | `{"rowVersion":"VERSION_ACTUAL"}` | Crear o recuperar la orden propia; devuelve PagoPayPalResponse |
| `POST /api/reservas/{id}/paypal/captura` | **Sin cuerpo** | Solicitar captura después de la aprobación; devuelve PagoPayPalResponse |
| `GET /api/reservas/{id}/paypal` | Sin cuerpo | Consultar el pago y conciliar capturas inciertas cuando corresponde |
| `POST /api/reservas/{id}/cancelacion` | `{"rowVersion":"VERSION_ACTUAL"}` | Cancelar una reserva cuando el backend todavía lo permite |
| `GET /api/admin/ventas/resumen?entorno=Sandbox` | Sin cuerpo | Resumen administrativo de capturas confirmadas por entorno |

Usar `rowVersion` Base64 de la respuesta JSON de reserva; nunca el hexadecimal de SQL ni la versión de un asiento. No enviar monto, moneda, vendedor, ordenId ni capturaId en las solicitudes de creación/captura: el servidor obtiene esos datos de su registro.

El webhook `POST /api/pagos/paypal/webhook` es una comunicación PayPal → backend, no una acción de Flutter. La conciliación administrativa `POST /api/admin/pagos/paypal/{pagoId}/conciliacion` es una herramienta admin; no usarla en el checkout de clientes ni esperar un cuerpo cuando responde 204.

## Flujo de pantallas y navegación

1. **Resumen de reserva:** mostrar el total USD devuelto por la API, pasajeros y vencimiento. Mantener los descuentos actuales: adulto/joven pagan 100%, niño 80% y bebé 75%; el importe definitivo siempre lo calcula el backend. Ofrecer un único botón **Pagar con PayPal**.
2. Al pulsarlo, bloquear pulsaciones repetidas y crear la orden con la versión vigente. Guardar `reservaId` y, cuando exista, `pagoId` antes de salir de la aplicación. No borrar este contexto si el proceso de Flutter se cierra.
3. Si llega `urlAprobacion` no vacía, abrir exactamente esa URL en el navegador. No construir otra URL ni capturar el pago con un SDK desde el dispositivo.
4. **Retorno de aprobación:** recuperar la reserva y la sesión del usuario, ejecutar captura sin cuerpo y evaluar su respuesta. Abrir la app o llegar a una pantalla llamada “éxito” no demuestra que se cobró.
5. **Resultado:** mostrar éxito únicamente con `estado=completado` y `estadoReserva=pagada`. Refrescar además `GET /api/reservas/{id}` para obtener su información vigente.
6. Con pago pendiente, en proceso o incierto, mostrar una pantalla de seguimiento y consultar el estado propio. No iniciar otra reserva o cobro automáticamente para resolver la incertidumbre.

Si el comprador vuelve manualmente del navegador sin una señal de aprobación fiable, mostrar **Continuar / verificar pago**. Esa acción puede solicitar captura: el backend verifica si la orden está aprobada antes de cobrar. El GET por sí solo no inicia capturas y el estado local `creada` no demuestra si el comprador aprobó en el navegador.

Si se cierra y abre la app después de enviar captura, consultar primero el pago existente. No repetir captura automáticamente cada vez que cambia el ciclo de vida de la app. Una acción explícita puede reintentar el mismo endpoint para la misma reserva; el servidor conserva su idempotencia.

## URLs de retorno: coordinación necesaria

Backend configura `PayPal:ReturnUrl` y `PayPal:CancelUrl` de forma global; el endpoint de orden no acepta URLs enviadas por Flutter. Las direcciones usadas temporalmente para Swagger deben reemplazarse por las acordadas con frontend.

Para móvil, acordar enlaces HTTPS de retorno/cancelación y su asociación con Android/iOS, o una página HTTPS intermedia que permita volver a la app. Para Flutter web, acordar sus rutas de retorno y el comportamiento al recargar la página. Son decisiones de despliegue pendientes; no existen pantallas Flutter ni rutas web de retorno implementadas por este cambio del backend.

La validación actual acepta URLs HTTPS y, en Sandbox, HTTP únicamente en localhost. **No acepta esquemas personalizados como `miapp://`.** En Sandbox también acepta HTTP con IPs privadas (10.x, 172.16-31.x, 192.168.x), útil para pruebas por LAN o ZeroTier. Acordar las URLs antes de implementar su navegación.

Conservar el contexto local de la reserva y restaurarlo al regresar. Los parámetros de la URL de retorno no autorizan compras ni sustituyen la identidad o la orden guardada por el backend. Si venció la sesión, pedir login y retomar la consulta con la misma cuenta.

## Respuesta del pago

Las tres rutas propias PayPal devuelven el mismo contrato. Ejemplo ilustrativo de una orden recién creada:

```json
{
  "pagoId": "00000000-0000-0000-0000-000000000001",
  "reservaId": 3,
  "estado": "creada",
  "estadoReserva": "pendiente_pago",
  "monto": 45.00,
  "moneda": "USD",
  "entorno": "Sandbox",
  "esSandbox": true,
  "ordenId": "ID_DE_LA_ORDEN",
  "capturaId": null,
  "urlAprobacion": "https://www.sandbox.paypal.com/checkoutnow?token=ID_DE_LA_ORDEN",
  "fechaCaptura": null,
  "requiereRevision": false,
  "rowVersionReserva": "AAAAAAAAJ9o="
}
```

Todos los IDs, enlaces, versiones e importes del ejemplo deben sustituirse por los de la respuesta real. `pagoId` es UUID; `reservaId` es entero; los IDs externos son strings. `ordenId`, `capturaId`, `urlAprobacion` y `fechaCaptura` admiten null. El monto llega como número JSON; no usar operaciones con punto flotante para determinar cuánto cobrar.

`estado` describe el pago; `estadoReserva` describe la reserva. Son conceptos distintos. Guardar `rowVersionReserva` para las operaciones posteriores que la necesiten. `requiereRevision=true` debe prevalecer en la presentación: no mostrar una venta normal solo porque exista capturaId o fechaCaptura.

| Estado de pago | Presentación y acción |
|---|---|
| `creando` | Preparando orden; esperar y consultar/reintentar creación de la misma reserva si corresponde. No abrir una URL inexistente |
| `creada` | Orden disponible para aprobación. Mostrar enlace solo si urlAprobacion existe; al volver, solicitar captura explícitamente |
| `verificando`, `capturando` | Procesando; bloquear nuevas acciones de cobro y consultar estado |
| `pendiente` | PayPal todavía no confirmó la captura; mantener seguimiento, sin mostrar compra exitosa |
| `completado` | Mostrar éxito cuando estadoReserva sea pagada y requiereRevision sea false |
| `fallido` | Informar que el pago falló. No crear otra orden automáticamente para esa reserva; releer la reserva y ofrecer las acciones permitidas |
| `revision` | Resultado que necesita revisión del administrador; conservar referencias y permitir consultar nuevamente |

Ante un valor nuevo/desconocido, mostrar estado pendiente de verificación y permitir actualizar. HTTP 200 no significa por sí solo que se pagó.

Como criterio de interfaz, consultar cada 5–10 segundos mientras la pantalla de seguimiento esté visible, con límite de intentos y botón **Actualizar**. Pausar consultas al cerrar la pantalla o perder sesión/conexión; no ejecutar consultas infinitas en segundo plano. Esta cadencia es una propuesta para Flutter, no una garantía de tiempo de confirmación de PayPal.

## Cancelación, vencimiento y errores

Cancelar el checkout en PayPal **no cancela automáticamente la reserva**. Al volver por CancelUrl, releer su estado y ofrecer **Volver a la reserva** o **Cancelar reserva**. La segunda opción llama explícitamente a la API con su versión actual. No solicitar captura en la ruta de cancelación.

Después de crear una orden no se permiten cambios de asientos/importes. Antes de iniciar el cobro todavía puede cancelarse la reserva si el backend lo admite. Una captura pendiente o incierta protege los asientos y bloquea cancelación/liberación hasta conciliación. No ponerlos como disponibles desde Flutter al llegar el contador a cero.

El plazo general de reserva es inicialmente 12 horas, configurable por backend y limitado por la salida del vuelo. La cuenta regresiva es informativa. Una pagada no expira y una expirada no debe reactivarse desde la app. No se implementaron reembolsos automáticos.

| Respuesta | Acción de Flutter |
|---|---|
| 400 | Mostrar validaciones; revisar RowVersion/cuerpo sin sustituirlos por valores inventados |
| 401 | Pedir login y conservar el contexto de reserva/pago para retomarlo con la misma cuenta |
| 403 | Ocultar o rechazar la acción administrativa para un cliente |
| 404 `pago_no_encontrado` | Puede faltar la orden o tratarse de una reserva ajena. Consultar primero la reserva propia; no marcarla pagada ni fallida por inferencia |
| 409 | Releer reserva/pago y mostrar el mensaje del backend; puede ser versión antigua, vencimiento o pago en curso |
| 503 `paypal_sin_configurar` | Informar indisponibilidad y reportarlo a backend; no pedir credenciales al usuario |
| 503 `paypal_no_disponible`, timeout o pérdida de conexión después de captura | Resultado incierto: consultar el pago existente antes de cualquier reintento; no afirmar que falló ni crear otro cobro |

Mostrar `detail` de ProblemDetails de forma controlada y conservar `codigo` para el manejo de errores. No registrar tokens ni datos de pasajeros. Mantener el total vigente de la reserva como autoridad, incluidos conflictos de tarifa anteriores al pago.

## Ventas e historial administrativo

Actualizar el DTO del resumen: `totalVentasSimuladas` fue sustituido por `totalVentas`; ahora se reciben `cantidadCompras`, `moneda`, `proveedor`, `entorno`, `esSandbox`, `desde` y `hasta`. Quitar la dependencia de `esSimulado`.

El filtro `entorno` acepta exactamente `Sandbox` o `Live`; el valor inicial es Sandbox. No sumar ambos entornos en un mismo indicador. Mostrar una etiqueta visible **Prueba Sandbox** cuando esSandbox sea true. El resumen suma capturas confirmadas, no reservas ni boletos; es bruto histórico, sin descontar reembolsos/comisiones. No incluye comprobantes simulados históricos.

## Lista de aceptación para el equipo Flutter

- Solo aparece PayPal como método de pago nuevo; la ruta simulada ya no se invoca.
- Compra completa desde reserva, navegador PayPal y captura confirmada; el total coincide con el backend y con ventas del mismo entorno.
- Doble pulsación no dispara acciones simultáneas ni duplica pantallas/compras.
- Volver por cancelación no acredita ni cancela automáticamente una reserva.
- Pago pendiente o pérdida de respuesta muestra seguimiento, sin una segunda compra automática.
- Cierre/reinicio de la app y sesión expirada permiten recuperar la misma reserva/pago.
- Reserva ajena, vencida y versión desactualizada muestran respuestas coherentes.
- Estados y campos nullable del pago se manejan sin errores de deserialización.
- Enlaces de retorno se prueban en las plataformas efectivamente entregadas; no dependen de que el dispositivo pueda abrir el localhost del servidor.
- Los históricos simulados siguen visibles como históricos, sin habilitar nuevas compras simuladas.

Contratos generales de vuelos, pasajeros y reservas: [API_FRONTEND.md](API_FRONTEND.md). Configuración exclusiva del servidor y ejemplos Swagger: [PAYPAL.md](PAYPAL.md) y [PayPal.http](PayPal.http). Este documento no cambia código Flutter, dependencias ni base de datos.
