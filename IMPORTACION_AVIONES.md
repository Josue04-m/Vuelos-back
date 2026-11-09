# Configuración: cargar aviones desde CSV

Solo el administrador puede cargar aviones. La importación es la única vía de alta: no existe POST /api/admin/aviones. Solo se conservan listado y detalle, además de las importaciones. PUT /api/admin/aviones/{id} y POST /api/admin/aviones/{id}/desactivacion fueron retirados. Flutter debe usar **Configuración → Cargar aviones**.

## Archivo

Exportar desde Excel como **CSV UTF-8**, separado por `;`, con estos nueve encabezados (se permite cambiar su orden):

```csv
modelo;codigo_iata;codigo_icao;matricula;capacidad_total;cantidad_primera_clase;cantidad_segunda_clase;cantidad_tercera_clase;letras_por_fila
Modelo de prueba;320;A320;EC-DEMO1;6;1;2;3;ABC
```

- Un archivo de hasta **1 MiB**, **500 aviones** y **100000 asientos** en total. El cuerpo multipart admite hasta 2 MiB incluyendo metadatos.
- Cada fila es una aeronave distinta, identificada por su matrícula. Modelo obligatorio de hasta 100 caracteres; matrícula de hasta 50, sin saltos de línea. Las matrículas se recortan y convierten a mayúsculas.
- Capacidad entera de 1 a 1000. Cantidades por clase enteras de 0 a 1000 cuya suma coincida exactamente con la capacidad. Se omiten los grupos con cantidad cero al crear asientos.
- Letras A-Z sin repetir, hasta 26; se normalizan a mayúsculas. Vacío genera números consecutivos. `ABC` genera 1A, 1B, 1C, 2A...; la última fila puede ser parcial.
- IATA admite hasta 3 caracteres e ICAO hasta 4; ambos pueden estar vacíos. Se guardan en codigo_iata/codigo_icao; no son únicos porque varios aviones pueden compartir modelo. Vacíos y \\N se guardan como NULL.
- Se aceptan BOM UTF-8, CRLF/LF y campos entrecomillados que contengan `;` o comillas escapadas. No se admite `.xlsx`, encabezados repetidos/desconocidos ni CSV con otra cantidad de columnas.
- Las capacidades se validan estructuralmente, sin verificar la configuración aeronáutica real del modelo. No se asignan precios; corresponden al vuelo.

Los asientos físicos se generan exclusivamente durante la importación; solo se exponen consultas GET de asientos físicos (incluido el filtro avionId). No hay alta, edición ni desactivación manual. La disponibilidad y los precios se gestionan en asientos del vuelo.

## Solicitud común

Ambos endpoints reciben **multipart/form-data**, con token Bearer de administrador:

| Campo | Tipo | Obligatorio |
|---|---|---|
| `archivo` | Archivo CSV | Sí |
| `primeraClaseId` | Entero positivo | Sí |
| `segundaClaseId` | Entero positivo | Sí |
| `terceraClaseId` | Entero positivo | Sí |

Categorías es un catálogo solo de lectura: sin POST, PUT ni desactivación. Primera, Segunda y Tercera Clase deben existir previamente; esta implementación no inserta ni modifica esos datos. Elegir los IDs desde `GET /api/admin/categorias-asiento`; los tres deben ser distintos, existentes y activos. Los nombres de categorías no se infieren del archivo. No enviar JSON ni una ruta local del servidor. En Flutter, dejar que el cliente multipart genere el boundary del Content-Type.

## Vista previa sin escrituras

`POST /api/admin/aviones/importacion/validar`

Devuelve HTTP 200 con `ImportacionAvionesResponse`. Revisar `estado`, `puedeImportar`, `errores` y `filas`; HTTP 200 no significa que se hayan importado datos.

| Estado | Acción de interfaz |
|---|---|
| `valido` | Mostrar filas, número de aviones/asientos y botón Confirmar |
| `errores_validacion` | Mostrar errores por fila/campo; deshabilitar Confirmar |
| `ya_importados` | Mostrar el mensaje y deshabilitar Confirmar; cero escrituras |
| `conflicto_matriculas` | Mostrar matrículas existentes; retirar esas filas del archivo y validar nuevamente |

Un archivo mal formado, excesivo, de extensión incorrecta o con encabezados inválidos devuelve 400 ProblemDetails con código `archivo_aviones_invalido`. Una carga que exceda el límite HTTP puede devolver 413.

## Confirmación atómica

`POST /api/admin/aviones/importacion`

Reenviar **el mismo archivo y los mismos IDs** revisados en la vista previa. Si el administrador cambia alguno, invalidar la vista previa de la interfaz y validar otra vez. El backend no confía en una validación anterior: repite controles bajo bloqueo administrativo antes de escribir.

| HTTP | Resultado |
|---|---|
| 200 `importado` | Todos los aviones y sus asientos se guardaron |
| 200 `ya_importados` | Todas las matrículas ya existen, incluso si están inactivas; no se creó nada |
| 400 `errores_validacion` | Datos o categorías inválidos; no se creó nada |
| 409 `conflicto_matriculas` | Algunas matrículas existen y otras son nuevas; no se creó nada |

Mensaje para carga repetida: **«Estos aviones ya se importaron o sus matrículas ya están registradas. No se creó ningún registro.»**

La detección usa matrículas persistidas, no el nombre del archivo ni una tabla de archivos importados. Renombrar el CSV no evita el control. Si las mismas matrículas fueron creadas manualmente, también se informa que ya están registradas. Cambiar otros datos de una matrícula existente nunca los sobrescribe. Dos aviones del mismo modelo con matrículas distintas son aeronaves distintas y pueden cargarse.

Si se mezclan matrículas nuevas y existentes, no se omiten filas silenciosamente ni se aplica una carga parcial. Hay que retirar las existentes y volver a validar. Una excepción, conflicto o cancelación antes del commit revierte **toda** la transacción. La restricción única original de matrícula permanece como protección adicional; todos los cambios administrativos usan el mismo bloqueo de aplicación de SQL Server.

## Contrato de respuesta

`ImportacionAvionesResponse` contiene:

- `estado`, `mensaje`, `puedeImportar` (true solo para vista previa válida).
- `totalFilas`, `totalAsientos`: cantidades planificadas del archivo; si existen errores, no asumir que son una carga ejecutada.
- `avionesImportados`, `asientosImportados`: cero salvo una confirmación terminada con estado `importado`.
- `filas`: fila física de inicio, modelo, matrícula, capacidad, cantidades de las tres clases, letras, códigos IATA/ICAO, `matriculaRegistrada`, `avionId` nullable y `datosOrigenJson` con los nueve valores originales del CSV.
- `errores`: elementos con `fila`, `campo`, `mensaje`. La fila 1 se usa para encabezados y configuración de clases.
- `advertencias`: capacidades no certificadas según el modelo real.

Las otras respuestas de infraestructura/concurrencia pueden ser ProblemDetails; no asumir que todo 400/409 tiene el mismo cuerpo. Sin token devuelve 401 y con rol cliente 403. Ante pérdida de respuesta durante confirmación, reenviar el mismo archivo permite consultar/revalidar las matrículas: si ya se guardaron, no se vuelven a crear. Deshabilitar doble pulsación en Flutter.

## Prueba en Swagger

1. Iniciar sesión como administrador y usar Authorize.
2. Obtener los tres IDs de categorías activas.
3. En **05. Aviones (admin)**, abrir `POST /api/admin/aviones/importacion/validar`; seleccionar el CSV e ingresar los IDs.
4. Si `puedeImportar=true`, abrir `POST /api/admin/aviones/importacion` y enviar el mismo archivo y selección.
5. Repetir la confirmación: debe devolver `ya_importados`, cero importados y el mensaje correspondiente.
6. Consultar aviones y `GET /api/admin/asientos-fisicos?avionId=ID` para comprobar los asientos generados.

## Persistencia de todos los campos y migración manual

La ampliación `AvionesDatosCompletos` incorpora siete columnas nullable en aviones:

| CSV | Columna SQL | Tipo |
|---|---|---|
| codigo_iata | codigo_iata | nvarchar(3) |
| codigo_icao | codigo_icao | nvarchar(4) |
| cantidad_primera_clase | cantidad_primera_clase | int |
| cantidad_segunda_clase | cantidad_segunda_clase | int |
| cantidad_tercera_clase | cantidad_tercera_clase | int |
| letras_por_fila | letras_por_fila | nvarchar(26) |
| Todos los nueve valores originales | datos_origen_json | nvarchar(max) |

Modelo, matrícula y capacidad ya se guardaban en aviones. Modelo pasa de varchar(100) a nvarchar(100), conservando su longitud. El JSON conserva sin recortar los valores de las nueve columnas, incluidos espacios, mayúsculas/minúsculas y la representación original de las cantidades. Los campos operativos se normalizan según las reglas anteriores. Se agregan CHECK para JSON válido y distribución no negativa cuya suma coincida con capacidad; las tres cantidades NULL permiten conservar registros anteriores.

Los asientos físicos se siguen generando, con sus categorías y códigos, en la misma transacción. Antes de confirmar se comprueba que la base conserve todos los datos del avión; un fallo revierte toda la carga. No se restauran endpoints de alta, edición ni desactivación. IATA/ICAO no son claves únicas; la matrícula sigue siendo la identidad para evitar duplicados.

GET listado/detalle de aviones incorpora `metadatos` con codigoIata, codigoIcao, cantidadPrimeraClase, cantidadSegundaClase, cantidadTerceraClase, letrasPorFila y datosOrigenJson. Los campos nuevos pueden ser NULL en registros anteriores; no se deducen ni rellenan automáticamente. Reimportar matrículas existentes sigue devolviendo ya_importados sin sobrescribirlas. Completar metadatos de aviones ya cargados requiere un proceso separado y explícito; no eliminar aviones ni sus asientos para resolverlo.

Detener la API, revisar la migración y aplicar manualmente desde el proyecto:

```powershell
dotnet ef database update AvionesDatosCompletos --project .\Vuelos_Backend.csproj
```

El comando aplica en orden las migraciones pendientes, incluida AeropuertosDatosCompletos si todavía no se aplicó. Reiniciar la API después: las consultas del modelo ya usan las nuevas columnas. El Down rechaza descartar metadatos o caracteres Unicode que se perderían al regresar a varchar.

No se ejecutó la migración ni se importaron datos a la base durante el desarrollo. No hay dependencias nuevas. Las pruebas usan repositorios en memoria y HTTP local: persistencia y concurrencia entre procesos en SQL Server quedan para la prueba de integración del equipo. Ejemplo de petición: [ImportacionAviones.http](ImportacionAviones.http).
