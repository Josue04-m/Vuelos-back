# Persistencia de Pacific Nexus Airways

La especificación vigente es `PROYECTO_API.md`. El equipo aclaró que la base
se creará desde cero mediante migraciones; no hay un SQL original que comparar.
Las referencias del documento a una base existente no describen este entorno.
Se adopta `dbo` y se proponen nombres explícitos de PK, FK, UQ y CHECK.

## Modelo preparado

- Las 11 tablas de la sección 15.1 tienen entidades y configuraciones propias.
- `Autenticacion/TokenSesionConfiguration.cs` representa la ampliación `Tokens`.
- `AmpliacionesConfiguration.cs` contiene las otras ampliaciones: `row_version`
  en reservas y asientos por vuelo, índice único filtrado de detalle activo y
  las marcas de eliminación de usuarios, con filtro de consulta.
- La migración inicial y su snapshot ya fueron creados y aplicados por el equipo.
  El nuevo modelo agrega tipo_pasajero nullable y su CHECK a detalle_reservas;
  su migración TiposPasajeroReservas ya está generada y pendiente de aplicación.
  DatosLaboratorioAdminJosue prepara dos cuentas de prueba mediante otra migración.
  NombresPasajerosSeparados renombra los primeros nombres/apellidos y agrega
  los segundos campos opcionales; su aplicación también es manual.
- Las relaciones usan `NoAction`; `SaveChanges` y `SaveChangesAsync` rechazan
  entidades marcadas como `Deleted`. La futura API tampoco deberá usar
  `ExecuteDelete`, SQL DELETE ni otros mecanismos que evadan esta protección.
- Las restricciones únicas del esquema base se representan como claves
  alternativas para generar UQ; el índice de asignación activa es filtrado.
- No se agrega CHECK sobre `tipo_accion`, ni tablas de pasajeros o pagos.

## Configuración manual pendiente

El equipo instaló EF Core SQL Server y Design 10.0.14. También están referenciados
Swagger, SwaggerGen y SwaggerUI 10.3.1, conservando las versiones instaladas.
El default de `eliminado` es false y está nombrado `DF_usuarios_eliminado`.
El equipo ya utilizó dotnet-ef para la migración inicial.
No se instalaron ni restauraron paquetes durante la implementación.

Configurar `ConnectionStrings:testConnection` mediante secretos de usuario o
la variable de entorno `ConnectionStrings__testConnection`. No se guardaron
credenciales. El registro del contexto no crea ni actualiza la base al arrancar.

La migración inicial se conserva. La ampliación de tipo_pasajero se describe
en Verification/Reservas.md, junto con el orden de generación, compilación y
aplicación manual. No se asigna un tipo inventado a detalles anteriores.
Los usuarios admin y josue están preparados en una migración de laboratorio
separada, pendiente de aplicación; no se reemplazan cuentas ni hashes existentes.
Los vuelos de demostración siguen pendientes.

## Límites y siguientes pasos

- Los defaults `GETDATE()` originales se conservan; no garantizan UTC.
  Los futuros servicios deberán enviar fechas UTC explícitas, también para
  `Tokens`, que conserva columnas `datetime` según el laboratorio.
- La duración de sesión debe configurarse en
  `Autenticacion:DuracionTokenMinutos`, como entero positivo, sin default.
  La API valida esta configuración al arrancar. La moneda acordada es USD.
  Los aeropuertos todavía no contienen zona horaria.
- El filtro de usuarios oculta eliminados en consultas normales. Las consultas
  futuras de historial que incluyan una navegación requerida a Usuario deben
  considerar ese filtro para no ocultar reservas o eventos históricos; usar
  consultas explícitas autorizadas cuando corresponda, sin quitar la validación
  de usuario no eliminado en autenticación.
- Reservas ya utiliza transacciones, lectura de vuelo con UPDLOCK/HOLDLOCK,
  actualizaciones condicionales de asientos e índice de asignación activa.
  La comprobación integral de estos mecanismos contra SQL Server está pendiente.
- Registro, autenticación, vuelos y reservas ya tienen Controllers, interfaces,
  Repositories, Services y DTOs. No se implementaron semillas ni se ejecutaron
  pruebas de concurrencia sobre SQL Server.
  El ejemplo WeatherForecast se conserva.
- Las carpetas futuras están preparadas sin clases vacías. Git no conserva
  carpetas vacías; deberán incorporarse cuando tengan implementación.
- Swagger UI está en `/swagger` en Development. Se eliminó el registro del
  generador integrado de ASP.NET Core. No hace falta un paquete de JWT ni un
  paquete adicional para RandomNumberGenerator o PasswordHasher.

## Comprobaciones de esta entrega

La revisión inicial comprobó los mapeos de las 11 tablas base y sus 68 columnas.
Después de la instalación manual de dependencias, `dotnet build --no-restore`
terminó con cero errores y advertencias. Las comprobaciones de
`Verification/AutenticacionChecks.cs.txt` construyen el modelo de 12 entidades,
verifican las 17 relaciones sin cascada, RowVersion y el índice filtrado;
además prueban servicios y HTTP con repositorios en memoria.
Se verificaron Swagger UI y su JSON en la aplicación real, sin consultas SQL.
La persistencia, el índice único ante registros simultáneos y las transacciones
contra SQL Server siguen sin comprobarse durante la implementación.
La migración inicial fue ejecutada por el equipo; las de tipo_pasajero y cuentas
de laboratorio están generadas y pendientes de aplicación manual.
Reservas tiene pruebas adicionales en Verification/ReservasChecks.cs.txt y
un procedimiento manual de concurrencia en Verification/Reservas.md.
