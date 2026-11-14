# Pacific Nexus Airways — Contrato de API para frontend

**Importación de aeropuertos con existentes:** los IATA ya registrados se omiten automáticamente, incluso si están inactivos. Una carga mixta responde `valido` al validar e `importado` al confirmar los nuevos; todos existentes responde `ya_importados`. `totalImportables` cuenta nuevos candidatos; `totalExistentes` se incluye en `totalOmitidos`, con motivo `iata_ya_registrado`, sin sumarse a errores. Las filas paginadas incluyen los candidatos nuevos y existentes para revisión. No se requiere retirar filas del CSV. Aviones conserva su comportamiento actual. Requiere desplegar esta versión; no requiere migración.

**Ciudad de aeropuerto opcional:** `ciudad` puede ser `null` en aeropuertos, catálogos y `origen`/`destino` de vuelos. Flutter debe usar el nombre del aeropuerto como alternativa. Requiere aplicar manualmente la migración PostgreSQL `CiudadAeropuertoOpcional` antes de importar registros sin ciudad; no cambian IATA, nombre ni país obligatorios.

**Búsqueda pública de aeropuertos:** `GET /api/aeropuertos?buscar=Ecuador&pagina=1&tamanoPagina=20`, sin token. Busca por nombre de país en español (también parcial, sin tildes/mayúsculas) o ISO exacto como EC. Responde `elementos`, `pagina`, `tamanoPagina`, `total`, `totalPaginas`; cada elemento contiene `aeropuertoId`, `codigoIata`, `nombre`, `ciudad`, `pais`. Flutter conserva el IATA para consultar vuelos y usa `pais` para las banderas. Contrato y validaciones: [REQUERIMIENTO_BUSQUEDA_PUBLICA_AEROPUERTOS.md](docs/REQUERIMIENTO_BUSQUEDA_PUBLICA_AEROPUERTOS.md). Pendiente de despliegue y prueba con datos reales; no requiere migración.

**Importes con IVA (laboratorio):** [IVA_RESERVAS.md](IVA_RESERVAS.md) describe subtotal, porcentajeIva y montoIva nullable; montoTotal y el monto PayPal son el total final. TotalEsperado de reservas nuevas debe incluir IVA. XML/SOAP/correo todavía no se implementan.

**Requerimiento para adaptar widgets Flutter:** [FLUTTER_IMPORTACIONES_CATALOGOS.md](FLUTTER_IMPORTACIONES_CATALOGOS.md) reúne pantallas, formularios multipart, catálogos de solo lectura, metadatos nuevos, estados y criterios de aceptación.

**Carga de aeropuertos (admin):** [IMPORTACION_AEROPUERTOS.md](IMPORTACION_AEROPUERTOS.md) reemplaza el CRUD manual por validación/importación del CSV original, con filtros y resumen de omisiones. Solo se conservan GET de listado y detalle.

**Carga de flota (admin):** [IMPORTACION_AVIONES.md](IMPORTACION_AVIONES.md) describe la pantalla Configuración → Cargar aviones, vista previa y confirmación CSV. Una carga repetida devuelve `ya_importados` y no duplica aviones ni asientos.

**Cambio requerido en Flutter:** PayPal es el único método para compras nuevas. La guía de adaptación, navegación de retorno, estados, errores y pruebas está en [FLUTTER_PAYPAL.md](FLUTTER_PAYPAL.md). Los comprobantes simulados se conservan exclusivamente como histórico.

Fecha de revisión: **7 de noviembre de 2026**. Documento contrastado con Controllers, DTOs, servicios y Swagger generado del backend actual: **40 endpoints y 55 contratos**. Describe funciones existentes; las limitaciones de integración se indican expresamente. Esta entrega usa PayPal; Sandbox identifica dinero de prueba y Live procesa cobros reales. Ver [PAYPAL.md](PAYPAL.md).

## 1. Conexión y convenciones

| Entorno | Dirección de ejemplo |
|---|---|
| API del servidor | `http://192.168.100.38:5236` |

Las rutas de este documento incluyen `/api`; no añadir ese prefijo dos veces. `0.0.0.0` es la dirección de escucha del servidor, no una URL para el cliente. En otro dispositivo, `localhost` apunta a ese dispositivo, no al equipo del backend. La accesibilidad de red y el puerto deben acordarse con backend.

- JSON usa **camelCase**. Enviar `Content-Type: application/json` cuando exista cuerpo.
- Fechas de calendario: `YYYY-MM-DD`. Horarios: ISO 8601 con zona explícita, por ejemplo `2026-11-15T10:00:00Z` o `2026-11-15T05:00:00-05:00`. Los horarios se normalizan a UTC; las búsquedas por día también son UTC.
- Moneda única: **USD**. Los importes son números JSON, no textos como `"$150"`. El backend utiliza decimal y valida hasta dos decimales en precios.
- Los IDs son enteros positivos. Las referencias, IDs, precios y versiones de los ejemplos son ilustrativos: sustituirlos por respuestas reales.
- Una respuesta `204` no contiene JSON; no intentar deserializarla.
- No se implementan borrados físicos. Los catálogos se desactivan; las reservas conservan su historial.

## 2. Autenticación y permisos

Registro, login, `GET /api/aeropuertos`, `GET /api/vuelos`, `GET /api/vuelos/{id}` y `GET /api/vuelos/{id}/asientos` son públicos. Se puede explorar sin enviar Authorization. El webhook PayPal no usa sesión, pero exige verificar la firma del proveedor. Los demás endpoints requieren sesión con rol `cliente` o `admin`; `/api/admin/*` exige `admin`.

```http
Authorization: Bearer TOKEN_RECIBIDO_DEL_LOGIN
```

El token es **opaco, no JWT**. No se puede decodificar para obtener usuario o rol. Se valida en el servidor junto con su expiración y el usuario persistido. No enviar `usuarioId` ni rol para identificar al titular de una reserva: el backend los obtiene de la sesión. Admin puede usar las operaciones normales para sus propias reservas; no obtiene acceso a reservas ajenas en `/api/reservas`.

### Registro

`POST /api/usuarios/registro` — público, devuelve `201` con `UsuarioResponse`. No inicia sesión automáticamente.

```json
{
  "nombre": "Ana",
  "apellido": "Torres",
  "email": "ana@ejemplo.test",
  "password": "CAMBIAR_CLAVE_DE_PRUEBA"
}
```

La contraseña de registro debe tener de 8 a 128 caracteres. El servidor siempre asigna rol `cliente`. Un correo duplicado devuelve `409`.

### Login

`POST /api/autenticacion/login` — público, devuelve `200` con `LoginResponse` o `401` si las credenciales no son válidas.

```json
{"email":"ana@ejemplo.test","password":"CAMBIAR_CLAVE_DE_PRUEBA"}
```

Ejemplo de respuesta:

```json
{
  "token": "TOKEN_OPACO_DEVUELTO_POR_EL_SERVIDOR",
  "tipoToken": "Bearer",
  "fechaExpiracion": "2026-11-01T13:00:00Z",
  "usuario": {
    "usuarioId": 7,
    "nombre": "Ana",
    "apellido": "Torres",
    "email": "ana@ejemplo.test"
  }
}
```

**Limitación actual:** `usuario` no incluye `rol` y no existe `/me` ni un endpoint de perfil. Acordar con backend la entrega del rol antes de implementar la navegación administrativa automática. No inferir permisos a partir del correo o del token. La autorización efectiva permanece en el servidor.

### Logout

`POST /api/autenticacion/logout` — autenticado, sin cuerpo, devuelve `204`. Revoca la sesión actual. Después, eliminar el token local. No existe refresh token ni renovación automática; ante `401` solicitar un nuevo login.

## 3. Versiones y actualizaciones concurrentes

| Campo | Procedencia | Uso |
|---|---|---|
| `rowVersion` | Catálogo, asiento o reserva consultado | Editar/desactivar catálogo, operar asiento, comprar o cancelar reserva |
| `rowVersionAsiento` | Disponibilidad actual del asiento | Crear reserva o elegir asiento de destino |
| `version` | Detalle administrativo de vuelo | Editar programación, cambiar avión o estado |

`rowVersion` y `rowVersionAsiento` son Base64 de 8 bytes. `version` del vuelo es un hexadecimal de 64 caracteres; no es una columna SQL rowversion. Tratar ambos formatos como valores opacos, sin fabricarlos ni incrementarlos. Cada versión corresponde a su entidad: no intercambiar la de una reserva con la de su asiento.

Ante `409` por versión desactualizada: consultar nuevamente, actualizar la pantalla y pedir confirmación antes de reenviar. Una operación correcta puede devolver una versión nueva; sustituir la anterior. La repetición de una compra ya registrada es idempotente y admite la versión original con formato válido.

## 4. Flujo del cliente

### 4.1 Buscar vuelos y disponibilidad

1. Explorar sin iniciar sesión. Solicitar login o registro al continuar con una reserva; después conservar la selección en Flutter y consultar otra vez la disponibilidad/versiones antes de reservar.
2. `GET /api/vuelos?origen=GYE&destino=UIO&fecha=2026-11-15`.
3. Consultar detalle con `GET /api/vuelos/{id}`.
4. Consultar `GET /api/vuelos/{id}/asientos` para dibujar el mapa y obtener los IDs y versiones actuales.

Filtros de vuelos: `origen`, `destino`, `fecha` **o** rango completo `fechaDesde`/`fechaHasta`; `pagina` desde 1 y `tamanoPagina` de 1 a 100 (20 por defecto). No combinar día y rango. Los códigos IATA tienen tres letras y no distinguen mayúsculas. El listado puede incluir vuelos que no admitan reservas: usar `permiteReservas`.

La respuesta paginada usa `vuelos`, `pagina`, `tamanoPagina`, `total` y `totalPaginas`. No usa `elementos`.

Ejemplo de disponibilidad:

```json
{
  "vueloId": 10,
  "numeroVuelo": "PN100",
  "moneda": "USD",
  "permiteReservas": true,
  "asientos": [{
    "asientoVueloId": 101,
    "codigoAsiento": "1A",
    "categoria": {"categoriaId": 1, "nombre": "Primera Clase", "descripcion": null},
    "precio": 150,
    "moneda": "USD",
    "estado": "disponible",
    "disponible": true,
    "rowVersion": "AAAAAAAAAAA="
  }]
}
```

Usar `disponible` para habilitar selección; también considera el estado y horario del vuelo. No confundir `asientoVueloId` con `asientoFisicoId`. El precio mostrado es base de clase; el precio final depende también del tipo de pasajero.

### 4.2 Pasajeros y consulta de cédula

`GET /api/pasajeros/consulta-cedula/{cedula}` requiere sesión y una cédula de exactamente 10 dígitos. Devuelve nombres sugeridos y `requiereRevision=true`. El frontend debe permitir revisar y corregir los cuatro campos antes de reservar. Si no hay resultado o el proveedor falla, permitir ingreso manual; el error puede incluir `permiteIngresoManual=true`.

La consulta no crea pasajeros ni reservas. No es obligatorio consultar el proveedor para ingresar un pasajero manualmente. Los datos se guardan en los detalles de la reserva.

Tipos admitidos, elegidos por el usuario: `adulto`, `joven`, `nino`, `bebe`. No se solicita fecha de nacimiento ni se comprueba edad. Todos tienen asiento propio, incluidos bebés. El titular de la cuenta no está obligado a viajar.

Descuentos configurados: `adulto` y `joven` pagan el 100 % del asiento elegido; `nino`, el 80 % (20 % de descuento); `bebe`, el 75 % (25 % de descuento). Por ejemplo, para un asiento de USD 100, el importe es USD 100, 100, 80 o 75 respectivamente. Se aplican a la clase elegida, con redondeo a dos decimales; el servidor conserva la autoridad sobre el total y el importe congelado. Usar los valores actuales de configuración si backend los modifica.

### 4.3 Crear reserva temporal

`POST /api/reservas` — propietario identificado por sesión, devuelve `201` con `ReservaResponse` y Location al detalle.

```json
{
  "vueloId": 10,
  "totalEsperado": 150,
  "pasajeros": [{
    "cedula": "DEMO-001",
    "primerNombre": "Ana",
    "segundoNombre": null,
    "primerApellido": "Torres",
    "segundoApellido": null,
    "tipoPasajero": "adulto",
    "asientoVueloId": 101,
    "rowVersionAsiento": "AAAAAAAAAAA="
  }]
}
```

Primer nombre y primer apellido son obligatorios; los segundos son opcionales. Los cuatro admiten hasta 100 caracteres. En la reserva, `cedula` admite hasta 50 caracteres; la regla de 10 dígitos pertenece al endpoint externo de consulta. No repetir documento ni asiento dentro de la misma reserva. Todos los asientos deben pertenecer al vuelo.

El backend calcula el total, aplica la configuración por tipo de pasajero y conserva `precioCongelado` de cada detalle. Si `totalEsperado` difiere, devuelve `409` con `codigo=tarifa_actualizada`, `montoTotalActual` y `moneda`. Mostrar el nuevo total y solicitar confirmación; no confirmar automáticamente un importe cambiado. Una falta de configuración tarifaria devuelve `503`.

La respuesta incluye `reservaId`, `codigoReserva`, `estado=pendiente_pago`, `fechaExpiracion`, `rowVersion`, `detalles`, `historial` y `compra=null`. Guardar IDs y versiones de la respuesta; no inventarlos a partir de los ejemplos.

### 4.4 Consultar historial y modificar asientos

- `GET /api/reservas`: reservas propias paginadas. Filtros `estado`, rango completo `fechaDesde`/`fechaHasta` por creación UTC, `pagina` y `tamanoPagina`.
- `GET /api/reservas/{id}`: detalle propio, historial y comprobante si existe. Reserva ajena devuelve `404`, incluso con rol admin.
- `PATCH /api/reservas/{id}/asientos`: cambios de detalles activos, conservando los pasajeros.

```json
{
  "rowVersion": "AAAAAAAAAAA=",
  "totalEsperado": 150,
  "cambios": [{"detalleId": 20, "asientoVueloId": 102, "rowVersionAsiento": "AAAAAAAAAAA="}]
}
```

Los detalles omitidos conservan asiento y precio congelado. Los cambiados usan la tarifa vigente y requieren confirmar el nuevo total. El plazo de compra no se reinicia. Una compra registrada no admite cambios de asientos del cliente. No modificar después del cierre del vuelo. El listado usa `reservas`, no `vuelos` ni `elementos`.

El detalle devuelve también filas históricas con `estado=liberado`. Para mostrar la asignación vigente, filtrar `detalles` por `estado=activo`; mantener los liberados en la vista de historial. Usar montoTotal del backend, que incluye IVA para reservas nuevas. PrecioCongelado del detalle es neto después de descuentos: sumar solo detalles activos da el subtotal, no el total con IVA. Incluir detalles liberados además duplicaría importes después de un cambio de asiento.

### 4.5 Pagar con PayPal

Crear `POST /api/reservas/{id}/paypal/orden` con `{"rowVersion":"VERSION_REAL"}` y la sesión del propietario. Abrir `urlAprobacion` y autorizar con el comprador PayPal del entorno correspondiente. Al regresar, llamar `POST /api/reservas/{id}/paypal/captura` sin cuerpo; el servidor utiliza la orden e importe de la reserva, sin aceptar sustituciones desde el frontend.

La respuesta `PagoPayPalResponse` incluye `pagoId`, `reservaId`, `estado`, `estadoReserva`, `monto`, `moneda`, `entorno`, `esSandbox`, `ordenId`, `capturaId`, `urlAprobacion`, `fechaCaptura`, `requiereRevision` y `rowVersionReserva`. Mostrar éxito solo si `estado=completado` y `estadoReserva=pagada`. La aprobación o URL de retorno no prueban el cobro.

Consultar `GET /api/reservas/{id}/paypal` si está pendiente o la respuesta fue incierta. No crear otro cobro ni liberar asientos desde la interfaz. Los reintentos no duplican capturas. Tras crear una orden no se permite cambiar el importe/asientos; se puede cancelar antes de iniciar el cobro. Una captura en curso o incierta protege la reserva incluso después de su vencimiento hasta conciliarla.

La ruta `compra-simulada` fue retirada. `compra` en el detalle de reserva permanece únicamente para históricos simulados; consultar el pago nuevo por la ruta PayPal. Configuración y flujo completo: [PAYPAL.md](PAYPAL.md).
### 4.6 Cancelar o esperar vencimiento

`POST /api/reservas/{id}/cancelacion`, con `{"rowVersion":"..."}`, devuelve el detalle actualizado. Permite cancelar una pendiente antes del cierre del vuelo; libera sus asientos. Una compra no admite cancelación del cliente ni reembolso en esta entrega.

| Estado de reserva | Qué mostrar | Disponibilidad |
|---|---|---|
| `pendiente_pago` | Retenida, con cuenta regresiva | Asientos reservados |
| `pagada` | Captura PayPal confirmada o comprobante histórico | Asientos reservados, no expiran |
| `expirada` | Plazo vencido; crear otra reserva | Detalles liberados |
| `cancelada` | Cancelación e historial | Asignaciones liberadas |
| `confirmada`, `modificada` | Registro anterior al flujo temporal | No inferir que esté pagado |

El plazo inicial es 12 horas, configurable por backend. El vencimiento no supera la salida programada y cambiar asientos no lo extiende. El contador del frontend es informativo: al agotarse, consultar nuevamente el estado. La API libera vencidas periódicamente y al consultar disponibilidad o intentar operar; iniciar embarque expira las pendientes del vuelo. No esperar que el navegador libere el asiento por sí solo. No existe notificación push/SignalR de estos cambios; refrescar la disponibilidad cuando sea necesario.

`anteriorAlFlujoTemporal=true` y `fechaExpiracion=null` identifican datos antiguos. No fabricar una expiración ni un comprobante. En una compra, la fecha de expiración original permanece como dato histórico; no tratar una reserva `pagada` como expirada por esa fecha.

## 5. Flujo administrativo

Todos los catálogos y operaciones administrativas exigen `admin`. La consulta normal de vuelos y reservas propias también puede usarse con esa cuenta para las pruebas.

### 5.1 Catálogos

Aviones ofrece únicamente listado, detalle e importación CSV. Asientos físicos ofrece solo listado y detalle; sus registros se generan con la importación del avión. Aeropuertos conserva listado, detalle e importación CSV; categorías conserva únicamente listado y detalle del catálogo existente (Primera, Segunda y Tercera Clase):

| Recurso | Ruta base |
|---|---|
| Aeropuertos | `/api/admin/aeropuertos` |
| Categorías/clases | `/api/admin/categorias-asiento` |
| Aviones | `/api/admin/aviones` |
| Asientos físicos | `/api/admin/asientos-fisicos` |

Los listados admiten `buscar` (hasta 100 caracteres), `activo` (true por defecto), `incluirInactivos`, `pagina` y `tamanoPagina` (1 a 100, por defecto 20). `incluirInactivos=true` incluye ambos estados; `activo=false` consulta solo inactivos. Solo asientos físicos admite `avionId`. Devuelven `elementos`, `pagina`, `tamanoPagina`, `total` y `totalPaginas`. El detalle puede consultar un inactivo.

Las importaciones de aviones y aeropuertos devuelven 200 con su resultado. Los catálogos no ofrecen creación, edición ni desactivación manuales. Las categorías existentes se usan por ID y no se generan desde Flutter ni durante la importación de aviones.

Los aeropuertos se cargan desde airports.csv; ya no existen POST de alta, PUT ni POST de desactivación manual. Ver IMPORTACION_AEROPUERTOS.md para los campos multipart y los filtros. El país de los registros nuevos es el código ISO del archivo, por ejemplo EC. Tras aplicar AeropuertosDatosCompletos, las consultas y filas de importación incluyen metadatos con coordenadas, elevación, identificadores, códigos, región, servicio regular, enlaces, palabras clave y datosOrigenJson. Los metadatos de registros anteriores pueden ser null; no inferir coordenadas cero ni fabricar datos.

Consultar GET /api/admin/categorias-asiento para mostrar Primera, Segunda y Tercera Clase y usar sus IDs reales. No fijar IDs 1/2/3 ni mostrar botones de crear, editar o desactivar categorías. La API no carga categorías automáticamente; deben existir y estar activas para importar aviones.

Los aviones se dan de alta exclusivamente mediante importación CSV. No existe `POST /api/admin/aviones`; retirar el formulario de creación individual. En Configuración → Cargar aviones, validar el archivo y las categorías activas, revisar la vista previa y confirmar la importación. Si ya existen todas las matrículas, mostrar `ya_importados` sin crear registros. Contrato y campos: [IMPORTACION_AVIONES.md](IMPORTACION_AVIONES.md).

Los aviones no tienen endpoints de edición ni desactivación. Asientos físicos no tiene creación, edición ni desactivación manual: conservar solo sus consultas, usando avionId para filtrar. Retirar esas acciones de Flutter. La importación nunca modifica aviones existentes. Tras aplicar AvionesDatosCompletos, GET listado/detalle incorpora metadatos con IATA/ICAO, cantidades por clase, letras y JSON del CSV original. Los nuevos campos pueden ser null en aviones antiguos y no se completan automáticamente al reimportar.

Las modificaciones que alteren datos ya utilizados por vuelos históricos se rechazan. No se altera la distribución de un avión con vuelos operativos. No se desactivan elementos con dependencias operativas; una categoría necesita no tener asientos físicos activos. Mostrar el mensaje de `409`, sin ocultar ni eliminar relaciones históricas en pantalla.

### 5.2 Crear y administrar vuelos

`GET /api/admin/vuelos` usa los mismos filtros y paginación de la consulta normal. `GET /api/admin/vuelos/{id}` devuelve **un contenedor** `{"vuelo":{...},"version":"...","cantidadAsientos":9}`; no tiene la misma forma que el detalle normal.

`POST /api/admin/vuelos` genera la disponibilidad y aplica un precio por clase:

```json
{
  "numeroVuelo":"PN100",
  "avionId":1,
  "aeropuertoOrigenId":1,
  "aeropuertoDestinoId":2,
  "salidaProgramada":"2026-11-15T10:00:00Z",
  "llegadaProgramada":"2026-11-15T11:00:00Z",
  "tarifasPorClase":[
    {"categoriaId":1,"precio":150},
    {"categoriaId":2,"precio":100},
    {"categoriaId":3,"precio":70}
  ]
}
```

Indicar exactamente las clases presentes en el avión, sin repetidos. El avión, categorías, asientos y aeropuertos deben estar activos; todos los asientos físicos deben estar configurados hasta la capacidad. Origen y destino distintos, salida futura y llegada posterior. Un mismo avión no puede tener intervalos de vuelos superpuestos; devuelve `409 avion_no_disponible`. Actualmente se admiten intervalos contiguos sin tiempo de escala y no se valida continuidad geográfica entre rutas.

`PUT /api/admin/vuelos/{id}` recibe número, origen, destino, salida, llegada y `version`. Conserva avión, asientos, precios y estado. Rechaza cierre operativo, retenciones vigentes, compras registradas o reservas anteriores al flujo temporal.

`PATCH /api/admin/vuelos/{id}/avion` recibe `avionId`, `version` y `tarifasPorClase` del nuevo avión. Antes de embarque/salida, permite distinta capacidad si no hay retenciones vigentes, compras ni reservas antiguas. Pendientes nuevas canceladas/expiradas sin compra no lo bloquean. Conserva filas anteriores bloqueadas y muestra solo los asientos del avión actual; no borra historial.

### 5.3 Estados del vuelo

`POST /api/admin/vuelos/{id}/estado`:

```json
{"nuevoEstado":"embarcando","version":"COPIAR_VERSION_DEL_GET","observacion":"Inicio de embarque"}
```

| Estado actual | Destinos permitidos |
|---|---|
| `programado` | `embarcando`, `retrasado`, `cancelado` |
| `retrasado` | `programado`, `embarcando`, `cancelado` |
| `embarcando` | `en vuelo`, `cancelado` |
| `en vuelo` | `finalizado` |
| `finalizado`, `cancelado` | Ninguno |

`salidaReal` solo se admite al pasar a `en vuelo`; `llegadaReal`, al finalizar. Son opcionales y, si se omiten, se usa la hora actual del servidor. No pueden ser futuras; llegada estrictamente posterior a salida. El embarque no registra salida real y expira las pendientes del vuelo. Las inconsistencias antiguas pueden devolver `409 horarios_requieren_revision`: requieren intervención de backend, no sobrescritura desde frontend.

Cancelar un vuelo sin capturas PayPal ni pagos protegidos libera asignaciones y cancela reservas vigentes, conservando el historial. Si existen, devuelve 409 hasta resolver los pagos; no genera reembolsos automáticos. `GET /api/admin/vuelos/{id}/historial` devuelve una lista sin paginación con actor, estados, fecha y observación; también puede contener cambios de avión sin cambio de estado.

### 5.4 Asientos por vuelo y manifiesto

- `GET /api/admin/vuelos/{id}/asientos`: lista sin paginación con código, categoría (texto), precio base, estado, rowVersion y `tieneReservaActiva`. No tiene el mismo contenedor que `/api/vuelos/{id}/asientos`.
- `PATCH /api/admin/vuelos/{id}/asientos/{asientoVueloId}`: endpoint de ajuste individual existente; recibe `rowVersion` y al menos `precio` o `estado`. Estado admite solo `disponible`/`bloqueado`. No cambia disponibilidad de un asiento reservado y no modifica precios congelados de compras. La creación normal ya asigna precios por clase; no requiere ajustes asiento por asiento.
- `GET /api/admin/vuelos/{id}/reservas`: lista sin paginación de reservas y pasajeros del vuelo, con titular y sus documentos. Es administrativo; no usarlo para el historial de un cliente. Incluye estados históricos, no solo pasajeros vigentes.

### 5.5 Acumulado de ventas

`GET /api/admin/ventas/resumen` — solo admin. Por defecto devuelve capturas de Sandbox; `entorno=Live` consulta cobros reales por separado. `desde` y `hasta` son días UTC de captura incluidos. Puede enviarse solo uno de los límites.

```http
GET /api/admin/ventas/resumen?desde=2026-11-01&hasta=2026-11-30
Authorization: Bearer TOKEN_ADMIN
```

```json
{
  "totalVentas":150,
  "cantidadCompras":1,
  "moneda":"USD",
  "proveedor":"PayPal",
  "entorno":"Sandbox",
  "esSandbox":true,
  "desde":"2026-11-01",
  "hasta":"2026-11-30"
}
```

Sin capturas devuelve ceros. `cantidadCompras` cuenta pagos capturados, no pasajeros ni boletos. No suma pendientes ni comprobantes simulados históricos. Es bruto: conserva capturas con reembolso o revisión sin descontarlas. Repetir una captura no incrementa el acumulado. Para probar, comparar el incremento con `monto` del PagoPayPalResponse y una compra adicional, usando el mismo entorno.

## 6. Errores y comportamiento del frontend

| HTTP | Interpretación | Acción de interfaz |
|---|---|---|
| 400 | Campos, fechas o formato incorrectos | Mostrar errores de campos o detalle |
| 401 | Credenciales inválidas, sesión ausente, expirada o revocada | Pedir login; no confundir con permiso insuficiente |
| 403 | Sesión válida sin rol requerido | Mostrar acceso denegado |
| 404 | Recurso inexistente o reserva ajena | Mostrar no encontrado, sin revelar otra cuenta |
| 409 | Versión, disponibilidad, total, estado o dependencias cambiaron | Refrescar datos y pedir reconfirmación |
| 500 | Error interno | Mostrar fallo genérico y conservar contexto de la operación |
| 503 | Configuración/proveedor no disponible o simulación deshabilitada | Informar y coordinar con backend |
| 504 | Timeout de consulta de cédula | Permitir ingreso manual |

Los errores de negocio usan `application/problem+json`; las validaciones pueden usar `ValidationProblemDetails`. Leer tanto `detail`/`title` como `errors` y extensiones. Ejemplos:

```json
{"title":"Conflicto de reserva","status":409,"detail":"El total vigente difiere del esperado.","instance":"/api/reservas","codigo":"tarifa_actualizada","montoTotalActual":150,"moneda":"USD"}
```

```json
{"title":"One or more validation errors occurred.","status":400,"errors":{"Password":["Mensaje de validación"]}}
```

Los nombres de campos en `errors` pueden corresponder a propiedades C# o rutas de JSON; no asumir que todos sean camelCase. Códigos útiles: `reserva_expirada`, `version_desactualizada`, `asiento_no_disponible`, `tarifa_actualizada`, `tarifas_sin_configurar`, `paypal_sin_configurar`, `paypal_no_disponible`, `pago_en_curso`, `pago_inconsistente`, `compra_no_modificable`, `avion_no_disponible`, `catalogo_en_uso`. El conjunto no es un enum cerrado para el cliente: conservar un tratamiento genérico.

## 7. Prerrequisitos y límites de integración

- Backend debe aplicar manualmente sus migraciones antes de probar esta versión. No ejecutar migraciones desde frontend.
- Para comprar configurar PayPal según PAYPAL.md, incluyendo URLs y webhook de la aplicación. Duración inicial de retención: 720 minutos. La expiración de sesión es independiente de la reserva.
- Los cuatro porcentajes están configurados en `appsettings.json`: adulto 100, joven 100, nino 80 y bebe 75 (porcentajes que se pagan). No hay endpoint de cotización ni administración de esos porcentajes en esta versión; no asumir que precio base del asiento sea el total final para todos los pasajeros. Usar la validación de total y reconfirmación descrita arriba.
- Login no devuelve rol; no existe `/me`, actualización de perfil, refresh token ni gestión administrativa de usuarios en los endpoints actuales.
- PayPal está integrado; no hay reembolso automático, listado individual de ventas ni consulta global administrativa de reservas independiente de un vuelo. Consultar pagos propios y acumulados por entorno.
- Los catálogos listados son administrativos. El cliente puede obtener aeropuertos, avión y clases desde los contratos de vuelos y asientos; no asumir un endpoint público adicional para catálogos.
- **Flutter web:** en Development se permite http://localhost:62226. Cors:OrigenesPermitidos reemplaza esa lista; fuera de Development no se agregan orígenes por defecto. Ver [CORS_FLUTTER_WEB.md](CORS_FLUTTER_WEB.md). La URL base es la dirección de la API sin /swagger/index.html. CORS no sustituye la autenticación ni autorización.
- No hay push de cambios. Gestionar carga, vacío, sesión expirada, conflicto y relectura de disponibilidad. No registrar tokens ni documentos de pasajeros en logs del frontend.

## 8. Inventario completo de endpoints

Las siguientes secciones se contrastan con el Swagger generado. Los estados enumerados por operación son los declarados en sus contratos; los errores generales del apartado 6 también pueden ocurrir aunque no se repitan en todas las anotaciones.

| Método | Endpoint | Acceso | Éxito |
|---|---|---|---|
| POST | `/api/autenticacion/login` | Público | 200 |
| POST | `/api/autenticacion/logout` | Cliente/admin | 204 |
| POST | `/api/usuarios/registro` | Público | 201 |
| GET | `/api/admin/aeropuertos` | Solo admin | 200 |
| GET | `/api/admin/aeropuertos/{id}` | Solo admin | 200 |
| POST | `/api/admin/aeropuertos/importacion` | Solo admin | 200 |
| POST | `/api/admin/aeropuertos/importacion/validar` | Solo admin | 200 |
| GET | `/api/admin/categorias-asiento` | Solo admin | 200 |
| GET | `/api/admin/categorias-asiento/{id}` | Solo admin | 200 |
| GET | `/api/admin/aviones` | Solo admin | 200 |
| GET | `/api/admin/aviones/{id}` | Solo admin | 200 |
| POST | `/api/admin/aviones/importacion` | Solo admin | 200 |
| POST | `/api/admin/aviones/importacion/validar` | Solo admin | 200 |
| GET | `/api/admin/asientos-fisicos` | Solo admin | 200 |
| GET | `/api/admin/asientos-fisicos/{id}` | Solo admin | 200 |
| GET | `/api/admin/vuelos` | Solo admin | 200 |
| POST | `/api/admin/vuelos` | Solo admin | 201 |
| GET | `/api/admin/vuelos/{id}` | Solo admin | 200 |
| PUT | `/api/admin/vuelos/{id}` | Solo admin | 200 |
| GET | `/api/admin/vuelos/{id}/asientos` | Solo admin | 200 |
| PATCH | `/api/admin/vuelos/{id}/asientos/{asientoVueloId}` | Solo admin | 200 |
| PATCH | `/api/admin/vuelos/{id}/avion` | Solo admin | 200 |
| POST | `/api/admin/vuelos/{id}/estado` | Solo admin | 200 |
| GET | `/api/admin/vuelos/{id}/historial` | Solo admin | 200 |
| GET | `/api/admin/vuelos/{id}/reservas` | Solo admin | 200 |
| GET | `/api/vuelos` | Cliente/admin | 200 |
| GET | `/api/vuelos/{id}` | Cliente/admin | 200 |
| GET | `/api/vuelos/{id}/asientos` | Cliente/admin | 200 |
| GET | `/api/pasajeros/consulta-cedula/{cedula}` | Cliente/admin | 200 |
| GET | `/api/reservas` | Cliente/admin; solo propias | 200 |
| POST | `/api/reservas` | Cliente/admin | 201 |
| GET | `/api/reservas/{id}` | Cliente/admin; propietario | 200 |
| PATCH | `/api/reservas/{id}/asientos` | Cliente/admin; propietario | 200 |
| POST | `/api/reservas/{id}/cancelacion` | Cliente/admin; propietario | 200 |
| GET | `/api/admin/ventas/resumen` | Solo admin | 200 |
| POST | `/api/admin/pagos/paypal/{pagoId}/conciliacion` | Solo admin | 204 |
| POST | `/api/pagos/paypal/webhook` | Firma PayPal verificada; sin sesión | 204 |
| GET | `/api/reservas/{id}/paypal` | Cliente/admin; propietario | 200 |
| POST | `/api/reservas/{id}/paypal/captura` | Cliente/admin; propietario | 200 |
| POST | `/api/reservas/{id}/paypal/orden` | Cliente/admin; propietario | 200 |
| GET | `/api/catalogos/aeropuertos` | Cliente/admin | 200 |
| GET | `/api/catalogos/aeropuertos/{id}` | Cliente/admin | 200 |
| GET | `/api/catalogos/aviones` | Cliente/admin | 200 |

## 9. Contrato HTTP por operación

Los nombres de query y propiedades siguientes proceden del Swagger de esta versión. Se muestran los contratos completos en el apartado 10. Las validaciones condicionales, propiedad, vigencia y dependencias se describen en los apartados anteriores y también se aplican aunque un campo no figure como requerido estático.

### POST /api/autenticacion/login

**Acceso:** Público. **Grupo Swagger:** 01. Autenticación.

**Cuerpo JSON:** [LoginRequest](#dto-loginrequest).

| HTTP declarado | Respuesta |
|---|---|
| 200 | [LoginResponse](#dto-loginresponse) |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |

### POST /api/autenticacion/logout

**Acceso:** Cliente/admin. **Grupo Swagger:** 01. Autenticación.

**Cuerpo:** no enviar cuerpo.

| HTTP declarado | Respuesta |
|---|---|
| 204 | Sin cuerpo |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |

### POST /api/usuarios/registro

**Acceso:** Público. **Grupo Swagger:** 02. Usuarios.

**Cuerpo JSON:** [RegistroUsuarioRequest](#dto-registrousuariorequest).

| HTTP declarado | Respuesta |
|---|---|
| 201 | [UsuarioResponse](#dto-usuarioresponse) |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 409 | [ProblemDetails](#dto-problemdetails) |

### GET /api/admin/aeropuertos

**Acceso:** Solo admin. **Grupo Swagger:** 03. Aeropuertos (admin).

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `Buscar` | query | string | No | longitud máxima: 100 |
| `Activo` | query | boolean | No |  |
| `IncluirInactivos` | query | boolean | No |  |
| `AvionId` | query | integer (int32) | No | mínimo: 1; máximo: 2147483647 |
| `Pagina` | query | integer (int32) | No | mínimo: 1; máximo: 2147483647 |
| `TamanoPagina` | query | integer (int32) | No | mínimo: 1; máximo: 100 |

**Cuerpo:** no enviar cuerpo.

| HTTP declarado | Respuesta |
|---|---|
| 200 | [AeropuertoAdminResponsePaginaCatalogoResponse](#dto-aeropuertoadminresponsepaginacatalogoresponse) |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 404 | [ProblemDetails](#dto-problemdetails) |
| 409 | [ProblemDetails](#dto-problemdetails) |

### GET /api/admin/aeropuertos/{id}

**Acceso:** Solo admin. **Grupo Swagger:** 03. Aeropuertos (admin).

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `id` | path | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |

**Cuerpo:** no enviar cuerpo.

| HTTP declarado | Respuesta |
|---|---|
| 200 | [AeropuertoAdminResponse](#dto-aeropuertoadminresponse) |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 404 | [ProblemDetails](#dto-problemdetails) |
| 409 | [ProblemDetails](#dto-problemdetails) |

### POST /api/admin/aeropuertos/importacion

**Acceso:** Solo admin. **Grupo Swagger:** 03. Aeropuertos (admin).

**Cuerpo:** multipart/form-data (archivo y campos de formulario; no JSON).

| Campo de formulario | Tipo | Obligatorio | Restricciones |
|---|---|---|---|
| `Archivo` | string (binary) | Sí |  |
| `SoloServicioRegular` | boolean | Opcional |  |
| `PaisIso` | string | Opcional | patrón: `^[A-Za-z]{2}$` |
| `OmitirIncompletos` | boolean | Opcional |  |
| `Pagina` | integer (int32) | Sí | mínimo: 1; máximo: 150000 |
| `TamanoPagina` | integer (int32) | Sí | mínimo: 1; máximo: 200 |

| HTTP declarado | Respuesta |
|---|---|
| 200 | [ImportacionAeropuertosResponse](#dto-importacionaeropuertosresponse) |
| 400 | [ImportacionAeropuertosResponse](#dto-importacionaeropuertosresponse) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 409 | [ImportacionAeropuertosResponse](#dto-importacionaeropuertosresponse) |
| 413 | [ProblemDetails](#dto-problemdetails) |

### POST /api/admin/aeropuertos/importacion/validar

**Acceso:** Solo admin. **Grupo Swagger:** 03. Aeropuertos (admin).

**Cuerpo:** multipart/form-data (archivo y campos de formulario; no JSON).

| Campo de formulario | Tipo | Obligatorio | Restricciones |
|---|---|---|---|
| `Archivo` | string (binary) | Sí |  |
| `SoloServicioRegular` | boolean | Opcional |  |
| `PaisIso` | string | Opcional | patrón: `^[A-Za-z]{2}$` |
| `OmitirIncompletos` | boolean | Opcional |  |
| `Pagina` | integer (int32) | Sí | mínimo: 1; máximo: 150000 |
| `TamanoPagina` | integer (int32) | Sí | mínimo: 1; máximo: 200 |

| HTTP declarado | Respuesta |
|---|---|
| 200 | [ImportacionAeropuertosResponse](#dto-importacionaeropuertosresponse) |
| 400 | [ProblemDetails](#dto-problemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 413 | [ProblemDetails](#dto-problemdetails) |

### GET /api/admin/categorias-asiento

**Acceso:** Solo admin. **Grupo Swagger:** 04. Clases de asiento (admin).

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `Buscar` | query | string | No | longitud máxima: 100 |
| `Activo` | query | boolean | No |  |
| `IncluirInactivos` | query | boolean | No |  |
| `AvionId` | query | integer (int32) | No | mínimo: 1; máximo: 2147483647 |
| `Pagina` | query | integer (int32) | No | mínimo: 1; máximo: 2147483647 |
| `TamanoPagina` | query | integer (int32) | No | mínimo: 1; máximo: 100 |

**Cuerpo:** no enviar cuerpo.

| HTTP declarado | Respuesta |
|---|---|
| 200 | [CategoriaAdminResponsePaginaCatalogoResponse](#dto-categoriaadminresponsepaginacatalogoresponse) |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 404 | [ProblemDetails](#dto-problemdetails) |
| 409 | [ProblemDetails](#dto-problemdetails) |

### GET /api/admin/categorias-asiento/{id}

**Acceso:** Solo admin. **Grupo Swagger:** 04. Clases de asiento (admin).

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `id` | path | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |

**Cuerpo:** no enviar cuerpo.

| HTTP declarado | Respuesta |
|---|---|
| 200 | [CategoriaAdminResponse](#dto-categoriaadminresponse) |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 404 | [ProblemDetails](#dto-problemdetails) |
| 409 | [ProblemDetails](#dto-problemdetails) |

### GET /api/admin/aviones

**Acceso:** Solo admin. **Grupo Swagger:** 05. Aviones (admin).

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `Buscar` | query | string | No | longitud máxima: 100 |
| `Activo` | query | boolean | No |  |
| `IncluirInactivos` | query | boolean | No |  |
| `AvionId` | query | integer (int32) | No | mínimo: 1; máximo: 2147483647 |
| `Pagina` | query | integer (int32) | No | mínimo: 1; máximo: 2147483647 |
| `TamanoPagina` | query | integer (int32) | No | mínimo: 1; máximo: 100 |

**Cuerpo:** no enviar cuerpo.

| HTTP declarado | Respuesta |
|---|---|
| 200 | [AvionAdminResponsePaginaCatalogoResponse](#dto-avionadminresponsepaginacatalogoresponse) |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 404 | [ProblemDetails](#dto-problemdetails) |
| 409 | [ProblemDetails](#dto-problemdetails) |

### GET /api/admin/aviones/{id}

**Acceso:** Solo admin. **Grupo Swagger:** 05. Aviones (admin).

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `id` | path | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |

**Cuerpo:** no enviar cuerpo.

| HTTP declarado | Respuesta |
|---|---|
| 200 | [AvionAdminResponse](#dto-avionadminresponse) |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 404 | [ProblemDetails](#dto-problemdetails) |
| 409 | [ProblemDetails](#dto-problemdetails) |

### POST /api/admin/aviones/importacion

**Acceso:** Solo admin. **Grupo Swagger:** 05. Aviones (admin).

**Cuerpo:** multipart/form-data (archivo y campos de formulario; no JSON).

| Campo de formulario | Tipo | Obligatorio | Restricciones |
|---|---|---|---|
| `Archivo` | string (binary) | Sí |  |
| `PrimeraClaseId` | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |
| `SegundaClaseId` | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |
| `TerceraClaseId` | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |

| HTTP declarado | Respuesta |
|---|---|
| 200 | [ImportacionAvionesResponse](#dto-importacionavionesresponse) |
| 400 | [ImportacionAvionesResponse](#dto-importacionavionesresponse) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 409 | [ImportacionAvionesResponse](#dto-importacionavionesresponse) |
| 413 | [ProblemDetails](#dto-problemdetails) |

### POST /api/admin/aviones/importacion/validar

**Acceso:** Solo admin. **Grupo Swagger:** 05. Aviones (admin).

**Cuerpo:** multipart/form-data (archivo y campos de formulario; no JSON).

| Campo de formulario | Tipo | Obligatorio | Restricciones |
|---|---|---|---|
| `Archivo` | string (binary) | Sí |  |
| `PrimeraClaseId` | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |
| `SegundaClaseId` | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |
| `TerceraClaseId` | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |

| HTTP declarado | Respuesta |
|---|---|
| 200 | [ImportacionAvionesResponse](#dto-importacionavionesresponse) |
| 400 | [ProblemDetails](#dto-problemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 413 | [ProblemDetails](#dto-problemdetails) |

### GET /api/admin/asientos-fisicos

**Acceso:** Solo admin. **Grupo Swagger:** 06. Asientos físicos (admin).

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `Buscar` | query | string | No | longitud máxima: 100 |
| `Activo` | query | boolean | No |  |
| `IncluirInactivos` | query | boolean | No |  |
| `AvionId` | query | integer (int32) | No | mínimo: 1; máximo: 2147483647 |
| `Pagina` | query | integer (int32) | No | mínimo: 1; máximo: 2147483647 |
| `TamanoPagina` | query | integer (int32) | No | mínimo: 1; máximo: 100 |

**Cuerpo:** no enviar cuerpo.

| HTTP declarado | Respuesta |
|---|---|
| 200 | [AsientoFisicoAdminResponsePaginaCatalogoResponse](#dto-asientofisicoadminresponsepaginacatalogoresponse) |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 404 | [ProblemDetails](#dto-problemdetails) |
| 409 | [ProblemDetails](#dto-problemdetails) |

### GET /api/admin/asientos-fisicos/{id}

**Acceso:** Solo admin. **Grupo Swagger:** 06. Asientos físicos (admin).

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `id` | path | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |

**Cuerpo:** no enviar cuerpo.

| HTTP declarado | Respuesta |
|---|---|
| 200 | [AsientoFisicoAdminResponse](#dto-asientofisicoadminresponse) |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 404 | [ProblemDetails](#dto-problemdetails) |
| 409 | [ProblemDetails](#dto-problemdetails) |

### GET /api/admin/vuelos

**Acceso:** Solo admin. **Grupo Swagger:** 07. Vuelos (admin).

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `Origen` | query | string | No | patrón: `^[A-Za-z]{3}$` |
| `Destino` | query | string | No | patrón: `^[A-Za-z]{3}$` |
| `Fecha` | query | string (date) | No |  |
| `FechaDesde` | query | string (date) | No |  |
| `FechaHasta` | query | string (date) | No |  |
| `Pagina` | query | integer (int32) | No | mínimo: 1; máximo: 2147483647 |
| `TamanoPagina` | query | integer (int32) | No | mínimo: 1; máximo: 100 |

**Cuerpo:** no enviar cuerpo.

| HTTP declarado | Respuesta |
|---|---|
| 200 | [PaginaVuelosResponse](#dto-paginavuelosresponse) |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 500 | [ProblemDetails](#dto-problemdetails) |

### POST /api/admin/vuelos

**Acceso:** Solo admin. **Grupo Swagger:** 07. Vuelos (admin).

**Cuerpo JSON:** [CrearVueloAdminRequest](#dto-crearvueloadminrequest).

| HTTP declarado | Respuesta |
|---|---|
| 201 | [VueloAdminResponse](#dto-vueloadminresponse) |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 409 | [ProblemDetails](#dto-problemdetails) |
| 500 | [ProblemDetails](#dto-problemdetails) |

### GET /api/admin/vuelos/{id}

**Acceso:** Solo admin. **Grupo Swagger:** 07. Vuelos (admin).

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `id` | path | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |

**Cuerpo:** no enviar cuerpo.

| HTTP declarado | Respuesta |
|---|---|
| 200 | [VueloAdminResponse](#dto-vueloadminresponse) |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 404 | [ProblemDetails](#dto-problemdetails) |
| 500 | [ProblemDetails](#dto-problemdetails) |

### PUT /api/admin/vuelos/{id}

**Acceso:** Solo admin. **Grupo Swagger:** 07. Vuelos (admin).

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `id` | path | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |

**Cuerpo JSON:** [EditarVueloAdminRequest](#dto-editarvueloadminrequest).

| HTTP declarado | Respuesta |
|---|---|
| 200 | [VueloAdminResponse](#dto-vueloadminresponse) |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 404 | [ProblemDetails](#dto-problemdetails) |
| 409 | [ProblemDetails](#dto-problemdetails) |
| 500 | [ProblemDetails](#dto-problemdetails) |

### GET /api/admin/vuelos/{id}/asientos

**Acceso:** Solo admin. **Grupo Swagger:** 07. Vuelos (admin).

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `id` | path | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |

**Cuerpo:** no enviar cuerpo.

| HTTP declarado | Respuesta |
|---|---|
| 200 | [AsientoVueloAdminResponse](#dto-asientovueloadminresponse)[] |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 404 | [ProblemDetails](#dto-problemdetails) |
| 500 | [ProblemDetails](#dto-problemdetails) |

### PATCH /api/admin/vuelos/{id}/asientos/{asientoVueloId}

**Acceso:** Solo admin. **Grupo Swagger:** 07. Vuelos (admin).

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `id` | path | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |
| `asientoVueloId` | path | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |

**Cuerpo JSON:** [ActualizarAsientoVueloAdminRequest](#dto-actualizarasientovueloadminrequest).

| HTTP declarado | Respuesta |
|---|---|
| 200 | [AsientoVueloAdminResponse](#dto-asientovueloadminresponse) |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 404 | [ProblemDetails](#dto-problemdetails) |
| 409 | [ProblemDetails](#dto-problemdetails) |
| 500 | [ProblemDetails](#dto-problemdetails) |

### PATCH /api/admin/vuelos/{id}/avion

**Acceso:** Solo admin. **Grupo Swagger:** 07. Vuelos (admin).

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `id` | path | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |

**Cuerpo JSON:** [CambiarAvionVueloRequest](#dto-cambiaravionvuelorequest).

| HTTP declarado | Respuesta |
|---|---|
| 200 | [VueloAdminResponse](#dto-vueloadminresponse) |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 404 | [ProblemDetails](#dto-problemdetails) |
| 409 | [ProblemDetails](#dto-problemdetails) |
| 500 | [ProblemDetails](#dto-problemdetails) |

### POST /api/admin/vuelos/{id}/estado

**Acceso:** Solo admin. **Grupo Swagger:** 07. Vuelos (admin).

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `id` | path | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |

**Cuerpo JSON:** [CambiarEstadoVueloAdminRequest](#dto-cambiarestadovueloadminrequest).

| HTTP declarado | Respuesta |
|---|---|
| 200 | [VueloAdminResponse](#dto-vueloadminresponse) |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 404 | [ProblemDetails](#dto-problemdetails) |
| 409 | [ProblemDetails](#dto-problemdetails) |
| 500 | [ProblemDetails](#dto-problemdetails) |

### GET /api/admin/vuelos/{id}/historial

**Acceso:** Solo admin. **Grupo Swagger:** 07. Vuelos (admin).

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `id` | path | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |

**Cuerpo:** no enviar cuerpo.

| HTTP declarado | Respuesta |
|---|---|
| 200 | [HistorialEstadoVueloAdminResponse](#dto-historialestadovueloadminresponse)[] |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 404 | [ProblemDetails](#dto-problemdetails) |
| 500 | [ProblemDetails](#dto-problemdetails) |

### GET /api/admin/vuelos/{id}/reservas

**Acceso:** Solo admin. **Grupo Swagger:** 07. Vuelos (admin).

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `id` | path | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |

**Cuerpo:** no enviar cuerpo.

| HTTP declarado | Respuesta |
|---|---|
| 200 | [ReservaVueloAdminResponse](#dto-reservavueloadminresponse)[] |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 404 | [ProblemDetails](#dto-problemdetails) |
| 500 | [ProblemDetails](#dto-problemdetails) |

### GET /api/vuelos

**Acceso:** Cliente/admin. **Grupo Swagger:** 08. Consulta de vuelos.

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `Origen` | query | string | No | patrón: `^[A-Za-z]{3}$` |
| `Destino` | query | string | No | patrón: `^[A-Za-z]{3}$` |
| `Fecha` | query | string (date) | No |  |
| `FechaDesde` | query | string (date) | No |  |
| `FechaHasta` | query | string (date) | No |  |
| `Pagina` | query | integer (int32) | No | mínimo: 1; máximo: 2147483647 |
| `TamanoPagina` | query | integer (int32) | No | mínimo: 1; máximo: 100 |

**Cuerpo:** no enviar cuerpo.

| HTTP declarado | Respuesta |
|---|---|
| 200 | [PaginaVuelosResponse](#dto-paginavuelosresponse) |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |

### GET /api/vuelos/{id}

**Acceso:** Cliente/admin. **Grupo Swagger:** 08. Consulta de vuelos.

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `id` | path | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |

**Cuerpo:** no enviar cuerpo.

| HTTP declarado | Respuesta |
|---|---|
| 200 | [VueloResponse](#dto-vueloresponse) |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 404 | [ProblemDetails](#dto-problemdetails) |

### GET /api/vuelos/{id}/asientos

**Acceso:** Cliente/admin. **Grupo Swagger:** 08. Consulta de vuelos.

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `id` | path | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |

**Cuerpo:** no enviar cuerpo.

| HTTP declarado | Respuesta |
|---|---|
| 200 | [AsientosVueloResponse](#dto-asientosvueloresponse) |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 404 | [ProblemDetails](#dto-problemdetails) |

### GET /api/pasajeros/consulta-cedula/{cedula}

**Acceso:** Cliente/admin. **Grupo Swagger:** 09. Consulta de pasajeros.

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `cedula` | path | string | Sí | longitud mínima: 10; longitud máxima: 10; patrón: `^[0-9]{10}$` |

**Cuerpo:** no enviar cuerpo.

| HTTP declarado | Respuesta |
|---|---|
| 200 | [ConsultaCedulaResponse](#dto-consultacedularesponse) |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 404 | [ProblemDetails](#dto-problemdetails) |
| 503 | [ProblemDetails](#dto-problemdetails) |
| 504 | [ProblemDetails](#dto-problemdetails) |

### GET /api/reservas

**Acceso:** Cliente/admin; solo propias. **Grupo Swagger:** 10. Reservas.

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `Estado` | query | string | No | patrón: `^(confirmada\|cancelada\|modificada\|pendiente_pago\|pagada\|expirada)$` |
| `FechaDesde` | query | string (date) | No |  |
| `FechaHasta` | query | string (date) | No |  |
| `Pagina` | query | integer (int32) | No | mínimo: 1; máximo: 2147483647 |
| `TamanoPagina` | query | integer (int32) | No | mínimo: 1; máximo: 100 |

**Cuerpo:** no enviar cuerpo.

| HTTP declarado | Respuesta |
|---|---|
| 200 | [PaginaReservasResponse](#dto-paginareservasresponse) |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 500 | [ProblemDetails](#dto-problemdetails) |

### POST /api/reservas

**Acceso:** Cliente/admin. **Grupo Swagger:** 10. Reservas.

**Cuerpo JSON:** [CrearReservaRequest](#dto-crearreservarequest).

| HTTP declarado | Respuesta |
|---|---|
| 201 | [ReservaResponse](#dto-reservaresponse) |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 404 | [ProblemDetails](#dto-problemdetails) |
| 409 | [ProblemDetails](#dto-problemdetails) |
| 500 | [ProblemDetails](#dto-problemdetails) |
| 503 | [ProblemDetails](#dto-problemdetails) |

### GET /api/reservas/{id}

**Acceso:** Cliente/admin; propietario. **Grupo Swagger:** 10. Reservas.

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `id` | path | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |

**Cuerpo:** no enviar cuerpo.

| HTTP declarado | Respuesta |
|---|---|
| 200 | [ReservaResponse](#dto-reservaresponse) |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 404 | [ProblemDetails](#dto-problemdetails) |
| 500 | [ProblemDetails](#dto-problemdetails) |

### PATCH /api/reservas/{id}/asientos

**Acceso:** Cliente/admin; propietario. **Grupo Swagger:** 10. Reservas.

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `id` | path | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |

**Cuerpo JSON:** [CambiarAsientosRequest](#dto-cambiarasientosrequest).

| HTTP declarado | Respuesta |
|---|---|
| 200 | [ReservaResponse](#dto-reservaresponse) |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 404 | [ProblemDetails](#dto-problemdetails) |
| 409 | [ProblemDetails](#dto-problemdetails) |
| 500 | [ProblemDetails](#dto-problemdetails) |
| 503 | [ProblemDetails](#dto-problemdetails) |

### POST /api/reservas/{id}/cancelacion

**Acceso:** Cliente/admin; propietario. **Grupo Swagger:** 10. Reservas.

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `id` | path | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |

**Cuerpo JSON:** [CancelarReservaRequest](#dto-cancelarreservarequest).

| HTTP declarado | Respuesta |
|---|---|
| 200 | [ReservaResponse](#dto-reservaresponse) |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 404 | [ProblemDetails](#dto-problemdetails) |
| 409 | [ProblemDetails](#dto-problemdetails) |
| 500 | [ProblemDetails](#dto-problemdetails) |

### GET /api/admin/ventas/resumen

**Acceso:** Solo admin. **Grupo Swagger:** 11. Ventas acumuladas (admin).

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `Entorno` | query | string | No | patrón: `^(Sandbox\|Live)$` |
| `Desde` | query | string (date) | No |  |
| `Hasta` | query | string (date) | No |  |

**Cuerpo:** no enviar cuerpo.

| HTTP declarado | Respuesta |
|---|---|
| 200 | [ResumenVentasResponse](#dto-resumenventasresponse) |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |

### POST /api/admin/pagos/paypal/{pagoId}/conciliacion

**Acceso:** Solo admin. **Grupo Swagger:** 12. Pagos PayPal.

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `pagoId` | path | string (uuid) | Sí |  |

**Cuerpo:** no enviar cuerpo.

| HTTP declarado | Respuesta |
|---|---|
| 204 | Sin cuerpo |
| 400 | [ProblemDetails](#dto-problemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 404 | [ProblemDetails](#dto-problemdetails) |
| 409 | [ProblemDetails](#dto-problemdetails) |
| 503 | [ProblemDetails](#dto-problemdetails) |

### POST /api/pagos/paypal/webhook

**Acceso:** Firma PayPal verificada; sin sesión. **Grupo Swagger:** 12. Pagos PayPal.

**Cuerpo JSON:** object.

| HTTP declarado | Respuesta |
|---|---|
| 204 | Sin cuerpo |
| 400 | [ProblemDetails](#dto-problemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 404 | [ProblemDetails](#dto-problemdetails) |
| 409 | [ProblemDetails](#dto-problemdetails) |
| 503 | [ProblemDetails](#dto-problemdetails) |

### GET /api/reservas/{id}/paypal

**Acceso:** Cliente/admin; propietario. **Grupo Swagger:** 12. Pagos PayPal.

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `id` | path | integer (int32) | Sí | mínimo: 1 |

**Cuerpo:** no enviar cuerpo.

| HTTP declarado | Respuesta |
|---|---|
| 200 | [PagoPayPalResponse](#dto-pagopaypalresponse) |
| 400 | [ProblemDetails](#dto-problemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 404 | [ProblemDetails](#dto-problemdetails) |
| 409 | [ProblemDetails](#dto-problemdetails) |
| 503 | [ProblemDetails](#dto-problemdetails) |

### POST /api/reservas/{id}/paypal/captura

**Acceso:** Cliente/admin; propietario. **Grupo Swagger:** 12. Pagos PayPal.

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `id` | path | integer (int32) | Sí | mínimo: 1 |

**Cuerpo:** no enviar cuerpo.

| HTTP declarado | Respuesta |
|---|---|
| 200 | [PagoPayPalResponse](#dto-pagopaypalresponse) |
| 400 | [ProblemDetails](#dto-problemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 404 | [ProblemDetails](#dto-problemdetails) |
| 409 | [ProblemDetails](#dto-problemdetails) |
| 503 | [ProblemDetails](#dto-problemdetails) |

### POST /api/reservas/{id}/paypal/orden

**Acceso:** Cliente/admin; propietario. **Grupo Swagger:** 12. Pagos PayPal.

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `id` | path | integer (int32) | Sí | mínimo: 1 |

**Cuerpo JSON:** [CrearOrdenPayPalRequest](#dto-crearordenpaypalrequest).

| HTTP declarado | Respuesta |
|---|---|
| 200 | [PagoPayPalResponse](#dto-pagopaypalresponse) |
| 400 | [ProblemDetails](#dto-problemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 404 | [ProblemDetails](#dto-problemdetails) |
| 409 | [ProblemDetails](#dto-problemdetails) |
| 503 | [ProblemDetails](#dto-problemdetails) |

### GET /api/catalogos/aeropuertos

**Acceso:** Cliente/admin. **Grupo Swagger:** 13. Búsqueda de catálogos (selectores).

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `Q` | query | string | No | longitud máxima: 100 |
| `Limite` | query | integer (int32) | No | mínimo: 1; máximo: 100 |

**Cuerpo:** no enviar cuerpo.

| HTTP declarado | Respuesta |
|---|---|
| 200 | [AeropuertoResumenResponse](#dto-aeropuertoresumenresponse)[] |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |

### GET /api/catalogos/aeropuertos/{id}

**Acceso:** Cliente/admin. **Grupo Swagger:** 13. Búsqueda de catálogos (selectores).

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `id` | path | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |

**Cuerpo:** no enviar cuerpo.

| HTTP declarado | Respuesta |
|---|---|
| 200 | [AeropuertoResumenResponse](#dto-aeropuertoresumenresponse) |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |
| 404 | [ProblemDetails](#dto-problemdetails) |

### GET /api/catalogos/aviones

**Acceso:** Cliente/admin. **Grupo Swagger:** 13. Búsqueda de catálogos (selectores).

| Parámetro | Lugar | Tipo | Obligatorio | Restricciones |
|---|---|---|---|---|
| `Q` | query | string | No | longitud máxima: 100 |
| `Limite` | query | integer (int32) | No | mínimo: 1; máximo: 100 |

**Cuerpo:** no enviar cuerpo.

| HTTP declarado | Respuesta |
|---|---|
| 200 | [AvionResumenResponse](#dto-avionresumenresponse)[] |
| 400 | [ValidationProblemDetails](#dto-validationproblemdetails) |
| 401 | [ProblemDetails](#dto-problemdetails) |
| 403 | [ProblemDetails](#dto-problemdetails) |

## 10. Contratos JSON: campos y restricciones

Los contratos siguientes se obtuvieron del Swagger generado. En solicitudes, «Sí» significa campo requerido del esquema; «Sí por validación» identifica valores positivos no anulables cuyo valor omitido sería rechazado. Hay requisitos condicionales adicionales: por ejemplo, rowVersion al cambiar/cancelar reservas y tarifas por clase al crear un vuelo. Un string requerido no debe estar vacío ni compuesto solo por espacios.

Para respuestas, «Devuelto» describe propiedades del DTO, no campos que deba enviar el cliente. Valores anulables, colecciones vacías y comprobante ausente son casos válidos. ProblemDetails/ValidationProblemDetails pueden omitir propiedades y añadir extensiones. Los formatos numéricos son los publicados por Swagger; los importes del servidor utilizan decimal.

### DTO ActualizarAsientoVueloAdminRequest

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `precio` | number (double) o null | Opcional/condicional | mínimo: 0; máximo: 99999999.99 |
| `estado` | string o null | Opcional/condicional | patrón: `^(disponible\|bloqueado)$` |
| `rowVersion` | string | Sí | longitud mínima: 1 |

### DTO AeropuertoAdminResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `aeropuertoId` | integer (int32) | Devuelto |  |
| `codigoIata` | string o null | Devuelto |  |
| `nombre` | string o null | Devuelto |  |
| `ciudad` | string o null | Devuelto |  |
| `pais` | string o null | Devuelto |  |
| `activo` | boolean | Devuelto |  |
| `fechaDesactivacion` | string (date-time) o null | Devuelto |  |
| `rowVersion` | string o null | Devuelto |  |
| `metadatos` | [MetadatosAeropuertoResponse](#dto-metadatosaeropuertoresponse) | Devuelto |  |

### DTO AeropuertoAdminResponsePaginaCatalogoResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `elementos` | [AeropuertoAdminResponse](#dto-aeropuertoadminresponse)[] o null | Devuelto |  |
| `pagina` | integer (int32) | Devuelto |  |
| `tamanoPagina` | integer (int32) | Devuelto |  |
| `total` | integer (int32) | Devuelto |  |
| `totalPaginas` | integer (int32) | Devuelto |  |

### DTO AeropuertoResumenResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `aeropuertoId` | integer (int32) | Devuelto |  |
| `codigoIata` | string o null | Devuelto |  |
| `codigoIcao` | string o null | Devuelto |  |
| `nombre` | string o null | Devuelto |  |
| `ciudad` | string o null | Devuelto |  |
| `pais` | string o null | Devuelto |  |

### DTO AeropuertoVueloResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `aeropuertoId` | integer (int32) | Devuelto |  |
| `codigoIata` | string o null | Devuelto |  |
| `nombre` | string o null | Devuelto |  |
| `ciudad` | string o null | Devuelto |  |
| `pais` | string o null | Devuelto |  |

### DTO AsientoFisicoAdminResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `asientoFisicoId` | integer (int32) | Devuelto |  |
| `avionId` | integer (int32) | Devuelto |  |
| `categoriaId` | integer (int32) | Devuelto |  |
| `codigoAsiento` | string o null | Devuelto |  |
| `activo` | boolean | Devuelto |  |
| `fechaDesactivacion` | string (date-time) o null | Devuelto |  |
| `rowVersion` | string o null | Devuelto |  |

### DTO AsientoFisicoAdminResponsePaginaCatalogoResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `elementos` | [AsientoFisicoAdminResponse](#dto-asientofisicoadminresponse)[] o null | Devuelto |  |
| `pagina` | integer (int32) | Devuelto |  |
| `tamanoPagina` | integer (int32) | Devuelto |  |
| `total` | integer (int32) | Devuelto |  |
| `totalPaginas` | integer (int32) | Devuelto |  |

### DTO AsientosVueloResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `vueloId` | integer (int32) | Devuelto |  |
| `numeroVuelo` | string o null | Devuelto |  |
| `moneda` | string o null | Devuelto |  |
| `permiteReservas` | boolean | Devuelto |  |
| `asientos` | [AsientoVueloResponse](#dto-asientovueloresponse)[] o null | Devuelto |  |

### DTO AsientoVueloAdminResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `asientoVueloId` | integer (int32) | Devuelto |  |
| `asientoFisicoId` | integer (int32) | Devuelto |  |
| `codigoAsiento` | string o null | Devuelto |  |
| `categoria` | string o null | Devuelto |  |
| `precio` | number (double) | Devuelto |  |
| `estado` | string o null | Devuelto |  |
| `rowVersion` | string o null | Devuelto |  |
| `tieneReservaActiva` | boolean | Devuelto |  |

### DTO AsientoVueloResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `asientoVueloId` | integer (int32) | Devuelto |  |
| `codigoAsiento` | string o null | Devuelto |  |
| `categoria` | [CategoriaAsientoResponse](#dto-categoriaasientoresponse) | Devuelto |  |
| `precio` | number (double) | Devuelto |  |
| `moneda` | string o null | Devuelto |  |
| `estado` | string o null | Devuelto |  |
| `disponible` | boolean | Devuelto |  |
| `rowVersion` | string o null | Devuelto |  |

### DTO AvionAdminResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `avionId` | integer (int32) | Devuelto |  |
| `modelo` | string o null | Devuelto |  |
| `matricula` | string o null | Devuelto |  |
| `capacidadTotal` | integer (int32) | Devuelto |  |
| `activo` | boolean | Devuelto |  |
| `fechaDesactivacion` | string (date-time) o null | Devuelto |  |
| `rowVersion` | string o null | Devuelto |  |
| `metadatos` | [MetadatosAvionResponse](#dto-metadatosavionresponse) | Devuelto |  |

### DTO AvionAdminResponsePaginaCatalogoResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `elementos` | [AvionAdminResponse](#dto-avionadminresponse)[] o null | Devuelto |  |
| `pagina` | integer (int32) | Devuelto |  |
| `tamanoPagina` | integer (int32) | Devuelto |  |
| `total` | integer (int32) | Devuelto |  |
| `totalPaginas` | integer (int32) | Devuelto |  |

### DTO AvionResumenResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `avionId` | integer (int32) | Devuelto |  |
| `modelo` | string o null | Devuelto |  |
| `matricula` | string o null | Devuelto |  |
| `capacidadTotal` | integer (int32) | Devuelto |  |

### DTO AvionVueloResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `avionId` | integer (int32) | Devuelto |  |
| `modelo` | string o null | Devuelto |  |
| `capacidadTotal` | integer (int32) | Devuelto |  |

### DTO CambiarAsientosRequest

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `rowVersion` | string | Sí | longitud mínima: 1 |
| `totalEsperado` | number (double) | Sí | mínimo: 0; máximo: 99999999.99 |
| `cambios` | [CambioAsientoRequest](#dto-cambioasientorequest)[] | Sí | mínimo de elementos: 1 |

### DTO CambiarAvionVueloRequest

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `avionId` | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |
| `version` | string | Sí | longitud mínima: 1; patrón: `^[A-Fa-f0-9]{64}$` |
| `tarifasPorClase` | [PrecioClaseAdminRequest](#dto-precioclaseadminrequest)[] | Sí | mínimo de elementos: 1; máximo de elementos: 1000 |

### DTO CambiarEstadoVueloAdminRequest

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `nuevoEstado` | string | Sí | longitud mínima: 1; patrón: `^(programado\|embarcando\|retrasado\|en vuelo\|finalizado\|cancelado)$` |
| `version` | string | Sí | longitud mínima: 1; patrón: `^[A-Fa-f0-9]{64}$` |
| `observacion` | string o null | Opcional/condicional | longitud máxima: 255 |
| `salidaReal` | string (date-time) o null | Opcional/condicional |  |
| `llegadaReal` | string (date-time) o null | Opcional/condicional |  |

### DTO CambioAsientoRequest

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `detalleId` | integer (int32) | Sí por validación | mínimo: 1; máximo: 2147483647 |
| `asientoVueloId` | integer (int32) | Sí por validación | mínimo: 1; máximo: 2147483647 |
| `rowVersionAsiento` | string | Sí | longitud mínima: 1 |

### DTO CancelarReservaRequest

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `rowVersion` | string | Sí | longitud mínima: 1 |

### DTO CategoriaAdminResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `categoriaId` | integer (int32) | Devuelto |  |
| `nombre` | string o null | Devuelto |  |
| `descripcion` | string o null | Devuelto |  |
| `activo` | boolean | Devuelto |  |
| `fechaDesactivacion` | string (date-time) o null | Devuelto |  |
| `rowVersion` | string o null | Devuelto |  |

### DTO CategoriaAdminResponsePaginaCatalogoResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `elementos` | [CategoriaAdminResponse](#dto-categoriaadminresponse)[] o null | Devuelto |  |
| `pagina` | integer (int32) | Devuelto |  |
| `tamanoPagina` | integer (int32) | Devuelto |  |
| `total` | integer (int32) | Devuelto |  |
| `totalPaginas` | integer (int32) | Devuelto |  |

### DTO CategoriaAsientoResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `categoriaId` | integer (int32) | Devuelto |  |
| `nombre` | string o null | Devuelto |  |
| `descripcion` | string o null | Devuelto |  |

### DTO CompraSimuladaResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `compraId` | string (uuid) | Devuelto |  |
| `fechaCompra` | string (date-time) | Devuelto |  |
| `monto` | number (double) | Devuelto |  |
| `moneda` | string o null | Devuelto |  |
| `esSimulada` | boolean | Devuelto |  |

### DTO ConsultaCedulaResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `cedula` | string o null | Devuelto |  |
| `nombreCompleto` | string o null | Devuelto |  |
| `primerNombre` | string o null | Devuelto |  |
| `segundoNombre` | string o null | Devuelto |  |
| `primerApellido` | string o null | Devuelto |  |
| `segundoApellido` | string o null | Devuelto |  |
| `requiereRevision` | boolean | Devuelto |  |

### DTO CrearOrdenPayPalRequest

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `rowVersion` | string | Sí | longitud mínima: 1; patrón: `^[A-Za-z0-9+/]{11}=$` |

### DTO CrearReservaRequest

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `vueloId` | integer (int32) | Sí por validación | mínimo: 1; máximo: 2147483647 |
| `totalEsperado` | number (double) | Sí | mínimo: 0; máximo: 99999999.99 |
| `pasajeros` | [PasajeroReservaRequest](#dto-pasajeroreservarequest)[] | Sí | mínimo de elementos: 1 |

### DTO CrearVueloAdminRequest

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `numeroVuelo` | string | Sí | longitud máxima: 20; patrón: `^[A-Za-z0-9][A-Za-z0-9 -]*$` |
| `avionId` | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |
| `aeropuertoOrigenId` | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |
| `aeropuertoDestinoId` | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |
| `salidaProgramada` | string (date-time) | Sí |  |
| `llegadaProgramada` | string (date-time) | Sí |  |
| `tarifasPorClase` | [PrecioClaseAdminRequest](#dto-precioclaseadminrequest)[] | Sí | mínimo de elementos: 1; máximo de elementos: 1000 |

### DTO DetalleReservaResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `detalleId` | integer (int32) | Devuelto |  |
| `asientoVueloId` | integer (int32) | Devuelto |  |
| `codigoAsiento` | string o null | Devuelto |  |
| `primerNombre` | string o null | Devuelto |  |
| `segundoNombre` | string o null | Devuelto |  |
| `primerApellido` | string o null | Devuelto |  |
| `segundoApellido` | string o null | Devuelto |  |
| `cedula` | string o null | Devuelto |  |
| `tipoPasajero` | string o null | Devuelto |  |
| `precioCongelado` | number (double) | Devuelto |  |
| `estado` | string o null | Devuelto |  |
| `rowVersionAsiento` | string o null | Devuelto |  |

### DTO EditarVueloAdminRequest

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `numeroVuelo` | string | Sí | longitud máxima: 20; patrón: `^[A-Za-z0-9][A-Za-z0-9 -]*$` |
| `aeropuertoOrigenId` | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |
| `aeropuertoDestinoId` | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |
| `salidaProgramada` | string (date-time) | Sí |  |
| `llegadaProgramada` | string (date-time) | Sí |  |
| `version` | string | Sí | longitud mínima: 1; patrón: `^[A-Fa-f0-9]{64}$` |

### DTO ErrorImportacionAeropuertoResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `fila` | integer (int32) | Devuelto |  |
| `campo` | string o null | Devuelto |  |
| `mensaje` | string o null | Devuelto |  |

### DTO ErrorImportacionAvionResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `fila` | integer (int32) | Devuelto |  |
| `campo` | string o null | Devuelto |  |
| `mensaje` | string o null | Devuelto |  |

### DTO FilaImportacionAeropuertoResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `fila` | integer (int32) | Devuelto |  |
| `codigoIata` | string o null | Devuelto |  |
| `nombre` | string o null | Devuelto |  |
| `ciudad` | string o null | Devuelto |  |
| `pais` | string o null | Devuelto |  |
| `codigoRegistrado` | boolean | Devuelto |  |
| `aeropuertoId` | integer (int32) o null | Devuelto |  |
| `metadatos` | [MetadatosAeropuertoResponse](#dto-metadatosaeropuertoresponse) | Devuelto |  |

### DTO FilaImportacionAvionResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `fila` | integer (int32) | Devuelto |  |
| `modelo` | string o null | Devuelto |  |
| `matricula` | string o null | Devuelto |  |
| `capacidadTotal` | integer (int32) | Devuelto |  |
| `cantidadPrimeraClase` | integer (int32) | Devuelto |  |
| `cantidadSegundaClase` | integer (int32) | Devuelto |  |
| `cantidadTerceraClase` | integer (int32) | Devuelto |  |
| `letrasPorFila` | string o null | Devuelto |  |
| `codigoIata` | string o null | Devuelto |  |
| `codigoIcao` | string o null | Devuelto |  |
| `matriculaRegistrada` | boolean | Devuelto |  |
| `avionId` | integer (int32) o null | Devuelto |  |
| `datosOrigenJson` | string o null | Devuelto |  |

### DTO HistorialEstadoVueloAdminResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `historialId` | integer (int32) | Devuelto |  |
| `vueloId` | integer (int32) | Devuelto |  |
| `estadoAnterior` | string o null | Devuelto |  |
| `estadoNuevo` | string o null | Devuelto |  |
| `fechaCambio` | string (date-time) | Devuelto |  |
| `usuarioAdminId` | integer (int32) | Devuelto |  |
| `adminNombre` | string o null | Devuelto |  |
| `observacion` | string o null | Devuelto |  |

### DTO HistorialReservaResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `historialReservaId` | integer (int32) | Devuelto |  |
| `tipoAccion` | string o null | Devuelto |  |
| `fechaAccion` | string (date-time) | Devuelto |  |
| `asientoVueloAnteriorId` | integer (int32) o null | Devuelto |  |
| `asientoVueloNuevoId` | integer (int32) o null | Devuelto |  |
| `observacion` | string o null | Devuelto |  |

### DTO ImportacionAeropuertosResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `estado` | string o null | Devuelto |  |
| `mensaje` | string o null | Devuelto |  |
| `puedeImportar` | boolean | Devuelto |  |
| `totalRegistros` | integer (int32) | Devuelto |  |
| `totalImportables` | integer (int32) | Devuelto |  |
| `totalOmitidos` | integer (int32) | Devuelto |  |
| `totalErrores` | integer (int32) | Devuelto |  |
| `erroresTruncados` | boolean | Devuelto |  |
| `totalExistentes` | integer (int32) | Devuelto |  |
| `aeropuertosImportados` | integer (int32) | Devuelto |  |
| `pagina` | integer (int32) | Devuelto |  |
| `tamanoPagina` | integer (int32) | Devuelto |  |
| `totalPaginas` | integer (int32) | Devuelto |  |
| `filas` | [FilaImportacionAeropuertoResponse](#dto-filaimportacionaeropuertoresponse)[] o null | Devuelto |  |
| `errores` | [ErrorImportacionAeropuertoResponse](#dto-errorimportacionaeropuertoresponse)[] o null | Devuelto |  |
| `resumenOmisiones` | object o null | Devuelto |  |

### DTO ImportacionAvionesResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `estado` | string o null | Devuelto |  |
| `mensaje` | string o null | Devuelto |  |
| `puedeImportar` | boolean | Devuelto |  |
| `totalFilas` | integer (int32) | Devuelto |  |
| `totalAsientos` | integer (int32) | Devuelto |  |
| `avionesImportados` | integer (int32) | Devuelto |  |
| `asientosImportados` | integer (int32) | Devuelto |  |
| `filas` | [FilaImportacionAvionResponse](#dto-filaimportacionavionresponse)[] o null | Devuelto |  |
| `errores` | [ErrorImportacionAvionResponse](#dto-errorimportacionavionresponse)[] o null | Devuelto |  |
| `advertencias` | string[] o null | Devuelto |  |

### DTO LoginRequest

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `email` | string (email) | Sí | longitud máxima: 150 |
| `password` | string | Sí | longitud máxima: 128 |

### DTO LoginResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `token` | string o null | Devuelto |  |
| `tipoToken` | string o null | Devuelto |  |
| `fechaExpiracion` | string (date-time) | Devuelto |  |
| `usuario` | [UsuarioResponse](#dto-usuarioresponse) | Devuelto |  |

### DTO MetadatosAeropuertoResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `idOrigen` | integer (int64) o null | Devuelto |  |
| `identificador` | string o null | Devuelto |  |
| `tipo` | string o null | Devuelto |  |
| `latitud` | number (double) o null | Devuelto |  |
| `longitud` | number (double) o null | Devuelto |  |
| `elevacionPies` | number (double) o null | Devuelto |  |
| `continente` | string o null | Devuelto |  |
| `regionIso` | string o null | Devuelto |  |
| `servicioRegular` | boolean o null | Devuelto |  |
| `codigoIcao` | string o null | Devuelto |  |
| `codigoGps` | string o null | Devuelto |  |
| `codigoLocal` | string o null | Devuelto |  |
| `sitioWeb` | string o null | Devuelto |  |
| `wikipedia` | string o null | Devuelto |  |
| `palabrasClave` | string o null | Devuelto |  |
| `datosOrigenJson` | string o null | Devuelto |  |

### DTO MetadatosAvionResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `codigoIata` | string o null | Devuelto |  |
| `codigoIcao` | string o null | Devuelto |  |
| `cantidadPrimeraClase` | integer (int32) o null | Devuelto |  |
| `cantidadSegundaClase` | integer (int32) o null | Devuelto |  |
| `cantidadTerceraClase` | integer (int32) o null | Devuelto |  |
| `letrasPorFila` | string o null | Devuelto |  |
| `datosOrigenJson` | string o null | Devuelto |  |

### DTO PaginaReservasResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `reservas` | [ReservaResumenResponse](#dto-reservaresumenresponse)[] o null | Devuelto |  |
| `pagina` | integer (int32) | Devuelto |  |
| `tamanoPagina` | integer (int32) | Devuelto |  |
| `total` | integer (int32) | Devuelto |  |
| `totalPaginas` | integer (int32) | Devuelto |  |

### DTO PaginaVuelosResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `vuelos` | [VueloResponse](#dto-vueloresponse)[] o null | Devuelto |  |
| `pagina` | integer (int32) | Devuelto |  |
| `tamanoPagina` | integer (int32) | Devuelto |  |
| `total` | integer (int32) | Devuelto |  |
| `totalPaginas` | integer (int32) | Devuelto |  |

### DTO PagoPayPalResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `pagoId` | string (uuid) | Devuelto |  |
| `reservaId` | integer (int32) | Devuelto |  |
| `estado` | string o null | Devuelto |  |
| `estadoReserva` | string o null | Devuelto |  |
| `monto` | number (double) | Devuelto |  |
| `moneda` | string o null | Devuelto |  |
| `entorno` | string o null | Devuelto |  |
| `esSandbox` | boolean | Devuelto |  |
| `ordenId` | string o null | Devuelto |  |
| `capturaId` | string o null | Devuelto |  |
| `urlAprobacion` | string o null | Devuelto |  |
| `fechaCaptura` | string (date-time) o null | Devuelto |  |
| `requiereRevision` | boolean | Devuelto |  |
| `rowVersionReserva` | string o null | Devuelto |  |
| `subtotal` | number (double) o null | Devuelto |  |
| `porcentajeIva` | number (double) o null | Devuelto |  |
| `montoIva` | number (double) o null | Devuelto |  |

### DTO PasajeroReservaRequest

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `cedula` | string | Sí | longitud máxima: 50 |
| `primerNombre` | string | Sí | longitud máxima: 100 |
| `segundoNombre` | string o null | Opcional/condicional | longitud máxima: 100 |
| `primerApellido` | string | Sí | longitud máxima: 100 |
| `segundoApellido` | string o null | Opcional/condicional | longitud máxima: 100 |
| `tipoPasajero` | string | Sí | longitud mínima: 1; patrón: `^(adulto\|joven\|nino\|bebe)$` |
| `asientoVueloId` | integer (int32) | Sí por validación | mínimo: 1; máximo: 2147483647 |
| `rowVersionAsiento` | string | Sí | longitud mínima: 1 |

### DTO PasajeroVueloAdminResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `detalleId` | integer (int32) | Devuelto |  |
| `primerNombre` | string o null | Devuelto |  |
| `segundoNombre` | string o null | Devuelto |  |
| `primerApellido` | string o null | Devuelto |  |
| `segundoApellido` | string o null | Devuelto |  |
| `documentoIdentidad` | string o null | Devuelto |  |
| `tipoPasajero` | string o null | Devuelto |  |
| `codigoAsiento` | string o null | Devuelto |  |
| `precioCongelado` | number (double) | Devuelto |  |
| `estado` | string o null | Devuelto |  |

### DTO PrecioClaseAdminRequest

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `categoriaId` | integer (int32) | Sí | mínimo: 1; máximo: 2147483647 |
| `precio` | number (double) | Sí | mínimo: 0; máximo: 99999999.99 |

### DTO ProblemDetails

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `type` | string o null | Devuelto |  |
| `title` | string o null | Devuelto |  |
| `status` | integer (int32) o null | Devuelto |  |
| `detail` | string o null | Devuelto |  |
| `instance` | string o null | Devuelto |  |

### DTO RegistroUsuarioRequest

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `nombre` | string | Sí | longitud máxima: 100 |
| `apellido` | string | Sí | longitud máxima: 100 |
| `email` | string (email) | Sí | longitud máxima: 150 |
| `password` | string | Sí | longitud mínima: 8; longitud máxima: 128 |

### DTO ReservaResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `reservaId` | integer (int32) | Devuelto |  |
| `codigoReserva` | string o null | Devuelto |  |
| `vueloId` | integer (int32) | Devuelto |  |
| `numeroVuelo` | string o null | Devuelto |  |
| `fechaReserva` | string (date-time) | Devuelto |  |
| `montoTotal` | number (double) | Devuelto |  |
| `moneda` | string o null | Devuelto |  |
| `estado` | string o null | Devuelto |  |
| `rowVersion` | string o null | Devuelto |  |
| `detalles` | [DetalleReservaResponse](#dto-detallereservaresponse)[] o null | Devuelto |  |
| `historial` | [HistorialReservaResponse](#dto-historialreservaresponse)[] o null | Devuelto |  |
| `fechaExpiracion` | string (date-time) o null | Devuelto |  |
| `anteriorAlFlujoTemporal` | boolean | Devuelto |  |
| `compra` | [CompraSimuladaResponse](#dto-comprasimuladaresponse) o null | Devuelto |  |
| `subtotal` | number (double) o null | Devuelto |  |
| `porcentajeIva` | number (double) o null | Devuelto |  |
| `montoIva` | number (double) o null | Devuelto |  |

### DTO ReservaResumenResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `reservaId` | integer (int32) | Devuelto |  |
| `codigoReserva` | string o null | Devuelto |  |
| `vueloId` | integer (int32) | Devuelto |  |
| `numeroVuelo` | string o null | Devuelto |  |
| `fechaReserva` | string (date-time) | Devuelto |  |
| `montoTotal` | number (double) | Devuelto |  |
| `moneda` | string o null | Devuelto |  |
| `estado` | string o null | Devuelto |  |
| `rowVersion` | string o null | Devuelto |  |
| `fechaExpiracion` | string (date-time) o null | Devuelto |  |
| `anteriorAlFlujoTemporal` | boolean | Devuelto |  |
| `subtotal` | number (double) o null | Devuelto |  |
| `porcentajeIva` | number (double) o null | Devuelto |  |
| `montoIva` | number (double) o null | Devuelto |  |

### DTO ReservaVueloAdminResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `reservaId` | integer (int32) | Devuelto |  |
| `codigoReserva` | string o null | Devuelto |  |
| `estado` | string o null | Devuelto |  |
| `montoTotal` | number (double) | Devuelto |  |
| `fechaReserva` | string (date-time) | Devuelto |  |
| `usuarioId` | integer (int32) | Devuelto |  |
| `titularNombre` | string o null | Devuelto |  |
| `titularEmail` | string o null | Devuelto |  |
| `pasajeros` | [PasajeroVueloAdminResponse](#dto-pasajerovueloadminresponse)[] o null | Devuelto |  |
| `subtotal` | number (double) o null | Devuelto |  |
| `porcentajeIva` | number (double) o null | Devuelto |  |
| `montoIva` | number (double) o null | Devuelto |  |

### DTO ResumenVentasResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `totalVentas` | number (double) | Devuelto |  |
| `cantidadCompras` | integer (int64) | Devuelto |  |
| `moneda` | string o null | Devuelto |  |
| `proveedor` | string o null | Devuelto |  |
| `entorno` | string o null | Devuelto |  |
| `esSandbox` | boolean | Devuelto |  |
| `desde` | string (date) o null | Devuelto |  |
| `hasta` | string (date) o null | Devuelto |  |

### DTO UsuarioResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `usuarioId` | integer (int32) | Devuelto |  |
| `nombre` | string o null | Devuelto |  |
| `apellido` | string o null | Devuelto |  |
| `email` | string o null | Devuelto |  |

### DTO ValidationProblemDetails

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `type` | string o null | Devuelto |  |
| `title` | string o null | Devuelto |  |
| `status` | integer (int32) o null | Devuelto |  |
| `detail` | string o null | Devuelto |  |
| `instance` | string o null | Devuelto |  |
| `errors` | object o null | Devuelto |  |

### DTO VueloAdminResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `vuelo` | [VueloResponse](#dto-vueloresponse) | Devuelto |  |
| `version` | string o null | Devuelto |  |
| `cantidadAsientos` | integer (int32) | Devuelto |  |

### DTO VueloResponse

| Campo JSON | Tipo | Presencia | Restricciones estáticas |
|---|---|---|---|
| `vueloId` | integer (int32) | Devuelto |  |
| `numeroVuelo` | string o null | Devuelto |  |
| `origen` | [AeropuertoVueloResponse](#dto-aeropuertovueloresponse) | Devuelto |  |
| `destino` | [AeropuertoVueloResponse](#dto-aeropuertovueloresponse) | Devuelto |  |
| `avion` | [AvionVueloResponse](#dto-avionvueloresponse) | Devuelto |  |
| `salidaProgramada` | string (date-time) | Devuelto |  |
| `llegadaProgramada` | string (date-time) | Devuelto |  |
| `salidaReal` | string (date-time) o null | Devuelto |  |
| `llegadaReal` | string (date-time) o null | Devuelto |  |
| `estado` | string o null | Devuelto |  |
| `permiteReservas` | boolean | Devuelto |  |

## 11. Validación de esta entrega

Se verificaron 43 operaciones y 57 contratos contra el documento Swagger generado por la configuración real de Program.cs, sin conectar a SQL Server. Los Controllers y servicios se revisaron para documentar reglas no expresadas completamente por el esquema. No se instalaron dependencias ni se modificó la base. Este archivo no sustituye las pruebas de integración con datos y tokens reales.
