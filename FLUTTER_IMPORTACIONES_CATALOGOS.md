# Requerimiento Flutter: importaciones y catálogos de consulta

## Objetivo y alcance

Adaptar la aplicación Flutter al backend actual: aviones y aeropuertos se cargan mediante archivos CSV; categorías de asiento y asientos físicos se consultan. Retirar los formularios y acciones CRUD anteriores de estos cuatro catálogos. Conservar la navegación, estilo visual, autenticación y flujos de vuelos, reservas y PayPal ya implementados.

Este requerimiento corresponde al frontend. No implementar otro backend, modificar la base desde Flutter ni ejecutar migraciones desde la aplicación. Antes de probar, backend debe haber aplicado `AeropuertosDatosCompletos` y `AvionesDatosCompletos` y reiniciado la API.

Las importaciones y consultas administrativas requieren rol admin y token Bearer válido. Ocultar sus pantallas y acciones a clientes; mantener los controles del servidor como autoridad. El token actual es opaco, no JWT: no decodificarlo para inventar un rol ni asumir un endpoint `/me` nuevo.

## Pantallas y widgets

| Pantalla | Adaptación requerida |
|---|---|
| Configuración administrativa | Agregar accesos **Cargar aviones** y **Cargar aeropuertos** |
| Cargar aviones | Selector CSV, tres selectores de categorías, botón Validar, resumen, vista previa, errores por fila y botón Confirmar |
| Cargar aeropuertos | Selector CSV, filtro de país, interruptor de servicio regular, casilla explícita de omisión de incompletos, vista previa paginada, resumen de omisiones/errores y Confirmar |
| Aviones: listado/detalle | Solo lectura. Mostrar modelo, matrícula, capacidad, códigos y distribución por clases cuando estén disponibles |
| Aeropuertos: listado/detalle | Solo lectura. Mostrar IATA, nombre, ciudad, país y los nuevos metadatos en el detalle |
| Clases de asiento | Catálogo solo de consulta: Primera, Segunda y Tercera Clase. Seleccionar IDs reales para importar aviones o configurar vuelos |
| Asientos físicos | Solo lectura; permitir consultar por avión con el filtro avionId. No crear asientos uno por uno |

Usar componentes de formulario, tarjetas/listas, indicadores de carga y diálogos coherentes con el diseño existente. En móvil, preferir listas compactas y detalles expandibles a tablas con muchas columnas desbordadas. No añadir un mapa como requisito de esta entrega: basta mostrar coordenadas; la visualización geográfica puede desarrollarse después.

## Acciones retiradas

Eliminar botones Crear/Editar/Desactivar y sus llamadas HTTP en:

- `/api/admin/aviones`
- `/api/admin/aeropuertos`
- `/api/admin/categorias-asiento`
- `/api/admin/asientos-fisicos`

Solo permanecen `GET` listado y `GET /{id}` para esos recursos, además de las importaciones de aviones y aeropuertos. No intentar sustituir las acciones retiradas llamando otros endpoints. Las consultas administrativas siguen siendo exclusivas de admin.

La configuración de precios y disponibilidad de **asientos del vuelo** se conserva en sus rutas actuales. Un asiento físico describe el avión; un asiento de vuelo representa su disponibilidad/precio en una salida concreta. No confundirlos ni retirar la gestión de vuelos.

## Cliente HTTP y archivos

Mantener la URL base configurable y el cliente HTTP existente. Las rutas siguientes incluyen `/api`: no duplicar el prefijo. Enviar `Authorization: Bearer TOKEN` también en las peticiones multipart; nunca en la URL ni en el archivo.

Las importaciones reciben **multipart/form-data**, no JSON, no Base64 ni una ruta del servidor. Seleccionar el archivo en el dispositivo/navegador y adjuntar su contenido al campo `archivo`. Dejar que la librería genere el boundary. No depender de una ruta local en Flutter web, donde el selector puede entregar bytes sin ruta.

Aceptar CSV UTF-8; no `.xlsx`. Validar extensión/tamaño en la interfaz para orientar al usuario, sin sustituir la validación del backend. No convertir, modificar ni eliminar filas silenciosamente. Para cambiar un archivo, pedir al administrador corregirlo o seleccionar nuevamente una versión revisada.

Reutilizar las dependencias existentes para selección de archivos y multipart; si falta alguna, el equipo debe decidirla y su versión. No incorporar credenciales PayPal ni acceso SQL por esta funcionalidad.

## Importación de aviones

### Endpoints

| Operación | Endpoint |
|---|---|
| Validar sin guardar | `POST /api/admin/aviones/importacion/validar` |
| Confirmar toda la carga | `POST /api/admin/aviones/importacion` |

Ambos reciben el mismo formulario:

| Campo | Tipo |
|---|---|
| `archivo` | CSV UTF-8 separado por `;` |
| `primeraClaseId` | ID positivo de categoría activa |
| `segundaClaseId` | ID positivo de categoría activa |
| `terceraClaseId` | ID positivo de categoría activa |

Cargar categorías con `GET /api/admin/categorias-asiento`; respetar su paginación si corresponde y seleccionar tres IDs distintos. Mostrar nombre en el selector y enviar ID; no fijar valores 1/2/3 ni crear categorías si no existen. Si faltan clases activas, informar al equipo backend y deshabilitar la confirmación.

Límites del archivo: **1 MiB, 500 aviones y 100000 asientos** en total. Cada avión admite entre 1 y 1000 asientos. Las cantidades por clase deben sumar la capacidad; se permiten clases con cero asientos. IATA/ICAO pueden repetirse entre modelos/aeronaves; la matrícula identifica cada avión y no debe duplicarse.

### Widgets y resultado

Mostrar nombre/tamaño del archivo, categorías elegidas y botón **Validar archivo**. Durante la validación bloquear el envío repetido. Después mostrar:

- `totalFilas` y `totalAsientos`.
- Vista de `filas` con matrícula, modelo, capacidad, cantidades por clase y códigos IATA/ICAO.
- `errores` con fila, campo y mensaje; señalar `matriculaRegistrada=true` sin tratarlo como un alta nueva.
- `advertencias`, incluyendo que el backend no certifica la capacidad real del modelo.

No hay paginación del resultado de aviones; la vista previa contiene hasta las 500 filas admitidas. Usar una lista con construcción bajo demanda para evitar crear todos los widgets al mismo tiempo.

Al confirmar, reenviar el archivo y la misma selección de clases. El servidor crea los aviones y sus asientos físicos en una transacción: Flutter no debe realizar POST de asientos físicos ni calcular precios de vuelo como parte de esta carga.

Respuesta `ImportacionAvionesResponse`: `estado`, `mensaje`, `puedeImportar`, `totalFilas`, `totalAsientos`, `avionesImportados`, `asientosImportados`, `filas`, `errores`, `advertencias`.

Cada fila incluye `fila`, `modelo`, `matricula`, `capacidadTotal`, `cantidadPrimeraClase`, `cantidadSegundaClase`, `cantidadTerceraClase`, `letrasPorFila`, `codigoIata`, `codigoIcao`, `matriculaRegistrada`, `avionId` nullable y `datosOrigenJson` nullable. El JSON de origen es texto para trazabilidad: no hace falta mostrarlo como parte del flujo de usuario.

## Importación de aeropuertos

### Endpoints y filtros

| Operación | Endpoint |
|---|---|
| Validar sin guardar | `POST /api/admin/aeropuertos/importacion/validar` |
| Confirmar toda la selección | `POST /api/admin/aeropuertos/importacion` |

Ambos reciben:

| Campo | Tipo / valor inicial |
|---|---|
| `archivo` | airports.csv original, UTF-8 separado por **coma** |
| `soloServicioRegular` | bool; true |
| `paisIso` | string opcional de dos letras, por ejemplo EC; omitir/vaciar para todos los países |
| `omitirIncompletos` | bool; false |
| `pagina` | int; 1 |
| `tamanoPagina` | int de 1 a 200; 100 |

Enviar los booleanos como campos del formulario `true`/`false`. No confundir el separador del CSV de aeropuertos con el de aviones. Límites: **20 MiB, 150000 registros de origen y 20000 aeropuertos importables**.

Se seleccionan aeropuertos pequeños/medianos/grandes; instalaciones cerradas, helipuertos y otros tipos se excluyen. Sin IATA también se excluyen. Los filtros y conteos del backend son la autoridad.

La opción **Omitir registros incompletos o inválidos** debe ser una decisión explícita del administrador, desactivada inicialmente. No activarla automáticamente al recibir errores. Mostrar que esos registros no se guardarán. No inventar una ciudad para completar una fila ni sustituir IATA por ICAO/GPS.

### Vista previa y confirmación

Mostrar `totalRegistros`, `totalImportables`, `totalOmitidos`, `totalErrores`, `totalExistentes` y el desglose de `resumenOmisiones`. Los motivos posibles son `tipo_no_admitido`, `sin_servicio_regular`, `fuera_del_pais`, `sin_iata`, `datos_incompletos_o_invalidos`; mapearlos a etiquetas legibles.

`filas` es una vista paginada de la selección válida. Para cambiar de página hay que reenviar el archivo y los mismos filtros a **validar**, ajustando pagina/tamanoPagina; no existe una sesión de importación ni un token de vista previa en el backend. Conservar el archivo/bytes mientras se navegan las páginas. Si se pierde ese contexto al cerrar la pantalla, pedir seleccionar y validar de nuevo.

La confirmación importa **todos los aeropuertos seleccionados del archivo**, no solo la página visible. El diálogo debe indicar el total global que se guardará y el total omitido. No enviar un POST de confirmación por cada página.

Respuesta `ImportacionAeropuertosResponse`: `estado`, `mensaje`, `puedeImportar`, `totalRegistros`, `totalImportables`, `totalOmitidos`, `totalErrores`, `erroresTruncados`, `totalExistentes`, `aeropuertosImportados`, `pagina`, `tamanoPagina`, `totalPaginas`, `filas`, `errores`, `resumenOmisiones`.

Cada fila incluye `fila`, `codigoIata`, `nombre`, `ciudad`, `pais`, `codigoRegistrado`, `aeropuertoId` nullable y `metadatos` nullable. Se muestran hasta 200 errores globales, con `fila`, `campo`, `mensaje`; no corresponden solo a la página visible. Si erroresTruncados=true, informar que la lista no contiene todos los errores. No interpretar totalErrores como número de filas: una fila puede tener varios errores.

La ciudad es opcional: mostrar `ciudad: null` como “Sin ciudad” o utilizar el nombre del aeropuerto. No solicitar activar omisión por faltar ciudad. En el CSV original hay 4008 importables con servicio regular y 8799 sin ese filtro, antes de comprobar existentes. Mostrar siempre los totales reales de la API, sin fijar estas cifras en Flutter. Requiere aplicar manualmente la migración PostgreSQL CiudadAeropuertoOpcional.

## Estados comunes de la importación

**HTTP 200 no garantiza importación ni validez.** Evaluar siempre `estado` y `puedeImportar`.

| Estado | Widget / comportamiento |
|---|---|
| `valido` | Habilitar Confirmar solo con puedeImportar=true y archivo/filtros sin cambios |
| `errores_validacion` | Mostrar lista de errores; deshabilitar Confirmar |
| `ya_importados` | Mostrar mensaje informativo **Ya se importaron / ya están registrados**; deshabilitar Confirmar. No crear nuevas identidades |
| `conflicto_matriculas` | Aviones: algunas matrículas existen. Explicar que no se importó nada y pedir corregir el archivo |
| `conflicto_codigos` | Aeropuertos: algunos IATA existen. Explicar que no se importó nada y pedir corregir el archivo |
| `importado` | Mostrar cantidades realmente guardadas y refrescar el catálogo; deshabilitar el botón de ese envío completado |
| Desconocido | Mostrar mensaje seguro y permitir revisar/actualizar; no asumir éxito |

Cambiar archivo, clases o filtros invalida la validación anterior. Si cambia la opción de omitir incompletos, validar otra vez y mostrar nuevos totales. Cambiar de página en aeropuertos no cambia la selección global, pero la respuesta vigente debe seguir siendo válida antes de confirmar.

No omitir duplicados silenciosamente desde Flutter ni cambiar matrículas/IATA automáticamente. Una coincidencia parcial bloquea toda la transacción; no existen resultados de confirmación parcialmente guardados.

## Adaptación de modelos y widgets de detalle

`AvionAdminResponse` conserva `avionId`, `modelo`, `matricula`, `capacidadTotal`, `activo`, `fechaDesactivacion`, `rowVersion` y agrega `metadatos`:

- `codigoIata`, `codigoIcao`: String nullable.
- `cantidadPrimeraClase`, `cantidadSegundaClase`, `cantidadTerceraClase`: int nullable.
- `letrasPorFila`, `datosOrigenJson`: String nullable.

Mostrar las cantidades como chips/resumen por clase. Diferenciar **0 asientos** de **dato no disponible** (null). Si faltan cantidades en un avión antiguo, no deducir un reparto arbitrario desde capacidadTotal. Los asientos reales pueden consultarse con `GET /api/admin/asientos-fisicos?avionId=ID`; respetar la paginación para obtener el listado completo.

`AeropuertoAdminResponse` conserva sus campos base y agrega `metadatos`:

- `idOrigen`: int nullable; no sustituye aeropuertoId interno.
- `identificador`, `tipo`, `continente`, `regionIso`, `codigoIcao`, `codigoGps`, `codigoLocal`: String nullable.
- `latitud`, `longitud`, `elevacionPies`: double nullable; en Dart admitir JSON num con `toDouble()`, porque un número puede llegar sin decimales.
- `servicioRegular`: bool nullable; mostrar Sí / No / No disponible.
- `sitioWeb`, `wikipedia`, `palabrasClave`, `datosOrigenJson`: String nullable.

Mostrar los campos adicionales en secciones del detalle, sin saturar las filas del listado. La elevación está en **pies**, no metros. No sustituir coordenadas null por 0,0. `pais` de nuevas importaciones es ISO (EC, US...), mientras datos antiguos pueden contener nombres completos: no asumir que todos los registros usan un único formato.

Los enlaces son contenido del CSV, no una fuente de confianza: si se permite abrirlos, admitir únicamente HTTP/HTTPS válidos. No renderizar HTML ni ejecutar contenido del archivo. `datosOrigenJson` es un string JSON con valores originales del CSV; para los widgets habituales usar los campos tipados, no volver a calcularlos desde ese texto.

Hacer que la deserialización tolere `metadatos=null`, campos internos null y propiedades adicionales. Los aviones/aeropuertos anteriores a las migraciones pueden no tener metadatos. Reimportarlos **no los completa**: el servidor responde ya_importados sin sobrescribir. No proponer borrar históricos o inventar una matrícula/IATA nueva para resolverlo.

## Errores, carga y recuperación

- Durante validación/confirmación mostrar progreso y bloquear pulsaciones simultáneas. No modificar el archivo o los filtros mientras se confirma.
- 400 puede contener errores del formulario en ProblemDetails/ValidationProblemDetails o una respuesta de importación con errores por fila.
- 401: pedir login; 403: denegar la pantalla/acción administrativa; 413: indicar que debe reducir el archivo.
- 409 puede contener la respuesta de coincidencias parciales o ProblemDetails por concurrencia. Revalidar antes de volver a confirmar.
- Pérdida de conexión/timeout durante confirmación: el resultado es incierto. Revalidar el mismo archivo antes de afirmar que falló o de lanzar otra carga. Si se guardó, ya_importados evita duplicación.
- Mantener el mensaje y código de error del backend de forma controlada. No registrar token Bearer ni contenido completo del archivo en logs.
- No confiar en un contador de progreso para anunciar éxito: esperar el estado importado. Una recarga de listado no reemplaza la respuesta de confirmación.

## Criterios de aceptación

1. Solo admin ve Configuración y las acciones de carga. Los catálogos tienen consultas sin botones CRUD retirados.
2. CSV de aviones válido: vista previa, confirmación, lectura de metadatos y consulta de asientos generados por backend.
3. CSV de aeropuertos: filtros, resumen de omisiones, ciudades faltantes aceptadas como NULL; la omisión explícita sigue aplicándose a errores reales.
4. Cambiar de página no dispara importaciones parciales ni pierde el archivo/filtros. Confirmar procesa el total global revisado.
5. Reenviar el mismo archivo muestra ya_importados; coincidencias parciales no se presentan como éxito.
6. Formularios admiten carga/vacío/error, doble pulsación, sesión expirada, 403, 413 y timeout sin escrituras duplicadas.
7. Listados y detalles manejan Unicode, códigos repetidos entre modelos de avión, cantidades cero, metadatos null y coordenadas numéricas sin errores.
8. Pruebas en las plataformas que entregue el equipo (móvil/web), incluyendo selección de archivos y diseño sin desbordamientos.
9. Vuelos, reservas, precios por clase y PayPal siguen funcionando con IDs internos existentes y permisos actuales.

Referencias del backend: [API_FRONTEND.md](API_FRONTEND.md), [IMPORTACION_AVIONES.md](IMPORTACION_AVIONES.md), [IMPORTACION_AEROPUERTOS.md](IMPORTACION_AEROPUERTOS.md). El flujo de pago PayPal permanece definido en [FLUTTER_PAYPAL.md](FLUTTER_PAYPAL.md).
