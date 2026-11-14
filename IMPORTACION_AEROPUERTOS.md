# Configuración → Cargar aeropuertos

La importación reemplaza el alta, edición y desactivación manuales de aeropuertos. Se mantienen `GET /api/admin/aeropuertos` y `GET /api/admin/aeropuertos/{id}`. No se elimina ni modifica ningún aeropuerto existente; vuelos e históricos conservan sus referencias.

## Archivo revisado

`docs/airports.csv` contiene 86343 registros, separados por **coma**, con encabezados originales. Incluye aeropuertos, instalaciones cerradas, helipuertos y otros tipos. No usa el formato con punto y coma del archivo de aviones.

Las columnas necesarias son `type`, `name`, `municipality`, `iso_country`, `iata_code` y `scheduled_service`. Se admiten y guardan todas las columnas originales; los metadatos ausentes en archivos reducidos se conservan como NULL. Se aceptan UTF-8 con/sin BOM, campos entrecomillados y comas dentro de textos. No se admite `.xlsx`, encabezados duplicados ni CSV con filas de distinta cantidad de columnas.

| Origen CSV | Campo guardado | Regla |
|---|---|---|
| `iata_code` | `codigo_iata` | Tres letras A-Z, únicas; se normalizan a mayúsculas |
| `name` | `nombre` | Obligatorio, hasta 150 caracteres |
| `municipality` | `ciudad` | Opcional: vacío, espacios o `\N` se guardan como NULL; si hay texto, hasta 100 caracteres sin controles |
| `iso_country` | `pais` | Se guarda el código ISO de dos letras, por ejemplo EC; no el nombre completo del país |

También se guardan estos campos adicionales en columnas propias y se devuelven dentro de `metadatos` en las consultas administrativas y la vista previa:

| CSV | Columna DB | Tipo |
|---|---|---|
| `id` | `id_origen` | bigint nullable; no reemplaza aeropuerto_id |
| `ident` | `identificador` | nvarchar(50) |
| `type` | `tipo` | nvarchar(32) |
| `latitude_deg` | `latitud` | float nullable, -90 a 90 |
| `longitude_deg` | `longitud` | float nullable, -180 a 180 |
| `elevation_ft` | `elevacion_pies` | float nullable, pies; admite valores negativos |
| `continent` | `continente` | nvarchar(2) |
| `iso_region` | `region_iso` | nvarchar(20) |
| `scheduled_service` | `servicio_regular` | bit nullable: yes=true, no=false |
| `icao_code` | `codigo_icao` | nvarchar(10) |
| `gps_code` | `codigo_gps` | nvarchar(20) |
| `local_code` | `codigo_local` | nvarchar(20) |
| `home_link` | `sitio_web` | nvarchar(max) |
| `wikipedia_link` | `wikipedia` | nvarchar(max) |
| `keywords` | `palabras_clave` | nvarchar(max) |

`datos_origen_json` (nvarchar(max)) conserva además los valores originales como strings de las 19 columnas, sin recortar espacios y sin perder la precisión textual de números. Incluye columnas adicionales si aparecen en un archivo futuro. Los campos tipados normalizan vacíos y \N a NULL; el JSON conserva esos valores originales. Latitud/longitud tipadas son double/float de SQL Server; si se necesita la representación decimal exacta del CSV, está en el JSON original. No se hacen solicitudes a los enlaces: son datos del archivo, no URLs de confianza para abrir automáticamente.

Los filtros de importación siguen vigentes: se guarda todo el contenido de cada fila seleccionada, no todos los tipos de instalaciones del archivo. Los IDs internos y los códigos IATA existentes se conservan. Una migración no rellena registros anteriores; reimportar IATA ya registrados sigue devolviendo `ya_importados`, sin completar ni sobrescribir datos. Para completar metadatos de aeropuertos ya cargados deberá prepararse un proceso explícito separado, conservando vuelos e historial; no eliminar esos aeropuertos.

## Endpoints y formulario

Solo rol **admin**, con `Authorization: Bearer TOKEN`. Ambos endpoints aceptan **multipart/form-data**:

- `POST /api/admin/aeropuertos/importacion/validar`: vista previa, sin escrituras.
- `POST /api/admin/aeropuertos/importacion`: confirmación de toda la selección, revalidada dentro de la transacción.

| Campo | Valor inicial | Significado |
|---|---|---|
| `archivo` | Obligatorio | CSV original desde el equipo del administrador |
| `soloServicioRegular` | `true` | Seleccionar solo `scheduled_service=yes` |
| `paisIso` | Vacío | Opcional: dos letras, por ejemplo EC; vacío incluye todos los países |
| `omitirIncompletos` | `false` | Autorizar expresamente omitir filas con campos incompletos/incorrectos |
| `pagina` | 1 | Página de la vista previa/respuesta |
| `tamanoPagina` | 100 | De 1 a 200 |

La selección siempre excluye tipos distintos de `small_airport`, `medium_airport`, `large_airport`, y filas sin IATA. El resumen informa esas omisiones. No se usa ICAO/GPS como sustituto de un código IATA faltante.

La ciudad vacía se admite y se guarda como NULL sin activar `omitirIncompletos`. País incorrecto, nombre vacío o textos demasiado largos siguen bloqueando la confirmación; `omitirIncompletos=true` permite omitir esas filas con conteo explícito. Los códigos IATA repetidos entre filas válidas y las filas mal formadas siguen siendo errores bloqueantes.

Límites: archivo de hasta **20 MiB**, cuerpo multipart hasta **24 MiB**, **150000 registros de origen** y **20000 aeropuertos importables** por solicitud. La confirmación procesa todos los seleccionados, **no solo la página visible**.

## Resultado y mensajes

La respuesta `ImportacionAeropuertosResponse` incluye:

- `estado`, `mensaje`, `puedeImportar` (true solo para una vista previa válida).
- `totalRegistros`, `totalImportables`, `totalOmitidos`, `totalErrores`, `erroresTruncados`.
- `totalExistentes`: códigos ya registrados antes de esta carga; `aeropuertosImportados`: cero salvo confirmación exitosa.
- `pagina`, `tamanoPagina`, `totalPaginas`, `filas`: vista paginada con línea física, IATA, nombre, ciudad, país, `codigoRegistrado` y `aeropuertoId` nullable.
- `errores`: hasta 200 errores con `fila`, `campo`, `mensaje`. Los errores son globales, no corresponden solo a la página visible; el total puede ser mayor. La fila 1 representa encabezados/configuración global.
- `resumenOmisiones`: conteos por `tipo_no_admitido`, `sin_servicio_regular`, `fuera_del_pais`, `sin_iata` y `datos_incompletos_o_invalidos`. Cada fila omitida se cuenta una vez, en el primer motivo aplicable.

| Estado | HTTP vista previa | HTTP confirmación | Qué hacer |
|---|---|---|---|
| `valido` | 200 | — | Revisar selección y omisiones; habilitar Confirmar |
| `importado` | — | 200 | Mostrar cantidad importada y refrescar catálogo |
| `ya_importados` | 200 | 200 | Informar que ya existen; no crear registros ni habilitar confirmación |
| `conflicto_codigos` | 200 | 409 | Retirar códigos existentes del archivo; no se importa parcialmente |
| `errores_validacion` | 200 | 400 | Corregir filas o autorizar omisiones cuando corresponda |

Mensaje de repetición: **«Estos aeropuertos ya se importaron o sus códigos IATA ya están registrados. No se creó ningún registro.»** Se detecta por código IATA, no por nombre del archivo; incluye registros que se crearon manualmente antes de retirar el CRUD. No se sobrescriben datos ni se reactivan registros.

Errores de archivo/formulario devuelven 400 ProblemDetails con `archivo_aeropuertos_invalido`; cargas excesivas pueden devolver 413. Conflictos SQL pueden devolver 409 ProblemDetails. No asumir un único cuerpo para todos los errores. Sin sesión responde 401; con cliente, 403.

## Flujo de Swagger y Flutter

1. Entrar como administrador. Obtener el archivo CSV guardado.
2. En **03. Aeropuertos (admin)**, abrir `/importacion/validar`, cargar el archivo y revisar filtros. Puede usarse `paisIso=EC` para empezar con Ecuador.
3. Revisar los totales, omisiones, errores y páginas. No habilitar Confirmar solo porque HTTP sea 200: exigir `estado=valido` y `puedeImportar=true`.
4. Si se elige omitir incompletos, hacerlo con una casilla explícita y validar nuevamente. Mostrar cuántas filas se excluirán.
5. Confirmar con el mismo archivo y filtros revisados. Cambiar archivo o filtros invalida la vista previa anterior. La paginación solo cambia la vista, no el alcance de la carga.
6. Mostrar `importado` o el aviso de códigos ya registrados. Deshabilitar doble pulsación. Si se pierde la respuesta, revalidar/reintentar el mismo archivo; no se duplican códigos existentes.

Para el archivo original de 86343 filas, se admiten ciudades NULL: con servicio regular hay **4008 importables**; con `soloServicioRegular=false` y sin filtro de país hay **8799 importables**, sin omitir incompletos. Los demás no tienen IATA o son tipos no admitidos. Los conteos describen filas válidas, no inserciones nuevas: la vista previa también comprueba los códigos existentes y el importador actual aún bloquea mezclas de nuevos y registrados.

### Ciudad opcional en Supabase

Antes de importar ciudades NULL, aplicar manualmente `20261113000000_CiudadAeropuertoOpcional` o el SQL incremental idempotente `Data/Scripts/Supabase_CiudadAeropuertoOpcional.sql` desde Supabase SQL Editor. Usar solo una vía. La migración solo retira NOT NULL de `pna.aeropuertos.ciudad`; no modifica los valores existentes, otras columnas, índices ni relaciones. No se ejecutó automáticamente.

Modelo, DTOs de catálogos y origen/destino de vuelos admiten `ciudad: null`. Flutter debe mostrar el nombre del aeropuerto cuando falte ciudad. La reversión de la migración se rechaza si todavía hay ciudades NULL para evitar inventar valores.

## Persistencia y codificación

Los registros se guardan por lotes dentro de **una sola transacción**, bajo el bloqueo administrativo compartido. Un fallo en cualquier lote revierte todo; el índice único IATA sigue siendo la protección final. No se modifican vuelos, reservas o catálogos existentes.

La migración `AeropuertosDatosCompletos` cambia nombre, ciudad y país de varchar a nvarchar con las mismas longitudes, y añade 16 columnas nullable (15 campos adicionales más JSON). Los registros anteriores conservan sus campos actuales y tienen nuevos campos NULL. No se borra ni recrea la tabla; PK, IATA único, RowVersion y relaciones permanecen. Se agregan CHECK para rangos de coordenadas y JSON válido.

Antes del commit, el importador lee cada lote y comprueba los datos completos. Si detecta alteraciones, revierte toda la carga. El Down de la migración rechaza eliminar metadatos existentes o convertir caracteres Unicode que se perderían.

## Aplicación manual de la migración

Detener la API, revisar la migración y ejecutar desde la carpeta del proyecto:

```powershell
dotnet ef database update AeropuertosDatosCompletos --project .\Vuelos_Backend.csproj
```

Después reiniciar la API y validar el CSV antes de confirmar. Es necesario aplicar la migración antes de ejecutar esta versión: las consultas del modelo ya incluyen las nuevas columnas.

No se ejecutó la migración ni se importó el archivo a SQL Server durante el desarrollo. No hay dependencias nuevas. Se probaron modelo/snapshot, SQL generado sin ejecutarlo, validaciones y servicio/HTTP con repositorios en memoria; la persistencia y concurrencia reales quedan para integración. Ejemplos: [ImportacionAeropuertos.http](ImportacionAeropuertos.http).
