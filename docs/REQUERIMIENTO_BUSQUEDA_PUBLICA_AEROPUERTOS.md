# Requerimiento: búsqueda pública de aeropuertos para Flutter

Estado: implementado en el código del backend. Pendiente de desplegar y validar con los datos reales de Supabase.

## Objetivo y experiencia del cliente

En Comprar, el cliente pulsa **Desde** o **Hacia** y se abre un modal. Escribe el nombre de un país, por ejemplo **Ecuador**, y recibe los aeropuertos coincidentes. Selecciona **Quito · Mariscal Sucre** y Flutter conserva internamente su `codigoIata` para buscar vuelos.

El cliente no necesita conocer ni escribir códigos IATA. La búsqueda inicial es por país; no se requiere búsqueda por ciudad o nombre de aeropuerto en esta entrega.

## 1. Endpoint público propuesto

```http
GET /api/aeropuertos?buscar=Ecuador&pagina=1&tamanoPagina=20
```

- Disponible sin token, tanto para visitantes como para clientes autenticados.
- Mantener los permisos de `/api/admin/aeropuertos`; no convertir el catálogo administrativo en público.
- Exponer únicamente los datos de consulta indicados en este documento.

## 2. Parámetros

| Parámetro | Tipo | Reglas propuestas |
|---|---|---|
| `buscar` | string | Obligatorio; quitar espacios al inicio y al final; entre 2 y 100 caracteres. Buscar por nombre de país o código ISO de dos letras. |
| `pagina` | integer | Desde 1; por defecto 1. |
| `tamanoPagina` | integer | Entre 1 y 100; por defecto 20. |

La comparación debe ignorar mayúsculas y tildes. Por ejemplo:

- `Ecuador`, `ecuador` y `EC` deben devolver los aeropuertos cuyo `pais` sea `EC`.
- `Perú`, `Peru` y `PE` deben corresponder al país `PE`.
- Para búsquedas parciales, como `Ecua`, devolver los aeropuertos de los países cuyos nombres coincidan con el texto.

El backend debe resolver los nombres de países a los códigos almacenados. No comparar únicamente el texto `Ecuador` con la columna que contiene `EC`.

## 3. Selección y paginación

- Devolver únicamente aeropuertos activos y con un código IATA válido.
- Buscar entre esos aeropuertos sin exigir que tengan vuelos disponibles en ese momento. La búsqueda posterior de vuelos determina la disponibilidad para las fechas seleccionadas.
- Aplicar el filtro y la paginación en el servidor; no enviar todo el catálogo para filtrarlo en Flutter.
- Ordenar de manera estable por ciudad, nombre e identificador para evitar repeticiones entre páginas.
- `total` debe indicar la cantidad de aeropuertos que coinciden con el filtro, antes de paginar.

## 4. Respuesta propuesta

Ejemplo ilustrativo; los identificadores y los datos deben proceder de la base real:

```json
{
  "elementos": [
    {
      "aeropuertoId": 1,
      "codigoIata": "UIO",
      "nombre": "Mariscal Sucre",
      "ciudad": "Quito",
      "pais": "EC"
    },
    {
      "aeropuertoId": 2,
      "codigoIata": "GYE",
      "nombre": "José Joaquín de Olmedo",
      "ciudad": "Guayaquil",
      "pais": "EC"
    }
  ],
  "pagina": 1,
  "tamanoPagina": 20,
  "total": 2,
  "totalPaginas": 1
}
```

### Campos de cada aeropuerto

| Campo | Tipo | Uso en Flutter |
|---|---|---|
| `aeropuertoId` | integer | Identificar el aeropuerto. |
| `codigoIata` | string | Filtrar los vuelos; tres letras, preferiblemente en mayúsculas. |
| `nombre` | string | Mostrar el nombre del aeropuerto. |
| `ciudad` | string o null | Mostrar la ciudad; si no existe, Flutter muestra el nombre del aeropuerto. |
| `pais` | string | Código ISO de dos letras, como `EC` o `CO`, para obtener el nombre y la bandera en Flutter. |

Conservar `pais` como nombre del campo para mantener compatibilidad con `origen.pais` y `destino.pais` de los vuelos.

## 5. Respuestas y errores

| Caso | Respuesta |
|---|---|
| Coincidencias encontradas | `200` con el contenedor paginado. |
| Sin coincidencias | `200`, `elementos: []`, `total: 0`, `totalPaginas: 0`. |
| Página posterior a la última | `200`, lista vacía y totales reales del filtro. |
| Parámetros fuera de los límites o búsqueda vacía | `400`, usando el formato habitual de errores de la API. |

No devolver `401` por ausencia de token en este endpoint público.

## 6. Compatibilidad con la búsqueda de vuelos

Mantener el contrato existente de consulta por IATA:

```http
GET /api/vuelos?origen=UIO&destino=BOG&fechaDesde=...&fechaHasta=...
```

El ejemplo anterior es esquemático: Flutter enviará las fechas completas con el formato UTC vigente del contrato, no los puntos suspensivos.

Flutter utilizará los códigos de los aeropuertos seleccionados para los parámetros `origen` y `destino`. No enviará nombres de países en esos parámetros. Conservar los filtros existentes de fecha y paginación de vuelos.

La clase de asiento y la cantidad de viajeros se comprueban actualmente en Flutter mediante la disponibilidad de cada vuelo. Este requerimiento no añade parámetros nuevos para esos filtros al backend.

## 7. Responsabilidades de Flutter

- Mostrar **Desde** y **Hacia** en el formulario.
- Abrir un modal con un campo para escribir el país y una lista de aeropuertos coincidentes.
- Mostrar ciudad, nombre del aeropuerto y país con bandera.
- Consultar al escribir, con una espera breve para evitar una solicitud por cada tecla.
- Cargar más resultados según sea necesario, sin controles de páginas numéricas.
- Mostrar las búsquedas recientes al abrir el modal, sin consultar todo el catálogo.
- Guardar los aeropuertos seleccionados en el dispositivo y conservar su IATA internamente.
- Evitar seleccionar el mismo aeropuerto como origen y destino. Permitir aeropuertos distintos dentro del mismo país.
- Conservar fechas, clase y cantidad de viajeros durante la selección.

## 8. Cambios que no se requieren

- No almacenar imágenes de banderas en la base: Flutter las genera a partir del código ISO.
- No guardar las búsquedas recientes en el backend.
- No modificar reservas, pasajeros, pagos ni sus permisos.
- No cambiar puertos ni direcciones del despliegue por este requerimiento.
- No se necesita una migración si los campos requeridos ya están almacenados. Confirmarlo con el esquema actual.

## 9. Criterios de aceptación del backend

- [ ] La consulta funciona sin sesión.
- [ ] `Ecuador` y `EC` devuelven las mismas coincidencias.
- [ ] `Perú` y `Peru` producen las mismas coincidencias.
- [ ] Una búsqueda parcial por país devuelve coincidencias.
- [ ] Se excluyen aeropuertos inactivos o sin IATA válido.
- [ ] La respuesta incluye los cinco campos indicados para cada aeropuerto.
- [ ] `pais` contiene el código ISO importado.
- [ ] Los resultados tienen orden estable y no se repiten entre páginas.
- [ ] Los totales corresponden al filtro completo.
- [ ] La búsqueda sin coincidencias devuelve una lista vacía con `200`.
- [ ] Los parámetros inválidos devuelven `400` con el formato de errores habitual.
- [ ] El endpoint administrativo mantiene su autenticación y autorización.
- [ ] Los aeropuertos seleccionados pueden usarse en la consulta existente de vuelos mediante sus IATA.

## 10. Entrega al equipo de frontend

Compartir la ruta definitiva, los parámetros aceptados, ejemplos reales de éxito y error, y la documentación actualizada en `docs`. Si el backend ya dispone de una consulta pública equivalente, confirmar su contrato para reutilizarla en lugar de crear otra.

La ruta definitiva es `GET /api/aeropuertos`, con el contrato descrito arriba. Flutter puede conectar el modal cuando se despliegue esta versión. Las consultas administrativas y los permisos de vuelos, reservas y pagos conservan su comportamiento.

## 11. Notas de implementación y verificación

- No requiere migraciones ni nuevos paquetes. No se modificó la base de datos.
- Los nombres españoles de países/regiones están incorporados en `Models/Catalogos/paises.es.json`, generados con `Intl.DisplayNames('es', {type: 'region'})` y CLDR/ICU 78.3. Son datos estáticos: no se llama a una API externa ni se necesita Node en Render. Los códigos ISO de dos letras se buscan de forma exacta; el texto de tres o más caracteres se compara parcialmente sin mayúsculas ni tildes.
- El repositorio filtra en PostgreSQL: activo, IATA de tres letras mayúsculas y país coincidente. Cuenta antes de paginar, ordena por ciudad/nombre/ID y proyecta únicamente los cinco campos públicos. No exige vuelos disponibles ni carga los metadatos JSON del CSV.
- Los ejemplos de este documento son ilustrativos; IDs y cantidades dependen de los registros de cada base. No se obtuvieron ejemplos de la base de producción durante esta implementación.
- Pruebas en `Tests/AeropuertosPublicosChecks.cs.txt`: resolución de países, SQL generado y HTTP local con repositorio simulado. Se ejecutan con la suite `Completa` de `Tests/VerificarModeloPostgreSql.ps1`. La comprobación con Supabase real y Flutter queda pendiente.
- Swagger presenta la consulta en **14. Aeropuertos (consulta pública)** sin exigir Bearer. Ejemplos manuales en `AeropuertosPublicos.http`.

Ejemplo de error para `GET /api/aeropuertos?buscar=E` (el texto, type y traceId pueden variar según ASP.NET):

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Buscar": ["La búsqueda debe tener entre 2 y 100 caracteres."]
  }
}
```
