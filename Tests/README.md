# Pruebas de Pacific Nexus Airways: PostgreSQL/Supabase

Guia actual: [SUPABASE_ETAPA_5.md](../docs/SUPABASE_ETAPA_5.md).

## Suite completa offline

Desde la raiz del proyecto:

```powershell
$script = Get-Content Tests/VerificarModeloPostgreSql.ps1 -Raw -Encoding UTF8
& ([scriptblock]::Create($script)) -ProjectRoot $PWD.Path -Comprobacion Completa
```

Resultado verificado: **924 comprobaciones aprobadas**. Incluye catalogos, importaciones CSV, vuelos, autenticacion/autorizacion HTTP de los fixtures, propiedad de reservas, expiracion, historial, versiones, ventas, descuentos, IVA, Swagger, CORS, modelo PostgreSQL y migracion. El proveedor de pago y los repositorios HTTP son simulados. No abre una base ni realiza cobros reales.

Las comprobaciones de estructura anteriores se adaptaron al modelo y migracion PostgreSQL actual. Se conservan Unicode, nulabilidad historica, FK, restricciones, ausencia de borrado fisico y proteccion del historial. Las fechas esperadas respetan la precision de microsegundos de PostgreSQL.

`VerificarVuelosAdmin.ps1` conserva el comando antiguo como alias de esta suite completa. Las rutas de CSV/configuracion se resuelven desde el proyecto/ensamblado y no dependen del directorio de lanzamiento.

Tambien se pueden ejecutar bloques independientes: `Modelo`, `Migracion` y `Persistencia`. Se compila con `--no-restore` en `obj/verificacion-supabase`; no se instalan ni restauran paquetes. La compilacion de la API es limpia; el compilador Roslyn de las pruebas conserva CS1702 sobre unificacion de referencias EF ya documentada.

`-Comprobacion UsuariosLaboratorio` verifica por separado los hashes, roles y protecciones del script opcional de cuentas de ejemplo; no lo ejecuta en la base. Instrucciones: [cuentas de laboratorio](../docs/SUPABASE_USUARIOS_LABORATORIO.md).

## Integracion con base real: opt-in

`-Comprobacion Integracion` requiere una base exclusiva de pruebas ya migrada, permisos runtime de etapa 4, `PNA_TEST_CONNECTION` y `PNA_TESTS_CONFIRM=BASE_DE_PRUEBAS`. Crea y conserva registros nuevos de laboratorio, sin modificar registros anteriores ni realizar DDL. El proveedor de pago sigue siendo simulado; no prueba PayPal Sandbox real.

```powershell
& ([scriptblock]::Create($script)) -ProjectRoot $PWD.Path -Comprobacion Integracion -SoloCompilar
```

La compilacion de ese ensayo esta verificada. No se ejecuto contra una base; se comprobo que, sin conexion/confirmacion, devuelve NO_EJECUTADA y no abre conexiones. No confundir ese estado con una prueba aprobada.

Instrucciones de ejecucion, efectos y pendientes: [etapa 5](../docs/SUPABASE_ETAPA_5.md). Instalacion y permisos manuales: [etapa 4](../docs/SUPABASE_ETAPA_4.md). La prueba del flujo HTTP contra la API real y PayPal Sandbox sigue pendiente.
