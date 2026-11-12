# Referencia histórica SQL Server

Las 10 migraciones SQL Server y su snapshot permanecen en el directorio padre, sin modificar su contenido. El traslado físico a esta carpeta no se pudo realizar por permisos del entorno.

`Vuelos_Backend.csproj` excluye `Data/Migrations/*.cs` y `Data/Migrations/SqlServer/**/*.cs` de compilación. Por tanto, EF no debe descubrir las migraciones SQL Server en la versión PostgreSQL. No se eliminaron ni ejecutaron.

La etapa 4 preparó `20261111050328_InicialPostgreSql` y el único snapshot activo en `Data/Migrations/PostgreSql`. Su aplicación sigue siendo manual y requiere revisar `docs/SUPABASE_ETAPA_4.md`; no ejecutar migraciones SQL Server sobre Supabase.

El cambio de carpeta puede completarse posteriormente si se desea, manteniendo la exclusión y verificando antes que todos los archivos se conserven.
