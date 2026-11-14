# Mapa de vuelos para el administrador: contrato y entrega a Flutter

Estado: backend implementado y probado localmente con servicios/repositorios simulados y SQL generado. Pendiente de desplegar esta versión y comprobarla con los registros reales de Supabase y la pantalla Flutter. No se implementó la interfaz en este repositorio.

## 1. Experiencia esperada

El administrador abre “Mapa de vuelos”, consulta el listado, filtra por países y selecciona un vuelo. El mapa mundial sencillo dibuja origen y destino, sus nombres y banderas, una curva orientativa y un icono de avión que indica el sentido origen → destino.

La ubicación del icono es ilustrativa. La API no calcula una posición actual, progreso de vuelo ni una trayectoria operacional. No se debe presentar como seguimiento en tiempo real, incluso cuando el estado del vuelo sea “en vuelo”.

Sugerencia visual: origen verde, destino rojo; las banderas se muestran junto a las etiquetas, no sustituyen los colores de los marcadores. El boceto mundial, los iconos, la curva, el zoom y los controles se implementan en Flutter.

## 2. Conexión y permisos

Base actual: `https://vuelos-back.onrender.com`. Las rutas nuevas y filtros estarán disponibles después del despliegue de esta versión. Para desarrollo local, utilizar la dirección configurada de la API.

Ambas consultas requieren `Authorization: Bearer TOKEN_OPACO_ADMIN`. El token debe pertenecer a un usuario con rol `admin` y seguir vigente. No enviar roles, credenciales de Supabase ni identificadores de usuario para autorizar la consulta.

| Situación | HTTP |
| --- | --- |
| Sin sesión válida | 401 |
| Sesión de cliente sin rol administrador | 403 |
| Parámetros inválidos | 400 con ValidationProblemDetails |
| Vuelo inexistente al pedir recorrido | 404 con ProblemDetails |
| Vuelo existente sin coordenadas suficientes | 200 con puedeMostrarRecorrido=false |
| Listado sin coincidencias o página posterior al final | 200 con vuelos vacío y totales correspondientes |

La pantalla debe ser accesible desde el menú administrativo. El backend aplica el permiso aunque el frontend oculte el menú. Las consultas públicas `/api/vuelos` conservan su contrato y su acceso para visitantes.

## 3. Listar, filtrar y ordenar vuelos

```http
GET /api/admin/vuelos?paisOrigen=EC&paisDestino=CO&ordenarPor=fechaSalida&direccion=desc&pagina=1&tamanoPagina=20
Authorization: Bearer TOKEN_ADMIN
```

El listado conserva la estructura previa `PaginaVuelosResponse`. Devuelve todos los estados que coincidan, sin ocultar cancelados, finalizados, vuelos pasados ni aquellos con `permiteReservas=false`. No filtrar la administración usando ese indicador de compra.

| Parámetro | Tipo | Reglas |
| --- | --- | --- |
| origen | string opcional | IATA de tres letras, por ejemplo UIO; ignora mayúsculas |
| destino | string opcional | IATA de tres letras; debe diferir de origen si se envían ambos |
| paisOrigen | string opcional | ISO de dos letras del aeropuerto de origen, por ejemplo EC; ignora mayúsculas |
| paisDestino | string opcional | ISO de dos letras del aeropuerto de destino, por ejemplo CO; ignora mayúsculas |
| fecha | fecha opcional | Día UTC de salida, YYYY-MM-DD |
| fechaDesde / fechaHasta | fechas opcionales | Enviar ambas, rango inclusivo por día UTC; no combinar con fecha |
| ordenarPor | string | fechaSalida (predeterminado), numeroVuelo, paisOrigen o paisDestino; respetar esta escritura |
| direccion | string | asc (predeterminado) o desc |
| pagina | integer | Desde 1, predeterminado 1 |
| tamanoPagina | integer | De 1 a 100, predeterminado 20 |

Los filtros se combinan con AND. `paisOrigen=EC&paisDestino=EC` busca vuelos nacionales, no vuelos que pasen por Ecuador en cualquiera de los extremos. Si el administrador solo elige un país de origen, enviar únicamente paisOrigen. Al limpiar un filtro, omitirlo; no enviar textos como “Todos”, “null” o “string”. Un ISO sin coincidencias devuelve una lista vacía, no un error de autenticación.

Los países se ordenan por su código ISO, no por el nombre traducido. Los números de vuelo se ordenan como texto. Los empates se resuelven por salida programada e identificador; para fechaSalida, por identificador. Esto permite paginación estable sobre datos que no estén cambiando concurrentemente.

La paginación y los filtros se ejecutan en PostgreSQL; no descargar todos los aeropuertos para construir este módulo. Las fechas filtran la salida programada y se interpretan en UTC.

Ejemplo ilustrativo de respuesta (IDs, fechas y coordenadas deben obtenerse de la API real):

```json
{
  "vuelos": [
    {
      "vueloId": 1,
      "numeroVuelo": "PN100",
      "origen": {
        "aeropuertoId": 1,
        "codigoIata": "UIO",
        "nombre": "Mariscal Sucre",
        "ciudad": "Quito",
        "pais": "EC"
      },
      "destino": {
        "aeropuertoId": 2,
        "codigoIata": "BOG",
        "nombre": "El Dorado",
        "ciudad": "Bogotá",
        "pais": "CO"
      },
      "avion": { "avionId": 1, "modelo": "Prueba", "capacidadTotal": 2 },
      "salidaProgramada": "2030-01-01T12:00:00+00:00",
      "llegadaProgramada": "2030-01-01T14:00:00+00:00",
      "salidaReal": null,
      "llegadaReal": null,
      "estado": "programado",
      "permiteReservas": true
    }
  ],
  "pagina": 1,
  "tamanoPagina": 20,
  "total": 1,
  "totalPaginas": 1
}
```

Al cambiar filtros u orden, reiniciar pagina=1. Al cargar otra página, mantener los filtros. Un listado vacío debe mostrar “No hay vuelos con estos filtros”. Los datos pueden cambiar durante la navegación; no tratar una consulta previa como garantía de disponibilidad.

## 4. Consultar recorrido al seleccionar un vuelo

```http
GET /api/admin/vuelos/1/recorrido
Authorization: Bearer TOKEN_ADMIN
```

Ejemplo ilustrativo:

```json
{
  "vueloId": 1,
  "numeroVuelo": "PN100",
  "estado": "programado",
  "origen": {
    "aeropuertoId": 1,
    "codigoIata": "UIO",
    "nombre": "Mariscal Sucre",
    "ciudad": "Quito",
    "pais": "EC",
    "latitud": -0.1292,
    "longitud": -78.3575,
    "coordenadasDisponibles": true
  },
  "destino": {
    "aeropuertoId": 2,
    "codigoIata": "BOG",
    "nombre": "El Dorado",
    "ciudad": "Bogotá",
    "pais": "CO",
    "latitud": 4.7016,
    "longitud": -74.1469,
    "coordenadasDisponibles": true
  },
  "puedeMostrarRecorrido": true,
  "motivoNoDisponible": null,
  "cruzaAntimeridiano": false,
  "tipoRecorrido": "orientativo_entre_aeropuertos",
  "esSeguimientoEnTiempoReal": false
}
```

| Campo | Uso en Flutter |
| --- | --- |
| vueloId / numeroVuelo / estado | Identificar el vuelo y rotular el mapa, también si está cancelado o finalizado |
| origen / destino | Marcar los extremos en ese sentido; no intercambiarlos al ordenar |
| aeropuertoId / codigoIata / nombre | Identificación y etiqueta de cada aeropuerto |
| ciudad | Nullable: si falta, usar el nombre del aeropuerto |
| pais | ISO de dos letras para bandera y nombre del país mediante el catálogo de Flutter |
| latitud / longitud | double nullable; coordenadas WGS84 en grados decimales |
| coordenadasDisponibles | Indica que el extremo tiene una pareja válida y completa |
| puedeMostrarRecorrido | Dibujar conexión e icono solo si es true |
| motivoNoDisponible | Código para mostrar un mensaje cuando no puede dibujarse la conexión |
| cruzaAntimeridiano | Diferencia absoluta de longitudes mayor de 180°; null cuando el recorrido no está disponible |
| tipoRecorrido | Valor actual orientativo_entre_aeropuertos |
| esSeguimientoEnTiempoReal | Siempre false en esta entrega |

El detalle administrativo previo `GET /api/admin/vuelos/{id}` se conserva: vuelo, version y cantidadAsientos. El mapa se consulta por la nueva ruta /recorrido; no tiene que inferir coordenadas del listado ni modificar contratos públicos.

No se devuelven reservas, documentos de pasajeros, datos de titulares, tokens ni metadatos completos del CSV en el recorrido. Los aeropuertos históricos inactivos vinculados a un vuelo no se descartan.

## 5. Coordenadas ausentes y datos del laboratorio

Si falta una coordenada de un extremo o es inválida, latitud y longitud de ese extremo se devuelven ambas como null y coordenadasDisponibles=false. Se conserva el extremo que sí tiene una pareja válida.

| motivoNoDisponible | Mensaje sugerido |
| --- | --- |
| coordenadas_origen_no_disponibles | El aeropuerto de salida no tiene coordenadas disponibles |
| coordenadas_destino_no_disponibles | El aeropuerto de llegada no tiene coordenadas disponibles |
| coordenadas_origen_y_destino_no_disponibles | Los aeropuertos no tienen coordenadas disponibles |

En estos casos el endpoint responde 200, puedeMostrarRecorrido=false y cruzaAntimeridiano=null. Mostrar el detalle del vuelo y el aviso; se puede mostrar el marcador del extremo válido, sin conexión ni avión sobre una ruta ficticia.

No reemplazar null por 0: (0,0) es una coordenada válida. Se admiten latitudes de -90 a 90 y longitudes de -180 a 180. Los números no finitos o fuera de rango se tratan como coordenadas no disponibles. Una biblioteca que use Mercator puede necesitar límites de presentación cerca de los polos; respetar su documentación sin cambiar los datos del aeropuerto.

Algunas ciudades del catálogo fueron completadas con texto “Localidad ficticia IATA” para este laboratorio. Las etiquetas pueden contenerlo. Las coordenadas se leen de los registros guardados; el backend no las inventa para fabricar un recorrido.

## 6. Dibujar el boceto mundial

Flutter elige el recurso vectorial o biblioteca de mapas. El backend no requiere proveedor de mapas, API key ni dependencia adicional. Para un boceto equirectangular sobre Canvas, las posiciones básicas son:

```text
x = (longitud + 180) / 360 * ancho
y = (90 - latitud) / 180 * alto
```

Estas fórmulas son solo para un mapa equirectangular: si se usa una biblioteca con otra proyección, utilizar su conversión nativa de coordenadas. Dibujar una curva ilustrativa entre los extremos, colocar un icono de avión en un punto intermedio y orientarlo hacia destino. El tamaño del icono o su animación no representa velocidad, altitud ni progreso real.

Si cruzaAntimeridiano=true, no dibujar una línea que atraviese todo el mapa por el lado largo. Utilizar el soporte de envoltura de la biblioteca, o ajustar temporalmente la longitud destino sumando/restando 360° para elegir el tramo corto y dividir/recortar el dibujo en el borde del mapa. Ajustar la vista con ese mismo criterio. Las coordenadas JSON permanecen en [-180,180].

Si ambos puntos coinciden, mostrar marcadores/etiquetas agrupadas y evitar dividir por una distancia cero. Cerca de puntos antipodales puede haber más de una curva ilustrativa válida; no atribuir precisión operacional al dibujo. Las rutas nacionales también se representan, aunque ambas banderas sean iguales.

## 7. Manejo de estado de la pantalla

1. Entrar con sesión de administrador y cargar la primera página.
2. Permitir elegir países de origen y destino; enviar ISO, no nombres traducidos. Flutter puede reutilizar su catálogo de países/banderas, sin consultar cada aeropuerto.
3. Mantener filtros y orden al paginar. No ocultar vuelos no reservables.
4. Al pulsar un vuelo, limpiar el recorrido anterior y mostrar carga mientras se consulta /recorrido.
5. Cancelar o ignorar respuestas de selecciones anteriores si el administrador cambia rápidamente de vuelo.
6. Si el vuelo desapareció, mostrar el 404 y refrescar el listado. Si faltan coordenadas, mantener el vuelo seleccionado y mostrar el motivo.
7. Ante 401, solicitar nueva sesión; ante 403, mostrar acceso restringido. Un fallo de red debe permitir reintentar, sin dejar el recorrido del vuelo anterior rotulado como si fuera el nuevo.

## 8. Pruebas de aceptación

- Administrador consulta lista y recorrido. Visitante recibe 401; cliente recibe 403.
- Sin filtros aparecen todos los estados, incluidas cancelaciones y finalizados.
- Filtrar solo por país de origen, solo por destino y por ambos; probar dos aeropuertos del mismo país.
- Probar IATA y fechas combinados con países. No confundir fecha local con día UTC de filtro.
- Probar los cuatro órdenes y ambas direcciones; conservar los totales y el formato paginado.
- Seleccionar un vuelo con coordenadas: mostrar colores distintos, banderas, etiquetas, curva y avión orientado al destino.
- Probar un extremo sin coordenadas, ambos sin coordenadas y ciudad null.
- Probar longitudes 179° y -179° para evitar el tramo largo, y coordenadas coincidentes para evitar errores matemáticos.
- Confirmar que el recorrido está rotulado como ilustrativo y no como ubicación actual.
- Confirmar que no cambian reservas, precios, pagos ni permisos de operaciones existentes.

## 9. Verificación del backend y despliegue

Casos automatizados: `Tests/MapaVuelosChecks.cs.txt`; regresiones administrativas y públicas en la suite `Completa` de `Tests/VerificarModeloPostgreSql.ps1`. Cubren HTTP local, autorización, validación, contrato Swagger, normalización, coordenadas y traducción de filtros/orden a SQL PostgreSQL. Los repositorios de las pruebas HTTP están simulados. No se hizo consulta a Supabase ni prueba visual de Flutter durante esta entrega.

No requiere migración, ejecución de SQL ni paquetes NuGet nuevos. Después de subir y desplegar el backend en Render, validar con token admin en Swagger: listar vuelos sin filtros, combinar países, abrir un ID real con /recorrido y revisar las coordenadas guardadas. Si faltan coordenadas, corregir los datos de origen mediante el proceso que decida el equipo; la API no fabrica valores.

Ejemplos ejecutables en `MapaVuelosAdmin.http`, junto al proyecto. Sustituir tokens, IDs y fechas de ejemplo por valores reales. Las pruebas de producción y la implementación/presentación del mapa en Flutter quedan a cargo del equipo al integrar esta versión.
