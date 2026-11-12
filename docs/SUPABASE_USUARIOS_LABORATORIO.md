# Cuentas de ejemplo para Supabase

La migración InicialPostgreSql creó el esquema y las clases de asiento, pero no ejecutó la carga histórica SQL Server `DatosLaboratorioAdminJosue`. Las cuentas se entregan ahora en un script opcional de datos, sin alterar esa migración ni crear otra.

Ejecutar manualmente el contenido completo de `Data/Scripts/Supabase_UsuariosLaboratorio.sql` en SQL Editor de la base de laboratorio. Se insertan únicamente:

| Cuenta | Correo | Rol de la aplicación |
| --- | --- | --- |
| Administrador Nexus | admin@pacificnexus.test | admin |
| Josue Laboratorio | josue@pacificnexus.test | cliente |

Los hashes son los mismos de la carga SQL Server original y se verificaron con PasswordHasher del backend. La contraseña de laboratorio se comunica en las instrucciones de entrega; la columna password_hash nunca recibe texto plano. No se modifica la validación del registro público.

El script no crea roles PostgreSQL como pna_api/pna_runtime. No cambia ni reactiva cuentas existentes, aunque su correo difiera por mayúsculas o espacios. Combina transacción, bloqueo de la carga y ON CONFLICT DO NOTHING para evitar duplicados. El SELECT final devuelve IDs, nombres, correo, rol y estado, sin hashes ni tokens. Si una cuenta ya existía, mantiene su contraseña y rol anteriores.

Después de ejecutarlo, usar `POST /api/autenticacion/login` en Swagger y autorizar el token de la cuenta correspondiente. El usuario admin podrá cargar catálogos y crear vuelos; Josue podrá recorrer el flujo de reservas. Estos correos son ficticios y no reciben correo real.

Verificación offline ejecutada: compilación correcta y 12 comprobaciones de hashes, DTO de login, roles y protecciones del script. No se abrió una conexión ni se ejecutó el script contra Supabase. No hay dependencias adicionales que instalar.

Para repetir la verificación:

```powershell
$script = Get-Content Tests/VerificarModeloPostgreSql.ps1 -Raw -Encoding UTF8
& ([scriptblock]::Create($script)) -ProjectRoot $PWD.Path -Comprobacion UsuariosLaboratorio
```
