# Catálogos administrativos

Se implementaron 20 endpoints para aeropuertos, aviones, categorías de asiento y asientos físicos. Todos requieren sesión con rol `admin`. Tarifas y consulta global de reservas quedan fuera de este bloque.

## Migración pendiente de ejecución manual

`20261101035850_CatalogosAdministrativos` añade únicamente tres columnas por catálogo: activo, fecha_desactivacion y row_version. Conserva tablas, datos, claves, restricciones únicas y relaciones. Todos los registros previos quedan activos. No fue aplicada automáticamente.

Antes de iniciar la API actualizada, detener la versión anterior y ejecutar manualmente desde la raíz:

```powershell
dotnet build --no-restore
dotnet ef database update CatalogosAdministrativos --no-build
```

El comando utiliza la conexión configurada por el equipo y aplica también migraciones anteriores que estén pendientes. No ejecutar además scripts antiguos de creación de tablas. El rollback impide retirar las marcas de desactivación si existen registros inactivos o con fecha de desactivación.

## Uso

1. Iniciar sesión como administrador y autorizar el token en Swagger.
2. Crear aeropuertos y una categoría activa.
3. Crear el avión con `capacidadTotal`, `categoriaId` y `letrasPorFila`: se generan todos sus asientos en la misma transacción. `ABC` (por defecto) genera 1A, 1B, 1C, 2A...; vacío o null genera 1, 2, 3... El alta admite de 1 a 1000 asientos por operación. La categoría inicial se exige por la relación obligatoria existente; no se inventan categorías. Antes de asociarlos a vuelos, puede editarse la categoría de cada asiento.
4. Crear el vuelo indicando un precio en USD por cada clase del avión mediante `tarifasPorClase`. No es necesario enviar cada asiento: se generan automáticamente con el precio de su categoría.
5. Antes de editar o desactivar un catálogo, consultar su detalle y enviar RowVersion. Usar la nueva versión recibida después de editar.

Los listados muestran activos por defecto. `activo=false` consulta solo inactivos e `incluirInactivos=true` incluye ambos estados. Las búsquedas usan la collation de SQL Server. No se reutilizan códigos únicos de registros desactivados y no se implementó reactivación.

No se permite editar datos usados por vuelos históricos ni modificar la distribución de aviones con vuelos operativos. La desactivación conserva relaciones e historial y se rechaza ante dependencias vigentes. Una categoría requiere no tener asientos físicos activos antes de desactivarse.

Los UPDATE parametrizados preservan las alternate keys originales de EF y sus restricciones SQL; verifican RowVersion en la escritura. Las transacciones administrativas usan `sys.sp_getapplock` para coordinar catálogos y programación de vuelos. No se agregaron dependencias.

## Verificación pendiente con SQL Server

La creación de avión admite `distribucionClases: [{ categoriaId, cantidad }]`. Las categorías deben ser activas, distintas y sus cantidades sumar exactamente la capacidad. Se asignan en el orden recibido a los códigos generados. Primera, Segunda y Tercera Clase son categorías del catálogo: utilizar sus IDs reales, sin asumir que sean 1, 2 y 3. Se conserva `categoriaId` único para clientes anteriores, pero no se combina con distribución. Las letras de `1A`, `1B` identifican posiciones, no clases.

El cambio de avión se realiza con `PATCH /api/admin/vuelos/{id}/avion`, nueva versión y tarifas por clase del avión destino. Se rechaza después de cualquier reserva registrada, aun cancelada. Las filas anteriores permanecen bloqueadas; las consultas operativas solo muestran asientos físicos del avión actual. No se requiere una migración adicional para esta mejora. Los descuentos por tipo de pasajero y precios congelados de reservas permanecen separados del precio base por clase.

Las pruebas locales sin SQL validan servicios, permisos HTTP, documentación, traducción de búsquedas y script de migración. Verificar manualmente persistencia, rechazo de duplicados según collation, nueva RowVersion en cada edición y rollback ante fallo.

Ejecutar simultáneamente altas de asientos para la última plaza y comprobar que no excedan la capacidad. Competir una desactivación de avión/aeropuerto/categoría con la creación de un vuelo: debe rechazarse una de las operaciones, sin referencias operativas inactivas. Comprobar que vuelos y reservas históricos sigan consultándose tras desactivar catálogos que ya no tengan vuelos operativos.
