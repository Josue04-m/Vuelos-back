# Primera integración de facturación del laboratorio: IVA y total PayPal

Esta etapa prepara los importes de la futura factura XML sin SRI. **Todavía no emite facturas, publica SOAP ni envía correos.** Se conserva el flujo PayPal actual con su validación de capturas y protección de asientos.

## Cálculo y persistencia

Para cada reserva nueva:

1. Calcular cada precio de pasajero con los descuentos vigentes: adulto/joven pagan 100%, niño 80%, bebé 75%.
2. Congelar esos precios, después del descuento y **antes de IVA**, en los detalles.
3. Sumar los detalles activos para obtener `subtotal`.
4. Calcular `montoIva = Round(subtotal × porcentajeIva / 100, 2, AwayFromZero)`.
5. Guardar `montoTotal = subtotal + montoIva`; ese es el importe que se confirma con totalEsperado y se cobra con PayPal.

El IVA se redondea una sola vez sobre el subtotal global, no boleto por boleto. Se usan decimal y límites compatibles con decimal(10,2). Se rechazan importes finales que excedan el esquema; no se truncan ni redondean hacia abajo para hacerlos caber.

Ejemplo del requerimiento (precios finales después de descuentos):

| Detalle | Cantidad | Precio unitario neto | Subtotal |
|---|---|---|---|
| Boleto niño | 1 | 150.00 | 150.00 |
| Boleto joven | 1 | 200.00 | 200.00 |
| Subtotal | | | 350.00 |
| IVA 15% | | | 52.50 |
| Total PayPal | | | 402.50 |

Si el precio base de un asiento de niño fuera 150.00, primero se aplica su descuento: 120.00 netos. Los 150.00 del ejemplo representan el precio ya descontado, no una excepción a las tarifas actuales.

## Configuración

`Reservas:PorcentajeIva` tiene valor inicial **15** y acepta de 0 a 100 con hasta dos decimales. Puede configurarse junto a las demás opciones sin reemplazarlas:

```json
{
  "Reservas": {
    "PorcentajeIva": 15
  }
}
```

En environmentVariables del perfil de launchSettings puede usarse `"Reservas__PorcentajeIva": "15"`. No se modificaron archivos de credenciales para introducir este valor. Una configuración inválida se rechaza, no habilita una tasa silenciosa distinta.

La tasa se congela al crear la reserva. Modificar la configuración solo afecta reservas nuevas. Al cambiar asientos, se recalculan subtotal e IVA con el porcentaje guardado en esa reserva; no se adopta el porcentaje actual. Una orden PayPal existente sigue impidiendo cambios de asientos/importes.

## Contratos para Flutter

El detalle y listado propios de reservas, la consulta administrativa de reservas de un vuelo y PagoPayPalResponse incorporan:

- `subtotal`: decimal nullable, suma de precios congelados después de descuentos y antes de IVA.
- `porcentajeIva`: decimal nullable, porcentaje congelado de esa reserva.
- `montoIva`: decimal nullable, IVA global redondeado a dos decimales.

Se conservan los campos existentes: `montoTotal` en reservas y `monto` en pagos contienen el **total final**. No renombrarlos a subtotal ni sumarles IVA de nuevo. En Dart, deserializar estos números como num y tratarlos como nullable; formatear dos decimales para mostrar USD.

```json
{
  "subtotal": 350.00,
  "porcentajeIva": 15.00,
  "montoIva": 52.50,
  "montoTotal": 402.50,
  "moneda": "USD"
}
```

Crear/cambiar una reserva sigue recibiendo `totalEsperado`, ahora con IVA cuando corresponda. No enviar subtotal, tasa ni importe de impuesto como datos de autoridad: el backend los calcula. Si hay discrepancia, responde 409 `tarifa_actualizada` con `montoTotalActual`; mostrar el total vigente y pedir reconfirmación.

La respuesta de reserva ya contiene el desglose antes de crear la orden PayPal. Mostrarlo en el resumen de compra y cobrar únicamente a través del backend. Las rutas de orden/captura no reciben otro importe del frontend.

`GET /api/admin/ventas/resumen` sigue siendo bruto de capturas confirmadas: los pagos nuevos incluyen el IVA cobrado. No representa subtotal neto ni separa impuestos en esta etapa.

## Reservas e importes anteriores

Las tres columnas nuevas son nullable, sin valor por defecto SQL ni UPDATE de datos antiguos. Los registros anteriores conservan su monto_total y quedan con subtotal/porcentaje_iva/monto_iva en NULL. Esto significa **sin desglose histórico**, no IVA cero.

No añadir IVA a compras anteriores, órdenes existentes ni reservas anteriores pendientes. Si una reserva antigua todavía admite cambios, conserva el cálculo anterior sin crear un desglose inventado. Sus pagos siguen usando el importe persistido. Una reserva nueva con tasa configurada 0 guarda explícitamente subtotal, porcentaje 0 e IVA 0; se distingue de los históricos NULL.

Las validaciones PayPal comparan la suma neta, el IVA congelado y el total. Una captura debe coincidir con el importe persistido de la reserva para acreditarse; si no coincide, queda para revisión sin reasignar ni liberar automáticamente asientos. Los reintentos no duplican cargos.

## Migración manual

`DesgloseIvaReservas` agrega:

- `reservas.subtotal decimal(10,2) NULL`.
- `reservas.porcentaje_iva decimal(5,2) NULL`.
- `reservas.monto_iva decimal(10,2) NULL`.
- CHECK para campos completos o todos NULL, importes no negativos, IVA calculado y total igual a subtotal más IVA.

Revisar y aplicar con la API detenida, antes de iniciar esta versión:

```powershell
dotnet ef database update DesgloseIvaReservas --project .\Vuelos_Backend.csproj
```

Después reiniciar la API y crear una reserva nueva para probar el desglose. El Down rechaza eliminar columnas si contienen importes, para proteger el historial. No se ejecutó la migración ni se modificó la base durante el desarrollo. No se agregaron dependencias.

## Verificación y siguiente etapa

Las pruebas locales usan calculadora de descuentos real, servicio de reservas, proveedor PayPal de prueba y repositorios en memoria. Cubren 350+52.50=402.50, redondeo, tasas, límites, snapshots, cambios de asientos, históricos, rollback, captura, ventas y manipulación de campos enviados por cliente. Se revisan snapshot y SQL generado sin ejecutarlo. No reemplazan la prueba de persistencia real en SQL Server ni un pago Sandbox de integración.

Pendiente para la siguiente etapa: factura persistida con número único, emisor y cliente congelados, detalle por boleto, XML, contrato SOAP protegido y envío/reintentos de correo al email registrado. El pago no se repetirá si falla esa futura emisión o envío.
