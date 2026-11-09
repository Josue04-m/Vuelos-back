> Documento histórico de la migración temporal. La compra simulada fue retirada: usar [PAYPAL.md](../PAYPAL.md) y PayPal.http para el flujo vigente.

# Reservas temporales y compra simulada

## Alcance

No hay pasarela, cargo real, tarjeta ni reembolso. La compra de laboratorio se identifica como simulada en su tabla, comprobante y evento de historial. El total se obtiene de la reserva y se contrasta con sus detalles activos; no se acepta un importe enviado por el cliente.

## Estados y disponibilidad

| Reserva | Significado | Asientos |
|---|---|---|
| pendiente_pago | Retención hasta fecha_expiracion | reservado |
| pagada | Compra simulada registrada | reservado, sin expiración automática |
| expirada | Plazo vencido o cierre del vuelo | disponible, detalles liberados |
| cancelada | Cancelada antes de comprar o por cancelación administrativa del vuelo | disponible |
| confirmada / modificada | Datos anteriores al flujo temporal | Se conserva el comportamiento anterior; no implica pago |

`bloqueado` sigue siendo un bloqueo operativo del asiento; nunca representa una compra.

Cada reserva nueva guarda `FechaExpiracion` en UTC, calculada como el menor valor entre creación más duración configurada y salida programada. Cambiar asientos mantiene el vencimiento y el estado pendiente; conserva precios de detalles no modificados. Compras ya registradas no admiten cambios ni cancelación del cliente.

La fecha de expiración original sigue visible después de comprar como información histórica, pero solo expiran reservas `pendiente_pago`. `AnteriorAlFlujoTemporal=true` identifica registros antiguos con vencimiento NULL; no se les inventa un pago ni se liberan automáticamente.

## Configuración

En `appsettings.Development.json`:

```json
"Reservas": {
  "DuracionReservaMinutos": 720,
  "IntervaloLiberacionSegundos": 60,
  "HabilitarCompraSimulada": true
}
```

La duración admite minutos enteros positivos. Cambiarla afecta solo a reservas nuevas; el vencimiento guardado de las anteriores no cambia. El intervalo de liberación admite de 1 a 3600 segundos. La compra requiere ambas condiciones: entorno `Development` y bandera activada; por defecto la bandera es false. Activarla fuera de Development se rechaza al iniciar y el servicio también comprueba el entorno antes de comprar.

## Endpoints

- `GET /api/admin/ventas/resumen`: solo administrador; devuelve `totalVentasSimuladas`, `cantidadCompras`, moneda USD y marca simulada. Sin filtros acumula todo el historial; `desde` y `hasta` filtran días de compra UTC. Cada compra ya está guardada en `compras_simuladas`, por lo que no hay contador adicional que se pierda al reiniciar o se duplique por reintentos. Incluye compras de vuelos cancelados al no existir reembolsos; es un total bruto, no neto. Ejemplos en `VentasAdmin.http`.

- `POST /api/reservas`: crea una retención `pendiente_pago`, no una compra.
- `GET /api/reservas` y `GET /api/reservas/{id}`: incluyen vencimiento y marca de datos anteriores; el detalle incluye `compra` si existe.
- `POST /api/reservas/{id}/compra-simulada`: recibe únicamente RowVersion de la reserva. Requiere sesión y propiedad, retención vigente y vuelo abierto; devuelve 200 con estado `pagada` y comprobante simulado.
- Repetir una compra devuelve el mismo comprobante y estado actual sin duplicar cargos ni historial; se permite repetir con la versión original válida en formato. Si el vuelo fue cancelado administrativamente, la repetición no lo reactiva ni revierte la cancelación.
- `POST /api/reservas/{id}/cancelacion`: libera una reserva pendiente antes del cierre. Las compradas se rechazan; no se simulan reembolsos.
- Compras ajenas devuelven 404; expiradas o en conflicto, 409; simulación deshabilitada, 503.

## Liberación y concurrencia

El worker revisa lotes de hasta 100 vuelos cada intervalo. La liberación también se comprueba al listar disponibilidad del vuelo, crear una reserva, consultar las propias e intentar comprar, cambiar o cancelar. La API debe estar ejecutándose para el worker; al reiniciar recoge pendientes vencidas. Una consulta de asientos ejecuta la liberación del vuelo antes de devolver su disponibilidad.

Todas las operaciones bloquean primero el vuelo dentro de una transacción. La expiración libera detalles y asientos, cambia estado, actualiza RowVersion y añade historial sin borrar filas. No modifica reservas pagadas ni antiguas. Iniciar embarque, pasar a en vuelo o finalizar expira cualquier pendiente restante en la misma transacción administrativa. Cancelar el vuelo cancela las reservas vigentes y libera asientos; conserva los comprobantes simulados, sin registrar un reembolso ficticio.

La compra tiene una restricción única por reserva. Una carrera entre compra y expiración termina en una compra con asiento retenido o una expiración sin compra; nunca una compra con asiento liberado. Una operación que cruza el vencimiento antes de confirmar se revierte.

El cambio de avión/programación sigue bloqueado por compras registradas, reservas antiguas y retenciones pendientes vigentes. Una retención nueva expirada o cancelada sin compra ya no bloquea el cambio: conserva sus detalles históricos liberados. Los asientos anteriores no se borran.

## Migración manual pendiente

`20261101223315_ReservasTemporalesCompraSimulada` añade fecha_expiracion nullable, amplía los CHECK de estados, añade índice de vencimiento y crea compras_simuladas con importe, usuario, fecha, referencia y restricción única por reserva. No transforma registros previos en pagados ni expirados. El rollback está bloqueado si existen reservas temporales o compras, para evitar pérdida de datos.

Con la API detenida, revisar la conexión y ejecutar manualmente:

```powershell
dotnet build --no-restore
dotnet ef database update ReservasTemporalesCompraSimulada --no-build
```

Aplicar antes de iniciar esta versión. También se aplicarán migraciones anteriores que estén pendientes. No se requieren dependencias nuevas.

## Verificación

La suite ejecuta servicios reales y HTTP local con repositorios de prueba, reloj controlado y autenticación simulada. Incluye plazo de 12 horas, configuración, límite de salida, modificación sin extensión, cancelación, expiración, idempotencia, propiedad, entorno, carreras y rollback simulado. No demuestra el comportamiento de bloqueos ni rollback de SQL Server.

Pendiente con una base de laboratorio: aplicar la migración, reservar con tokens persistidos, configurar temporalmente un minuto, esperar el worker, comprobar reasignación y ejecutar compras/expiraciones simultáneas desde procesos separados. Verificar que solo exista un comprobante y que nunca quede un asiento comprado disponible. Para ese experimento usar datos de prueba; cambiar luego el plazo al valor deseado.
