# Pacific Nexus Airways — Especificación de la API

## 1. Propósito y alcance

Módulo administrativo de mapa de vuelos: listado paginado de todos los estados con filtros ISO de países de origen/destino y ordenamiento; consulta `GET /api/admin/vuelos/{id}/recorrido` con coordenadas WGS84 existentes y banderas a partir de ISO. Mantener rol admin, rutas/contratos públicos y operaciones de escritura. El recorrido es ilustrativo, sin posición real del avión. La visualización se implementa en Flutter. Contrato: `docs/FLUTTER_MAPA_VUELOS_ADMIN.md`; no requiere cambios de esquema ni dependencias nuevas.

Actualización de importación de aeropuertos: omitir automáticamente IATA existentes, incluidos los inactivos, y guardar solo nuevos bajo la transacción y bloqueo administrativo existentes. No sobrescribir ni reactivar registros. Contar existentes como omisiones `iata_ya_registrado`, sin errores. La carga mixta deja de responder `conflicto_codigos`; si todos existen, responder `ya_importados`. No requiere migración. Ver `IMPORTACION_AEROPUERTOS.md`.

Actualización de importación: `ciudad` de aeropuerto es opcional y se conserva como NULL si `municipality` está vacío o indica `\N`. Modelo, DTOs e importador están adaptados; aplicar manualmente la migración PostgreSQL `CiudadAeropuertoOpcional` o `Data/Scripts/Supabase_CiudadAeropuertoOpcional.sql`. IATA, nombre y país mantienen sus validaciones. Ver `IMPORTACION_AEROPUERTOS.md`.

Requerimiento adicional: consulta pública paginada de aeropuertos por país en `GET /api/aeropuertos`, sin token. Contrato, límites y responsabilidades de Flutter: `docs/REQUERIMIENTO_BUSQUEDA_PUBLICA_AEROPUERTOS.md`. La administración permanece protegida; no cambia los permisos de consulta de vuelos ni de reservas/pagos.

Diseñar e implementar una API de reservas de vuelos basada en arquitectura orientada a servicios (SOA). Será consumida por una aplicación móvil desarrollada en Flutter y por los clientes administrativos que se desarrollen posteriormente. Este documento abarca todo el backend, incluidos los servicios y endpoints de administración.

La API permitirá registrar clientes, autenticar usuarios, consultar vuelos, seleccionar asientos, confirmar reservas y consultar, modificar o cancelar las reservas propias. También permitirá administrar aeropuertos, aviones, categorías y asientos, vuelos, tarifas y estados operativos mediante endpoints protegidos para el rol admin.

## 2. Tecnologías y decisiones

- Backend: C# y ASP.NET Core Web API con Controllers. Esta decisión reemplaza la referencia anterior a Flask.

- Persistencia original: SQL Server mediante Entity Framework Core. En la rama `feature/SupaBase` se prepara PostgreSQL de Supabase por etapas; ver `docs/SUPABASE_ETAPA_1.md` a `docs/SUPABASE_ETAPA_5.md`. Modelo, repositorios y migración están preparados. La suite offline completa está adaptada y aprobada; las pruebas contra Supabase y PayPal Sandbox reales siguen pendientes de conexión/base de pruebas y ejecución. No se declara operativo ni se hace el corte de datos todavía.

- Consumidor futuro: Flutter, mediante HTTP/HTTPS y JSON.

- Documentación y pruebas manuales: Swagger UI 

- Autenticación: tokens opacos aleatorios, persistidos y con expiración; no implementar JWT.

- Concurrencia: SQL Server `rowversion`, transacciones y restricciones de integridad.

- Eliminación: exclusivamente lógica.

- Las versiones de .NET, EF Core y paquetes serán elegidas por el usuario o el equipo.

## 3. Instrucciones obligatorias para Codex

1. Leer el proyecto existente antes de modificarlo y respetar sus convenciones.

2. No instalar, restaurar ni actualizar dependencias automáticamente. No ejecutar herramientas que descarguen paquetes ni crear proyectos con restauración automática.

3. No elegir ni fijar versiones de paquetes por cuenta propia. Inspeccionar primero las versiones existentes.

4. Implementar lo posible sin instalar paquetes. Si faltan dependencias necesarias, documentar el bloqueo y no afirmar que se compiló o probó correctamente.

5. Al finalizar, indicar los archivos modificados, las comprobaciones realizadas, las pendientes y las dependencias que el usuario debe instalar manualmente, explicando para qué sirve cada una. Las versiones quedan a decisión del equipo.

6. Implementar el backend completo, incluidos los endpoints administrativos. Las interfaces web y móviles se desarrollarán por separado.

7. No ejecutar migraciones ni scripts contra la base de datos automáticamente. Prepararlos para revisión y ejecución manual.

8. No eliminar físicamente registros ni configurar eliminaciones físicas en cascada.

9. No guardar contraseñas de usuarios en texto plano ni registrar tokens o credenciales en logs.

## 4. Organización de carpetas

Mantener las carpetas principales solicitadas y agrupar su contenido por funcionalidad. Los nombres siguientes son ejemplos orientativos:

```text
Controllers/
    Usuarios/UsuariosController.cs
    Autenticacion/AutenticacionController.cs
    Vuelos/VuelosController.cs
    Reservas/ReservasController.cs
Data/
    AppDbContext.cs
    Configurations/
        Usuarios/
        Autenticacion/
        Vuelos/
        Reservas/
    Migrations/
    Scripts/
Interface/
    Usuarios/
    Autenticacion/
    Vuelos/
    Reservas/
Models/
    Usuarios/
    Autenticacion/
    Aeropuertos/
    Aviones/
    Asientos/
    Vuelos/
    Reservas/
    Common/
Repositories/
    Usuarios/
    Autenticacion/
    Vuelos/
    Reservas/
Services/
    Usuarios/
    Autenticacion/
    Vuelos/
    Reservas/
Program.cs
appsettings.json
```

Crear solamente carpetas y clases que realmente se necesiten. Dentro de cada funcionalidad de Models, separar entidades y DTOs cuando corresponda. Mantener el nombre `Interface` solicitado.

| Carpeta | Responsabilidad |
| --- | --- |
| Controllers | Recibir solicitudes, validar el contrato HTTP, invocar servicios y devolver respuestas. |
| Data | Contexto, mapeos de EF Core, índices, restricciones, migraciones y scripts. |
| Interface | Contratos de servicios y repositorios, por ejemplo IUsuarioService e IUsuarioRepository. |
| Models | Entidades, DTOs de entrada/salida, enumeraciones y modelos comunes. |
| Repositories | Consultas y persistencia; sin reglas de negocio ni respuestas HTTP. |
| Services | Reglas del negocio, autenticación, reservas y coordinación de operaciones transaccionales. |

Flujo: Controller → interfaz del servicio → Service → interfaz del repositorio → Repository → AppDbContext → SQL Server.

Registrar implementaciones mediante inyección de dependencias. Los repositorios que participen en una reserva compartirán el mismo contexto y transacción; no confirmar parcialmente desde cada repositorio. No devolver entidades de EF directamente como contratos públicos.

## 5. Arquitectura SOA

- Consumidores: aplicación móvil Flutter y futuros clientes administrativos.

- Proveedor: API ASP.NET Core que expone los servicios de negocio.

- Contratos: endpoints, métodos HTTP, DTOs JSON, autenticación y respuestas documentadas.

- Persistencia: SQL Server centralizado.

Los servicios pueden ejecutarse dentro de una misma API, con responsabilidades y contratos separados. Las capas de carpetas no sustituyen la definición de servicios. Elaborar un diagrama que muestre consumidor, servicios y persistencia. No agregar microservicios ni un bus de servicios para este alcance.

## 6. Conexión de desarrollo

Usar la clave `ConnectionStrings:testConnection` con la siguiente configuración local. La doble barra es el escape correcto dentro de JSON:

```json
{
  "ConnectionStrings": {
    "testConnection": "Server=.\\ANALYTICS_MD; Database=Vuelos; User id=sa; Password=123456; TrustServerCertificate=true"
  }
}
```

Conservar la configuración real en secretos de usuario o variables de entorno y no subirla a un repositorio público. La instancia indicada es local al equipo donde se ejecuta la API. Flutter consumirá la API y nunca se conectará directamente a SQL Server.

## 7. Servicios y contratos iniciales

| Servicio | Método y endpoint | Acceso | Función |
| --- | --- | --- | --- |
| Usuarios | POST /api/usuarios/registro | Público | Registrar nombres, apellidos, correo y contraseña. |
| Autenticación | POST /api/autenticacion/login | Público | Validar credenciales y emitir token con expiración. |
| Autenticación | POST /api/autenticacion/logout | Autenticado | Revocar el token de la sesión actual. |
| Vuelos | GET /api/vuelos | Público | Buscar por origen, destino y fecha o rango de fechas. |
| Vuelos | GET /api/vuelos/{id} | Público | Consultar horarios, estado y datos del vuelo. |
| Vuelos | GET /api/vuelos/{id}/asientos | Público | Consultar asientos, precios y disponibilidad. |
| Reservas | POST /api/reservas | Autenticado | Retener temporalmente vuelo, pasajeros y asientos hasta su compra o vencimiento. |
| Reservas | GET /api/reservas | Autenticado | Listar reservas propias con filtros y paginación. |
| Reservas | GET /api/reservas/{id} | Propietario | Consultar detalle propio. |
| Reservas | PATCH /api/reservas/{id}/asientos | Propietario | Cambiar asientos y confirmar el total esperado. |
| Reservas | POST /api/reservas/{id}/cancelacion | Propietario | Cancelar y liberar asientos conservando el historial. |
| PayPal | POST /api/reservas/{id}/paypal/orden | Propietario | Crear orden con importe congelado de la reserva. |
| PayPal | POST /api/reservas/{id}/paypal/captura | Propietario | Capturar orden aprobada y verificar el cobro. |
| PayPal | GET /api/reservas/{id}/paypal | Propietario | Consultar y conciliar el pago. |
| PayPal | POST /api/pagos/paypal/webhook | Firma PayPal | Recibir eventos verificados e idempotentes. |
| PayPal | POST /api/admin/pagos/paypal/{pagoId}/conciliacion | Admin | Consultar PayPal sin iniciar cobros. |

Registrar estos contratos en Swagger, incluidos DTOs, ejemplos, parámetros, autenticación Bearer y errores. Usar respuestas coherentes: 400 para entradas inválidas, 401 para token ausente/inválido/expirado, 404 para recursos inexistentes o reservas ajenas, y 409 para conflictos de disponibilidad, versión o estado. No revelar la existencia de reservas de otros usuarios.

Swagger incluye grupos numerados para seguir el flujo de laboratorio y una guía en la descripción principal. Reservas están en el grupo 10, ventas acumuladas en el grupo 11 y pagos PayPal en el grupo 12, con GET /api/admin/ventas/resumen. Los ejemplos son ilustrativos; usar IDs y RowVersion reales y el total calculado por el backend. La configuración se centraliza en Swagger/DocumentacionApi.cs y su prueba utiliza esa misma configuración sin conectar a SQL Server.

La ampliación autorizada reemplaza la compra simulada por PayPal Orders v2, inicialmente Sandbox. Live usa dinero real y exige configuración específica. No se implementan reembolsos automáticos ni se reciben tarjetas en esta API. Ver PAYPAL.md.

### 7.1. Administración y protección de endpoints

La administración forma parte del alcance del backend. Implementar consultas e importación de aviones; consultas de asientos físicos; consultas e importación de aeropuertos; consulta del catálogo fijo existente de categorías de asiento; gestión de vuelos y sus asientos, precios y disponibilidad; configuración de tarifas por tipo de pasajero; consulta administrativa de reservas y actualización de estados operativos con historial.

Organizar estas operaciones bajo `/api/admin`. Todas requieren un token de sesión válido y rol `admin`, verificados en el servidor. Un cliente autenticado no obtiene permisos administrativos. Devolver 401 si la sesión falta, es inválida o expiró, y 403 si el usuario autenticado no tiene el rol requerido. No aceptar roles ni identidad administrativa enviados por el cliente; obtenerlos desde la sesión y el usuario persistido.

Mantener públicos el registro y el login; el webhook PayPal no usa sesión, pero exige verificar su firma con PayPal. Las consultas de vuelos y asientos requieren una sesión válida. Proteger las operaciones de reservas, la consulta de cédula y todas las operaciones administrativas. El registro público seguirá asignando exclusivamente el rol `cliente`.

Documentar los contratos administrativos, DTOs, permisos y errores en Swagger. Respetar las reglas de integridad, concurrencia y eliminación lógica existentes. No permitir cambios de avión, asientos o disponibilidad que invaliden reservas vigentes. Definir y documentar las transiciones permitidas entre estados de vuelo; registrar el administrador, fecha, estado anterior, nuevo estado y observación en el historial. Al cancelar un vuelo, cancelar sus reservas vigentes y liberar sus asignaciones en una misma transacción, conservando precios e historial y sin implementar reembolsos.

Si alguna función administrativa requiere columnas o tablas adicionales —por ejemplo, configuración de tarifas o marcas de desactivación—, documentar y preparar la ampliación incremental correspondiente para ejecución manual, conservando los datos existentes. No inventar importes ni porcentajes.

Agregar comprobaciones de acceso sin sesión, acceso con rol cliente y acceso con rol admin para todos los endpoints administrativos, además de las validaciones de negocio correspondientes.

### 7.2. Reservas temporales y pagos PayPal

Las reservas nuevas retienen asientos durante 720 minutos configurables en `Reservas:DuracionReservaMinutos`, sin superar la salida del vuelo. El precio se congela por pasajero; cambiar asientos no extiende el plazo. Reservas anteriores con vencimiento NULL conservan su historia, sin inferir pago.

El propietario crea una orden PayPal enviando RowVersion, aprueba en el navegador y solicita captura. El backend usa exclusivamente el importe USD persistido y verifica orden, referencia de pago, vendedor y captura completa. Solo `COMPLETED` cambia la reserva a `pagada`. `APPROVED`, el retorno del navegador y `PENDING` no acreditan una compra. La ruta de compra simulada fue retirada y sus comprobantes se conservan como histórico.

Antes de enviar una captura se persiste la protección de asientos. Los pagos pendientes o inciertos bloquean cancelación, expiración y cierre operativo que los libere; se concilian con GET oficial, worker y webhook verificado. No mantener transacciones SQL abiertas durante HTTP ni crear un nuevo cobro para resolver una respuesta incierta. Capturas tardías sobre reservas ya liberadas requieren revisión y nunca reasignan asientos automáticamente.

Crear una orden congela cambios de asientos e importes, aunque admite cancelar antes del cobro. Sin pago en curso, vencer o cancelar libera los asientos. Las pagadas no expiran. Vuelos con pagos protegidos no pueden cerrarse; vuelos con capturas o pagos protegidos no se cancelan administrativamente hasta resolver el caso de pago. No se inventan reembolsos.

`GET /api/admin/ventas/resumen` usa SUM/COUNT de capturas confirmadas en `pagos_paypal`, sin duplicar reintentos ni contar reservas pendientes o compras simuladas históricas. `entorno=Sandbox` es el valor inicial; `entorno=Live` se consulta separado. Devuelve `totalVentas`, `cantidadCompras`, `proveedor`, `entorno`, `esSandbox` y moneda USD, con fechas UTC inclusivas. Es bruto histórico, sin descontar comisiones o reembolsos.

La migración `PagosPayPal` agrega exclusivamente `pagos_paypal` y `eventos_paypal`, con índices únicos y RowVersion. Se prepara para revisión y ejecución manual; no convierte datos previos. Configuración segura, URLs, eventos y pruebas manuales: `PAYPAL.md`. Ejemplos: `PayPal.http`.
## 8. Autenticación con tokens aleatorios

### Implementación administrativa: etapa 1, vuelos

- `GET /api/admin/vuelos`: listado paginado, con los filtros existentes de origen, destino y fechas UTC; incluye todos los estados.
- `GET /api/admin/vuelos/{id}`: detalle, cantidad de asientos y `version` para editar.
- `POST /api/admin/vuelos`: crea un vuelo `programado`, sus asientos `disponible` y el historial inicial con el administrador de la sesión. Recibe `tarifasPorClase`, con `categoriaId` y `precio` base en USD para cada categoría presente en el avión, sin omisiones, duplicados ni categorías extra. El servidor lee sus asientos físicos y asigna el precio de su clase; no se envía una lista de asientos ni precios individuales.
- `PUT /api/admin/vuelos/{id}`: edita número, ruta y horarios. Exige `version` del detalle administrativo; conserva avión, asientos, precios y estado. Rechaza vuelos con retenciones vigentes, compras registradas, reservas antiguas, operaciones iniciadas o salida pasada.
- `PATCH /api/admin/vuelos/{id}/avion`: recibe `avionId`, `version` y `tarifasPorClase` del avión nuevo. Solo permite vuelos futuros programados o retrasados, sin horas reales, sin retenciones vigentes, compras registradas ni reservas anteriores al flujo temporal, y sin asientos reservados. Verifica avión activo, capacidad completa, clases activas y ausencia de solapamiento. Permite cambiar capacidad según el avión nuevo. Conserva las filas de asientos anteriores bloqueadas y oculta las ajenas al avión actual en consultas operativas; si se regresa al avión anterior, reutiliza sus claves. Nunca borra registros ni modifica una reserva. Registra actor, fecha y ambos aviones en el historial del vuelo.

Todos estos endpoints requieren rol `admin`. Las fechas de entrada llevan hora y zona explícita (`Z` o desplazamiento) y se normalizan a UTC. La llegada debe ser posterior a la salida y la salida futura. No se permite programar vuelos no cancelados del mismo avión con intervalos superpuestos; los intervalos contiguos se admiten, sin inventar un tiempo mínimo de escala.

La creación y edición son transaccionales. Bloquean primero el avión para serializar su programación; la edición bloquea después el vuelo con `UPDLOCK, HOLDLOCK`, compatible con el protocolo de reservas. La comprobación de reservas registradas se realiza manteniendo ese bloqueo. `version` es una huella del contenido actual de programación, comprobada dentro de la transacción; no agrega ni sustituye una columna SQL `rowversion`.

Esta etapa usa el esquema existente y no requiere migración. Requiere aeropuertos, avión y asientos físicos ya cargados. La gestión de catálogos, cambios de estado, disponibilidad/precios, tarifas y consultas globales de reservas corresponden a las siguientes etapas. Ejemplos manuales: `VuelosAdmin.http`.

### Implementación administrativa: etapa 2, gestión operativa de vuelos (estados, asientos y reservas)

Reglas de estados y horas reales:

- Desde `embarcando` solo se permite `en vuelo` o `cancelado`; no se vuelve a `retrasado` ni `programado`.
- El embarque no asigna `SalidaReal`. Esta se registra únicamente al entrar en `en vuelo`; `LlegadaReal`, únicamente al finalizar. Si se omiten, se usa la hora UTC actual.
- Las horas reales no pueden ser futuras y la llegada debe ser estrictamente posterior a la salida. Enviar una hora real para otro estado devuelve 400.
- Los vuelos con salida o llegada real registrada no permiten reservas ni cambios/cancelaciones del cliente, aunque su estado antiguo indique `programado` o `retrasado`.
- Si un vuelo ya tiene horas reales antes de entrar en `en vuelo`, o carece de salida válida al finalizar, devuelve 409 con código `horarios_requieren_revision`. No se sobrescriben automáticamente datos históricos asignados por versiones anteriores; deben revisarse por el equipo. No se garantiza detectar una hora histórica incorrecta que sea temporalmente coherente.
- El historial conserva sus eventos aunque el administrador se elimine lógicamente; mantiene identificador del actor, fecha, estados y observación.

- `POST /api/admin/vuelos/{id}/estado`: cambia el estado operativo del vuelo (`programado`, `embarcando`, `retrasado`, `en vuelo`, `finalizado`, `cancelado`) registrando auditoría en `historial_estados_vuelo`. Valida matriz de transiciones permitidas y exige `version`. Si se cancela el vuelo, en la misma transacción se cancelan sus reservas vigentes (`cancelada`), se liberan sus asignaciones (`liberado`), se registran eventos en `historial_cambios_reserva` y se liberan los asientos a `disponible`.
- `GET /api/admin/vuelos/{id}/historial`: consulta la trazabilidad completa de cambios de estado del vuelo con actor administrativo, fechas UTC y observaciones.
- `GET /api/admin/vuelos/{id}/asientos`: lista los asientos por vuelo indicando código, categoría, precio base en USD, estado operativo (`disponible`, `reservado`, `bloqueado`), `RowVersion` en Base64 y si cuenta con reserva activa.
- `PATCH /api/admin/vuelos/{id}/asientos/{asientoVueloId}`: permite al administrador ajustar el precio base o bloquear/desbloquear un asiento mediante concurrencia con `RowVersion`. Rechaza modificaciones de disponibilidad si el asiento tiene una reserva activa o si el vuelo ya finalizó o fue cancelado.
- `GET /api/admin/vuelos/{id}/reservas`: consulta el manifiesto de pasajeros y reservas asignadas al vuelo, con datos del usuario titular, montos y asientos asignados.


### Implementación administrativa: catálogos

#### Importación de aeropuertos

`POST /api/admin/aeropuertos/importacion/validar` y `POST /api/admin/aeropuertos/importacion` reemplazan el alta, edición y desactivación manuales de aeropuertos. Solo admin, multipart/form-data con el CSV airports original (UTF-8, coma), filtros `soloServicioRegular` (true), `paisIso` opcional y `omitirIncompletos` (false), y paginación de la vista previa. Se mantienen GET listado/detalle. La confirmación importa toda la selección, no únicamente la página visible.

Se usan name→nombre, municipality→ciudad, iso_country→pais (código ISO) e iata_code→codigo_iata; también se persisten ID de origen, identificador, tipo, coordenadas, elevación, continente/región, servicio regular, ICAO/GPS/local, enlaces y palabras clave. DatosOrigenJson conserva los valores originales de todas las columnas. Los metadatos se exponen en GET y vista previa; no se completan automáticamente los de aeropuertos ya existentes. Solo aeropuertos pequeños/medianos/grandes; tipos no admitidos y registros sin IATA se excluyen con conteo explícito. Datos incompletos bloquean salvo autorización expresa de omisión; nunca inventar ciudad ni país. Máximos: 20 MiB de archivo, 150000 registros y 20000 importables; respuesta de hasta 200 filas por página y 200 errores con total global.

Los IATA existentes, incluidos inactivos, no se sobrescriben. Si todos existen, responder ya_importados sin escrituras; coincidencias parciales bloquean toda la carga. Revalidar dentro del bloqueo administrativo compartido y guardar todos los lotes en una sola transacción, verificando lectura exacta de todos los datos antes del commit. Cualquier fallo revierte el lote completo. La ampliación autorizada AeropuertosDatosCompletos añade metadatos de las 19 columnas CSV, conserva originales en datos_origen_json y amplía nombre/ciudad/pais a nvarchar. Las columnas nuevas son nullable para registros previos. No se ejecuta la migración ni una carga automática. Guía y ejemplos: IMPORTACION_AEROPUERTOS.md e ImportacionAeropuertos.http.

#### Importación de flota autorizada

Configuración → Cargar aviones se apoya en `POST /api/admin/aviones/importacion/validar` (vista previa sin escrituras) y `POST /api/admin/aviones/importacion` (confirmación atómica), exclusivos de admin. Reciben multipart/form-data con CSV UTF-8 separado por ; y los IDs de tres categorías activas y distintas. Máximos: 1 MiB de archivo, 500 aviones y 100000 asientos; cada avión conserva el límite de 1000 asientos.

Si todas las matrículas ya existen, incluidos aviones inactivos, responder `ya_importados` y «Estos aviones ya se importaron o sus matrículas ya están registradas. No se creó ningún registro.». Si solo algunas existen, rechazar toda la carga con conflicto y errores por fila. Validar duplicados internos, capacidad y suma de clases, longitudes, letras y encabezados. Revalidar bajo el bloqueo administrativo compartido antes de guardar; reutilizar la generación individual de asientos y revertir todo ante fallos. No sobrescribir, reactivar ni eliminar aeronaves existentes.

La importación es la única vía de alta de aviones; POST /api/admin/aviones fue retirado y no aparece en Swagger. Solo se conservan consultas e importación de aviones. PUT /api/admin/aviones/{id} y POST /api/admin/aviones/{id}/desactivacion también fueron retirados. Flutter debe reemplazar el formulario de alta por la carga. La ampliación AvionesDatosCompletos guarda IATA/ICAO, cantidades por clase, letras y JSON de los nueve valores originales; modelo pasa a nvarchar. Las siete columnas nuevas son nullable para conservar registros previos y los GET devuelven metadatos. La generación de asientos se conserva y no se sobrescriben aviones existentes. No se agrega catálogo de modelos ni tabla de importaciones, ni se ejecuta la migración o se instalan paquetes automáticamente. Contratos, estados y pantalla Flutter: `IMPORTACION_AVIONES.md`; ejemplo: `ImportacionAviones.http`.

Los catálogos /api/admin/aeropuertos, /api/admin/aviones, /api/admin/categorias-asiento y /api/admin/asientos-fisicos requieren exclusivamente admin y ofrecen solo GET listado y GET /{id}. Aeropuertos y aviones tienen además sus importaciones. No se exponen altas, ediciones ni desactivaciones manuales de estos catálogos. Categorías mantiene los registros existentes de Primera, Segunda y Tercera Clase para consulta y selección por ID; no se insertan ni modifican automáticamente. Las tres deben existir y estar activas antes de importar aviones.

Los listados tienen `buscar`, `activo` (por defecto true), `incluirInactivos` (true incluye ambos estados), `pagina` y `tamanoPagina` (1 a 100). Asientos físicos admite además `avionId`. El detalle permite consultar inactivos. No se ofrecen eliminación física ni reactivación. Los códigos IATA, matrículas y códigos de asiento se normalizan a mayúsculas; se conservan las restricciones únicas existentes, también para elementos inactivos.

La migración incremental `CatalogosAdministrativos` añade `activo BIT NOT NULL DEFAULT 1`, `fecha_desactivacion DATETIME2 NULL` y `row_version ROWVERSION` en los cuatro catálogos. Los registros existentes permanecen activos. La API devuelve RowVersion en Base64; se omite al crear y se exige al editar o desactivar. Una versión antigua devuelve 409. No hay filtros globales nuevos que oculten relaciones históricas.

Reglas de protección:

- No se cambian los datos de aeropuertos, aviones o categorías que ya participan en vuelos, incluso históricos; un asiento físico ya asignado a un vuelo tampoco admite cambios de avión, categoría o código. Las escrituras sin cambios de datos sí admiten una nueva versión.
- La capacidad de un avión debe ser positiva y no menor que sus asientos físicos activos. Crear un asiento requiere avión y categoría activos; el número de asientos activos no puede superar la capacidad.
- La importación crea cada avión y exactamente su capacidad de asientos físicos. Las clases deben estar activas, sus cantidades sumar la capacidad y los códigos de asiento se generan con las letras elegidas o números consecutivos. El precio corresponde al vuelo.
- No se altera la distribución de asientos de un avión con vuelos operativos. Tampoco se desactivan aeropuertos, aviones o asientos asociados a vuelos operativos. Se considera operativo todo vuelo que no esté finalizado ni cancelado, aunque su horario ya haya pasado.
- Una categoría con asientos físicos activos no puede desactivarse. No se ofrecen endpoints para reasignar o desactivar asientos físicos, ni para editar/desactivar aviones; los registros históricos inactivos siguen disponibles para consulta.
- Una nueva programación de vuelo exige avión y aeropuertos activos y todos los asientos físicos activos, con categorías activas, hasta completar la capacidad. Los vuelos ya existentes conservan sus referencias e historial.
- Las transacciones de catálogos y vuelos administrativos comparten un bloqueo SQL de aplicación exclusivo, con propietario Transaction y espera máxima de diez segundos. Esto serializa escrituras administrativas y evita carreras entre validar dependencias y crear/programar vuelos. Las reservas mantienen sus bloqueos por vuelo.

Ejemplos: `CatalogosAdmin.http`. Debe aplicarse manualmente la migración antes de iniciar esta versión de la API; también afecta las lecturas de vuelos porque sus entidades relacionadas incorporan columnas nuevas. Ver `Data/CATALOGOS_ADMIN.md`.

Usar `System.Security.Cryptography.RandomNumberGenerator` para generar 32 bytes y convertirlos en hexadecimal. El token tendrá 64 caracteres; será opaco y no contendrá datos del usuario.

Referencia solicitada para la emisión:

```csharp
UsuarioId = usuario.Id,
Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
FechaCreacion = ahora.DateTime,
FechaExpiracion = expiracion.DateTime,
Activo = true
```

Preferir fechas UTC con DateTimeOffset (`ahora = DateTimeOffset.UtcNow`) y persistirlas de manera consistente. Si el modelo usa DateTime, emplear UtcDateTime en lugar de DateTime al convertir. Definir `expiracion` sumando una duración configurable positiva a `ahora`; no inventar una duración definitiva. Documentar la clave de configuración y dejar su valor a elección del equipo.

Para este laboratorio se utilizará la tabla `Tokens` y se almacenará el token aleatorio en su columna `Token`, conforme al diseño solicitado. Entregar el token únicamente al iniciar sesión y no incluirlo en listados ni logs. No sustituir esta columna por TokenHash ni agregar otra estrategia de autenticación sin una decisión posterior del equipo.

Recibirlo mediante `Authorization: Bearer <token>`. En cada operación protegida comprobar que el token exista, esté activo, no haya expirado y pertenezca a un usuario no eliminado lógicamente. Identificar al cliente desde esa sesión, nunca desde un UsuarioId enviado por Flutter. Implementar autenticación integrada con el mecanismo de ASP.NET Core y documentarla en Swagger como HTTP Bearer sin formato JWT.

Cerrar sesión desactiva el token actual sin eliminarlo. No implementar renovación automática ni refresh tokens sin un requisito posterior. Permitir varias sesiones vigentes y revocar únicamente la actual al cerrar sesión.

Usar un mecanismo de hash adaptativo para contraseñas, como PasswordHasher. No exponer hashes, tokens persistidos ni campos internos en DTOs.

## 9. Modelo de datos

La base de datos ya tiene un script SQL de referencia. Crear los modelos y sus mapeos a partir de ese esquema, conservando tablas, columnas, tipos, longitudes, nulabilidad, claves y restricciones. Los pasajeros están incluidos en `detalle_reservas`; no crear una tabla Pasajeros separada. Ver la sección 15 para los detalles y ampliaciones autorizadas.

Separar el asiento físico de su disponibilidad por vuelo. Un asiento puede estar ocupado en un vuelo y disponible en otro. Los detalles confirmados conservarán el precio aplicado. Usar decimal con precisión explícita para importes y definir una moneda única para los datos del proyecto.

Conservar nombres legibles y relaciones explícitas. Evitar modelos genéricos o herencias innecesarias. Separar el estado de la reserva, el estado operativo del vuelo, la disponibilidad del asiento y la marca de eliminación lógica.

## 10. Concurrencia con RowVersion

Usar columnas SQL Server `rowversion`, representadas por `byte[] RowVersion` y configuradas como tokens de concurrencia generados por la base de datos, en las entidades modificables cuya concurrencia deba protegerse, especialmente reservas y asientos por vuelo.

RowVersion detecta cambios concurrentes; no es una fecha, un historial ni el versionado del código o de la API. No asignar su valor manualmente.

Exponer RowVersion como Base64 en los DTOs necesarios y exigir la versión original en modificaciones y cancelaciones. Comprobar esa versión en la escritura, devolver la nueva al completar y responder 409 si la información quedó desactualizada. No sobrescribir silenciosamente ni reintentar confirmaciones de negocio automáticamente.

La reserva completa se confirma dentro de una transacción. Actualizar condicionalmente los asientos por vuelo disponibles usando su versión y validar las filas afectadas. Agregar una restricción o índice único filtrado que impida más de una asignación vigente para el mismo asiento por vuelo. RowVersion por sí solo no evita dos inserciones simultáneas de detalles.

Si dos clientes intentan reservar el mismo asiento, solo uno debe completar la operación; el otro recibirá 409 y ningún cambio parcial. Al cambiar varios asientos, liberar anteriores, asignar nuevos, ajustar detalles e historial en la misma transacción. Un fallo conservará la reserva original.

## 11. Eliminación lógica

No ejecutar DELETE físico de registros de negocio. Agregar mediante migración `eliminado BIT NOT NULL DEFAULT 0` y `fecha_eliminacion DATETIME2 NULL` a `usuarios`, mapeados como `Eliminado` y `FechaEliminacion`; en las demás entidades usar sus estados de negocio y añadir marcas de eliminación solo cuando exista una operación que las necesite. Usar estas marcas, aplicar filtros en consultas normales y conservar relaciones e historial.

Cancelar una reserva significa cambiar su estado a Cancelada, registrar fecha e historial y liberar las asignaciones vigentes; debe seguir apareciendo en el historial del cliente. No ocultarla mediante la marca de eliminación. Revocar una sesión significa `Activo = false`. Estas acciones representan estados de negocio distintos.

Mantener el correo único incluso si una cuenta se desactiva o elimina lógicamente. No implementar restauración o reutilización de cuentas sin una regla explícita.

## 12. Reglas del negocio

- Cada cliente consulta y gestiona únicamente sus reservas.

- Los correos se normalizan y deben ser únicos; las contraseñas se almacenan con hash seguro.

- Origen y destino deben ser diferentes; la llegada programada debe ser posterior a la salida.

- Usar fechas UTC en persistencia y contratos con zona explícita. La interpretación de la fecha de búsqueda debe documentarse según la información horaria disponible; ver sección 15.6.

- Solo se reservan vuelos programados o retrasados que no hayan iniciado el embarque y cuya salida programada sea futura.

- Cada pasajero tiene un asiento; todos los asientos pertenecen al mismo vuelo y no se repiten dentro de una reserva.

- Validar de nuevo disponibilidad y tarifa al confirmar. El servidor calcula el importe; no confía en totales enviados por el móvil.

- Si el total esperado por el cliente difiere del vigente, devolver 409 sin confirmar; informar la tarifa actual para solicitar una nueva confirmación.

- Toda reserva tiene un código único respaldado por una restricción de base de datos.

- Una operación fallida no deja pasajeros, reservas ni asignaciones parciales.

- Cancelar libera los asientos y conserva la reserva y su historial. Una cancelación repetida no duplica liberaciones ni eventos.

- Modificar o cancelar solo se permite antes del inicio del embarque y de la salida programada. Verificar estas condiciones dentro de la operación transaccional.

- No modificar reservas canceladas ni de vuelos en embarque, en vuelo, finalizados o cancelados.

- Al cambiar asientos, conservar los precios de los detalles sin cambios y aplicar la tarifa vigente a los nuevos; confirmar el total esperado antes de guardar.

- Los valores persistidos de estado de vuelo son exactamente `programado`, `embarcando`, `retrasado`, `en vuelo`, `finalizado` y `cancelado`, respetando el CHECK existente. Su actualización administrativa se realizará mediante endpoints protegidos para el rol admin, conservando el historial de cambios.

## 13. Pruebas y criterios de aceptación

Swagger UI será la herramienta principal para probar manualmente cada endpoint, con ejemplos y el botón de autorización Bearer. No sustituye la verificación de integridad y concurrencia.

Preparar instrucciones reproducibles para verificar:

1. Registro válido, correo duplicado y campos inválidos.

2. Login correcto e incorrecto; rechazo de token inválido, expirado y revocado.

3. Búsquedas de vuelos y consulta de asientos.

4. Reserva con varios pasajeros, código único e importe calculado por el servidor.

5. Rechazo de accesos a reservas ajenas.

6. Conflicto de versión sin sobrescribir datos.

7. Dos solicitudes simultáneas por un asiento: una reserva exitosa y un conflicto, sin duplicidad.

8. Fallo durante una reserva o cambio de asientos: reversión completa.

9. Cancelación que libera disponibilidad y conserva historial.

10. Persistencia de los precios originales y rechazo de cambios de tarifa no confirmados.

11. Ausencia de borrados físicos y aplicación correcta de filtros lógicos.

No afirmar que una prueba simultánea se verificó ejecutando solicitudes consecutivas. Si requiere una herramienta o dependencia ausente, preparar el procedimiento e indicar que está pendiente.

## 14. Entrega esperada

Entregar código organizado, contratos OpenAPI/Swagger, scripts de inicialización, migraciones preparadas para ejecución manual, diagrama de arquitectura, instrucciones de configuración y pruebas, y lista de pendientes.

Al finalizar la implementación, indicar explícitamente qué dependencias debe instalar el usuario. Candidatas según el proyecto existente: proveedor SQL Server de EF Core, herramientas de diseño de EF Core, herramienta dotnet-ef si se utilizará y una solución compatible para Swagger UI/OpenAPI, por ejemplo Swashbuckle.AspNetCore. No instalar ni fijar versiones. Diferenciar paquetes necesarios de herramientas opcionales y no agregar soluciones de Swagger duplicadas. RandomNumberGenerator pertenece a la biblioteca de .NET y no requiere un paquete adicional.

Si no se puede compilar, generar migraciones o ejecutar pruebas sin dependencias, explicar el motivo y dejar los pasos manuales correspondientes. No presentar tareas pendientes como completadas.


## 15. Base de datos existente y ampliaciones autorizadas

### 15.1. Fuente de verdad para los modelos

El proyecto ya dispone del script SQL de la base `Vuelos`. Ese script y la estructura real de la base son la referencia para generar las entidades y configurar Entity Framework Core. No diseñar otra base desde cero ni reemplazar datos existentes.

La siguiente relación resume el esquema aportado. Todas las claves primarias son INT IDENTITY(1,1); los campos son NOT NULL salvo donde se indica NULL. Configurar explícitamente los nombres SQL mediante Fluent API, aunque las propiedades C# usen PascalCase.

| Tabla | Columnas existentes |
| --- | --- |
| usuarios | usuario_id INT PK; nombre VARCHAR(100); apellido VARCHAR(100); email VARCHAR(150); password_hash VARCHAR(255); rol VARCHAR(20); fecha_creacion DATETIME2 DEFAULT GETDATE() |
| aeropuertos | aeropuerto_id INT PK; codigo_iata CHAR(3); nombre VARCHAR(150); ciudad VARCHAR(100); pais VARCHAR(100) |
| aviones | avion_id INT PK; modelo VARCHAR(100); matricula VARCHAR(50); capacidad_total INT |
| categorias_asiento | categoria_id INT PK; nombre VARCHAR(50); descripcion VARCHAR(255) NULL |
| asientos_fisicos | asiento_fisico_id INT PK; avion_id INT FK; categoria_id INT FK; codigo_asiento VARCHAR(10) |
| vuelos | vuelo_id INT PK; numero_vuelo VARCHAR(20); avion_id INT FK; aeropuerto_origen_id INT FK; aeropuerto_destino_id INT FK; salida_programada DATETIME2; llegada_programada DATETIME2; salida_real DATETIME2 NULL; llegada_real DATETIME2 NULL; estado VARCHAR(20) DEFAULT 'programado'; fecha_creacion DATETIME2 DEFAULT GETDATE() |
| historial_estados_vuelo | historial_id INT PK; vuelo_id INT FK; estado_anterior VARCHAR(20) NULL; estado_nuevo VARCHAR(20); fecha_cambio DATETIME2 DEFAULT GETDATE(); usuario_admin_id INT FK; observacion VARCHAR(255) NULL |
| asientos_vuelo | asiento_vuelo_id INT PK; vuelo_id INT FK; asiento_fisico_id INT FK; precio DECIMAL(10,2); estado VARCHAR(20) DEFAULT 'disponible' |
| reservas | reserva_id INT PK; codigo_reserva VARCHAR(10); usuario_id INT FK; vuelo_id INT FK; fecha_reserva DATETIME2 DEFAULT GETDATE(); monto_total DECIMAL(10,2) DEFAULT 0.00; estado VARCHAR(20) DEFAULT 'confirmada' |
| detalle_reservas | detalle_id INT PK; reserva_id INT FK; asiento_vuelo_id INT FK; primer_nombre VARCHAR(100); segundo_nombre VARCHAR(100) NULL; primer_apellido VARCHAR(100); segundo_apellido VARCHAR(100) NULL; documento_identidad VARCHAR(50); precio_congelado DECIMAL(10,2); estado VARCHAR(20) DEFAULT 'activo' |
| historial_cambios_reserva | historial_reserva_id INT PK; reserva_id INT FK; asiento_vuelo_anterior_id INT FK NULL; asiento_vuelo_nuevo_id INT FK NULL; tipo_accion VARCHAR(50); fecha_accion DATETIME2 DEFAULT GETDATE(); observacion VARCHAR(255) NULL |

Conservar las restricciones del script original:

- Unicidad: usuarios.email, aeropuertos.codigo_iata, aviones.matricula, categorias_asiento.nombre, asientos_fisicos(avion_id, codigo_asiento), asientos_vuelo(vuelo_id, asiento_fisico_id) y reservas.codigo_reserva.
- usuarios.rol: únicamente `cliente` y `admin`.
- aviones.capacidad_total > 0; asientos_vuelo.precio >= 0.
- vuelos: origen diferente de destino y llegada programada posterior a salida programada.
- vuelos.estado: `programado`, `embarcando`, `retrasado`, `en vuelo`, `finalizado`, `cancelado`.
- asientos_vuelo.estado: `disponible`, `reservado`, `bloqueado`.
- reservas.estado: `confirmada`, `cancelada`, `modificada`.
- detalle_reservas.estado: `activo`, `liberado`.
- Todas las relaciones apuntan a las claves de las tablas indicadas por sus nombres. En particular, ambas relaciones de aeropuerto apuntan a aeropuertos.aeropuerto_id; usuario_admin_id apunta a usuarios.usuario_id; los asientos anterior y nuevo del historial apuntan a asientos_vuelo.asiento_vuelo_id.

El SQL original no declara CHECK para tipo_accion; la aplicación utilizará `CAMBIO_ASIENTO` y `CANCELACION`. No presentar esas convenciones como restricciones ya existentes.

Si hay diferencias entre este resumen, el script proporcionado y la base real, documentarlas antes de generar cambios destructivos. Mantener los nombres originales de PK, FK, UQ y CHECK al mapear o reconstruir el esquema.

El esquema conserva el rol `admin` y la tabla `historial_estados_vuelo`. Crear sus modelos, relaciones y funciones administrativas con autorización por rol admin. El registro público asignará siempre `cliente`; nunca aceptará el rol desde el cuerpo de la solicitud.

No agregar tablas de pasajeros. La ampliación autorizada `pagos_paypal` registra pagos y `eventos_paypal` deduplica notificaciones; `compras_simuladas` se conserva únicamente como histórico. `precio_congelado` representa el importe pactado para el pasajero; la confirmación de una reserva temporal no implica una compra ni un cobro real.

### 15.2. Tabla Tokens para el laboratorio

Agregar esta tabla mediante una migración incremental si todavía no existe. La FK propuesta originalmente como `Usuarios(Id)` debe corregirse a `usuarios(usuario_id)`, que es la tabla y clave reales. La propiedad C# puede llamarse Id o UsuarioId según corresponda, siempre con mapeo explícito.

```sql
CREATE TABLE dbo.Tokens (
    Id INT IDENTITY(1,1) NOT NULL,
    UsuarioId INT NOT NULL,
    Token NVARCHAR(200) NOT NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT GETUTCDATE(),
    FechaExpiracion DATETIME NOT NULL,
    Activo BIT NOT NULL DEFAULT 1,
    CONSTRAINT PK_Tokens PRIMARY KEY (Id),
    CONSTRAINT FK_Tokens_usuarios FOREIGN KEY (UsuarioId)
        REFERENCES dbo.usuarios(usuario_id),
    CONSTRAINT UQ_Tokens_Token UNIQUE (Token),
    CONSTRAINT CK_Tokens_Fechas CHECK (FechaExpiracion > FechaCreacion)
);
```

Este SQL es una propuesta de ampliación, no una afirmación de que la tabla ya esté creada. Se asume esquema dbo; verificar el esquema real antes de generar la migración. Si Tokens ya existe, comparar su definición y preparar solo los cambios faltantes, sin recrearla ni borrar sesiones.

FechaCreacion y FechaExpiracion usarán UTC. La aplicación enviará ambas fechas explícitamente y la duración se leerá de configuración. Mantener DATETIME en esta tabla conforme al laboratorio, sin convertir automáticamente las columnas del esquema original.

Generar el token con 32 bytes criptográficamente aleatorios y representación hexadecimal. Respetar la columna Token del laboratorio; el almacenamiento de un hash podrá evaluarse posteriormente sin cambiar este requisito por iniciativa de Codex.

### 15.3. Concurrencia pendiente en el SQL original

El script original NO contiene columnas rowversion ni el índice de asignación activa. Preparar una migración incremental que añada, como mínimo:

```sql
ALTER TABLE dbo.reservas
    ADD row_version ROWVERSION NOT NULL;

ALTER TABLE dbo.asientos_vuelo
    ADD row_version ROWVERSION NOT NULL;

CREATE UNIQUE INDEX UX_detalle_reservas_asiento_activo
    ON dbo.detalle_reservas (asiento_vuelo_id)
    WHERE estado = 'activo';
```

Mapear cada row_version a `byte[] RowVersion` con IsRowVersion(). SQL Server generará automáticamente los valores para las filas existentes y las futuras.

Antes de crear el índice, comprobar si existen varias filas activas para un mismo asiento_vuelo_id. Si hay inconsistencias, informarlas sin borrar ni corregir datos automáticamente. El índice permite conservar detalles liberados y que el asiento vuelva a reservarse posteriormente.

El cambio de estado operativo de un vuelo también puede competir con una reserva: dentro de la transacción de confirmación, modificación o cancelación, proteger la lectura y validación del vuelo hasta completar la escritura mediante una estrategia de bloqueo o aislamiento adecuada y documentada. Las dos columnas rowversion anteriores no detectan por sí solas cambios en la fila del vuelo.

La asignación de asientos debe seguir protegida por actualización condicional, transacción e índice único. Liberar el detalle anterior antes de insertar el nuevo cuando el índice lo requiera, siempre dentro de la misma transacción. No guardar estados parciales si se produce una excepción.

### 15.4. Borrado lógico pendiente en el SQL original

El esquema original tampoco incluye marcas genéricas de eliminación. Preparar una ampliación explícita para usuarios:

```sql
ALTER TABLE dbo.usuarios ADD
    eliminado BIT NOT NULL
        CONSTRAINT DF_usuarios_eliminado DEFAULT 0,
    fecha_eliminacion DATETIME2 NULL;
```

Aplicar el filtro de usuario no eliminado en autenticación. No agregar estos campos indiscriminadamente a todas las tablas. Para reservas y detalles usar `cancelada` y `liberado`; para Tokens usar Activo. Ninguna operación de la API eliminará físicamente filas.

### 15.5. Datos de prueba mediante migración

Preparar una migración de datos separada que cree dos cuentas de laboratorio:

| Nombre | Apellido | Email | Rol |
| --- | --- | --- | --- |
| Administrador | Nexus | admin@pacificnexus.test | admin |
| Josue | Laboratorio | josue@pacificnexus.test | cliente |

Los nombres son datos ficticios. Las contraseñas serán exclusivas de pruebas y se comunicarán en las instrucciones locales de ejecución. Guardar únicamente sus hashes, compatibles con el mismo verificador utilizado al iniciar sesión; no insertar contraseñas en texto plano en password_hash.

Si se generan hashes antes de escribir la migración, generarlos una sola vez y mantenerlos estables; no calcular valores aleatorios en HasData o durante la construcción del modelo. No instalar dependencias para generarlos sin autorización.

La migración comprobará si el correo ya existe y no reemplazará datos, roles ni contraseñas de cuentas existentes. La cuenta admin permite probar los endpoints administrativos protegidos; las interfaces se desarrollarán por separado. No crear tokens de prueba permanentes: cada cuenta debe iniciar sesión para obtener un token vigente.

Mantener la migración de datos de laboratorio identificada y separada para no aplicarla por accidente fuera del entorno de pruebas. No ejecutar ninguna migración automáticamente.

### 15.6. Integración con una base ya creada

Antes de preparar migraciones, verificar si existe historial de migraciones de EF Core y si las tablas del SQL ya están creadas. Si la base existe sin historial de EF, preparar una estrategia de línea base que registre el modelo actual sin volver a ejecutar CREATE TABLE sobre las tablas existentes. No marcar cambios pendientes como ya aplicados.

Separar claramente:

1. Modelo y mapeos del esquema original.
2. Línea base, únicamente si hace falta.
3. Ampliaciones: Tokens, RowVersion, índice de asignación activa y marcas de borrado lógico de usuarios.
4. Migración con usuarios de laboratorio.

Conservar los datos existentes y documentar el orden de ejecución manual. Los scripts anteriores son referencias para generar las migraciones, no deben ejecutarse además de ellas sobre la misma base.

Los DEFAULT GETDATE() del esquema original no garantizan UTC. No reinterpretar fechas históricas como UTC sin conocer su zona de origen. Para nuevos registros la API enviará fechas UTC explícitas; documentar la convención de fechas de los datos precargados. El esquema actual de aeropuertos no tiene zona horaria: no inventar una columna existente ni prometer búsqueda local por aeropuerto sin definir primero esa información.

## Reservas y tipos de pasajeros

Un usuario autenticado podrá realizar múltiples reservas para diferentes vuelos, sujeto a disponibilidad. Cada reserva corresponderá a un solo vuelo y podrá incluir uno o varios pasajeros.

El titular de la cuenta no estará obligado a viajar. Las reservas pertenecerán al usuario que las realizó, quien podrá consultarlas y gestionarlas.

### Datos de cada pasajero

Al realizar una reserva se solicitará:

- Cédula, almacenada en el campo existente `documento_identidad`.
- Primer nombre y segundo nombre.
- Primer apellido y segundo apellido.
- Tipo de pasajero.
- Asiento seleccionado.

Los nombres y apellidos se guardarán por separado en primer_nombre,
segundo_nombre, primer_apellido y segundo_apellido, con un máximo de 100
caracteres cada uno. El segundo nombre y segundo apellido son opcionales.
La migración NombresPasajerosSeparados conserva las columnas antiguas
renombrándolas a los primeros campos, sin adivinar cómo dividir nombres
compuestos de registros anteriores.

Para este laboratorio no se solicitará ni almacenará la fecha de nacimiento. El usuario seleccionará el tipo de pasajero y la API validará que sea uno de los valores permitidos, sin calcular ni comprobar su edad.

| Valor | Tipo de pasajero | Referencia para la selección |
| --- | --- | --- |
| `adulto` | Adulto | Desde los 15 años |
| `joven` | Joven | De 12 a 14 años |
| `nino` | Niño | De 2 a 11 años |
| `bebe` | Bebé | Menor de 2 años |

Todos los pasajeros, incluidos los bebés, tendrán un asiento propio para simplificar el alcance del laboratorio.

### Tarifas por tipo de pasajero

El precio dependerá del vuelo, del asiento seleccionado y del tipo de pasajero. Las tarifas o descuentos serán configurables; Codex no deberá inventar importes ni porcentajes.

Valores aprobados por el equipo: adulto y joven pagan el 100 % del precio base del asiento; niño (`nino`) paga el 80 % (descuento del 20 %) y bebé (`bebe`) paga el 75 % (descuento del 25 %). Se configuran en `TarifasPasajero:Porcentajes` de `appsettings.json`. Los valores representan el porcentaje que se paga, no el porcentaje de descuento. Se aplican sobre la clase/asiento elegido y se redondean a dos decimales. Reiniciar la API tras cambiar esta configuración para cargar los valores. No se recalculan precios congelados de reservas ya existentes.

La API calculará el importe de cada pasajero y el total de la reserva. No confiará en los precios enviados por la aplicación móvil.

Al confirmar, se validarán nuevamente las tarifas y la disponibilidad. El importe aplicado se conservará en `precio_congelado`, evitando que cambios posteriores alteren reservas existentes.

### Consulta de cédula para autocompletar

`GET /api/pasajeros/consulta-cedula/{cedula}` requiere sesión y envía la cédula
a EcuadorLegal mediante POST con los campos `name` y `tipo=1`. Lee `ciN/nameN`
del HTML, o `name0` si solo se devuelve esa celda. Retorna los cuatro campos
sugeridos y `requiereRevision=true`, tomando los últimos dos términos como
apellidos. El cliente puede corregirlos antes de guardar; si no hay datos,
continúa con ingreso manual. No modifica la base ni requiere migración.


## Flutter web y CORS

En Development, el origen inicial permitido es http://localhost:62226. Se puede reemplazar mediante Cors:OrigenesPermitidos, sin rutas ni comodines. No se usa AllowAnyOrigin ni se habilitan cookies cross-origin. Se admiten encabezados Bearer/JSON y los métodos HTTP, manteniendo autorización por endpoint. La política se ejecuta tras routing y antes de autenticación/autorización. El desarrollo local admite HTTP; fuera de Development se conserva la redirección HTTPS y se exige configurar los orígenes explícitamente. Detalles y prueba manual: CORS_FLUTTER_WEB.md. No requiere migraciones ni paquetes.
## Facturación del laboratorio sin SRI: etapa 1, importes

Las reservas nuevas aplican descuentos de pasajero, guardan subtotal neto, porcentaje_iva (15 inicialmente, configurable en Reservas:PorcentajeIva), monto_iva redondeado globalmente a dos decimales AwayFromZero y monto_total final. PayPal cobra ese total, no el subtotal. Se congelan tasa e importes; cambiar configuración no modifica reservas anteriores y cambios de asiento mantienen la tasa guardada. TotalEsperado debe confirmar el total final con IVA.

La migración DesgloseIvaReservas añade tres columnas nullable y CHECK de consistencia, sin convertir importes anteriores. NULL en las tres identifica históricos sin desglose; no se infiere IVA cero ni se recobran impuestos retroactivos. La captura verifica detalle neto, IVA, importe de reserva y dinero confirmado. Las respuestas propias, administrativas de reserva y PayPal incluyen el desglose; ventas sigue siendo bruto capturado.

La etapa no implementa todavía factura, XML, SOAP ni correo. La siguiente etapa usará esos importes congelados para emitir un comprobante XML del laboratorio sin SRI, guardarlo y enviarlo al email registrado, sin duplicar pagos ni facturas. Detalles, configuración y migración manual: IVA_RESERVAS.md. No se ejecuta la migración ni se instalan dependencias automáticamente.
