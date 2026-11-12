# Catalogos administrativos

Los cuatro catalogos son de consulta: aeropuertos, aviones, categorias de asiento y asientos fisicos. Cada uno conserva GET de listado y detalle bajo /api/admin y requiere rol admin. Las rutas POST de alta, PUT y POST de desactivacion fueron retiradas, junto con sus servicios, contratos y persistencia sin uso.

## Carga y consultas

1. Autorizar un token admin en Swagger.
2. Importar aeropuertos con POST /api/admin/aeropuertos/importacion (ver ../ImportacionAeropuertos.http).
3. Consultar las categorias existentes y usar sus IDs reales en la importacion de aviones.
4. Importar aviones con POST /api/admin/aviones/importacion (ver ../ImportacionAviones.http). La carga genera sus asientos automaticamente segun capacidad, clases y letras del CSV, en una transaccion.
5. Consultar los catalogos para crear vuelos con las tarifas correspondientes a cada clase.

Los listados muestran activos por defecto. activo=false consulta inactivos e incluirInactivos=true ambos estados. AvionId filtra los asientos fisicos. Los detalles conservan estado, RowVersion y metadatos importados. Una carga repetida no duplica los registros existentes.

## Persistencia y comprobaciones

Se conservan entidades, mapeos, columnas de estado, RowVersion, relaciones y migraciones anteriores: siguen siendo necesarios para las consultas, importaciones e historial. La limpieza de codigo no requiere una migracion ni modifica datos.

Tests/CatalogosChecks.cs.txt verifica consultas, permisos, rutas retiradas, Swagger y mapeos sin conectar a SQL Server. Las suites de importacion comprueban validaciones, duplicados y rollback con repositorios en memoria. La persistencia y concurrencia reales requieren comprobacion manual con SQL Server.
