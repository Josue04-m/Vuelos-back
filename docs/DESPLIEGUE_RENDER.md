# API en Render y base de datos en Supabase

La base existente permanece en Supabase. Render ejecutará la API .NET 10 mediante el Dockerfile. Este despliegue no crea tablas, usuarios ni ejecuta migraciones.

## 1. Preparar el repositorio

Sube los cambios a la rama que quieras desplegar. Antes de subir, revisa que appsettings.json, launchSettings.json y el historial de Git no contengan secretos reales. .dockerignore evita copiarlos a Docker, pero no evita que Git los publique. Si ya se publicaron secretos, reemplázalos en el proveedor.

El Dockerfile debe construirse con la carpeta que contiene Vuelos_Backend.csproj como contexto. Usa imágenes oficiales .NET 10 y las versiones NuGet declaradas por el equipo. Render descargará las dependencias durante la construcción; no se instalaron ni restauraron paquetes localmente para preparar estos archivos.

## 2. Crear el servicio

En Render: **New → Web Service**, conecta el repositorio y selecciona la rama de Supabase.

- Language: **Docker**.
- Root Directory: carpeta del repositorio que contiene Vuelos_Backend.csproj. Si ya está en la raíz, déjala vacía.
- Dockerfile Path: `./Dockerfile`, relativo a esa carpeta.
- Docker Build Context: `.` si aparece este campo.
- Docker Command: vacío; se usa el comando de la imagen.
- Health Check Path: `/health`.
- Región: preferiblemente cercana a Supabase (actualmente us-east-2).
- Selecciona el plan que corresponda a tu laboratorio antes de crear el servicio.

El contenedor escucha en 0.0.0.0 y utiliza PORT de Render. Sin esa variable utiliza 8080.

## 3. Variables de entorno

En **Environment** agrega estas claves. No pegues secretos en archivos del repositorio ni en argumentos de construcción Docker.

| Clave | Valor |
| --- | --- |
| ASPNETCORE_ENVIRONMENT | Production |
| Hosting__HttpsEnProxy | true |
| Swagger__Habilitado | true para las pruebas del equipo; false para ocultar Swagger |
| Autenticacion__DuracionTokenMinutos | 120, o la duración aprobada por el equipo |
| ConnectionStrings__SupabaseConnection | Cadena Npgsql descrita abajo, con contraseña real |
| Cors__OrigenesPermitidos__0 | http://localhost:62226 para esa ejecución de Flutter web |
| Cors__OrigenesPermitidos__1 | Origen HTTPS real del frontend web, cuando exista; omite esta variable hasta entonces |

Cadena de conexión, en una sola línea:

```text
Host=aws-0-us-east-2.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.feszptvupdetbvpsbmno;Password=TU_PASSWORD;SSL Mode=VerifyFull
```

Usa los datos actuales de **Connect → Session pooler** de tu proyecto. Es una cadena Npgsql, no la URI postgresql://. Si la contraseña contiene delimitadores de cadenas Npgsql, debes entrecomillarla y escapar sus comillas según Npgsql. No agregues Trust Server Certificate para saltar la validación TLS.

No necesitas crear pna_api ni otra base. Mantén la conexión que ya comprobaste, cambiando solo lo necesario para este entorno. launchSettings.json y los secretos de usuario de tu PC no se cargan al ejecutar el DLL en Render.

Los porcentajes adulto/joven=100, niño=80, bebé=75 e IVA=15 están en deploy/appsettings.json. Puedes sobrescribirlos con variables, por ejemplo `Reservas__PorcentajeIva=15`.

## 4. PayPal

Copia la configuración Sandbox que ya funciona a variables de Render:

```text
PayPal__Habilitado=true
PayPal__Entorno=Sandbox
PayPal__ClientId=VALOR_REAL
PayPal__ClientSecret=VALOR_REAL
PayPal__MerchantId=VALOR_REAL
PayPal__WebhookId=ID_DEL_WEBHOOK_REAL
PayPal__ReturnUrl=URL_HTTPS_DE_RETORNO
PayPal__CancelUrl=URL_HTTPS_DE_CANCELACION
```

No utilices los textos de ejemplo como credenciales. Actualiza en PayPal la URL del webhook para apuntar al endpoint existente de esta API usando su dominio público de Render; conserva su ruta y vuelve a configurar el ID si registras otro webhook. Las URL de retorno y cancelación son pantallas del cliente, no el endpoint de captura. Para probar solo el backend puedes usar la URL HTTPS de Swagger como destino y continuar la captura manualmente con el flujo documentado en PAYPAL.md.

No se habilita Live en esta entrega. Si PayPal queda deshabilitado, las consultas normales pueden funcionar pero no podrás completar compras.

## 5. Desplegar y comprobar

Haz **Create Web Service** y revisa los logs de compilación y arranque. Render construye la imagen y asigna una URL como https://tu-servicio.onrender.com.

1. Abre `/health`: debe responder 200 con Healthy. Es disponibilidad HTTP, no verifica la conexión a Supabase ni PayPal.
2. Abre `/swagger/index.html` si habilitaste Swagger.
3. Inicia sesión con una cuenta existente y prueba una consulta autenticada para comprobar Supabase.
4. Comprueba que un cliente recibe 403 en una operación administrativa.
5. Ejecuta una reserva y un pago Sandbox usando el flujo existente, y verifica su persistencia.
6. Cambia API_BASE_URL de Flutter a la URL HTTPS del servicio, sin `/swagger/index.html`. Prueba también desde Flutter web para comprobar el origen CORS exacto.

Render impone HTTPS públicamente. Hosting__HttpsEnProxy evita que ASP.NET vuelva a redirigir la conexión HTTP interna del proxy. Activa esta opción solo detrás de un proxy que imponga HTTPS. No se habilita confianza indiscriminada en cabeceras reenviadas.

El plan Free suspende servicios tras 15 minutos sin tráfico y el siguiente acceso puede tardar alrededor de un minuto. Durante esa suspensión no se ejecutan los trabajadores de liberación de reservas ni conciliación PayPal. Para ejecución periódica continua necesitas un servicio sin suspensión. Los datos persistentes siguen en Supabase; no guardes documentos necesarios únicamente en el sistema de archivos temporal de Render.

No configures migraciones automáticas como comando de inicio o predeploy. Cualquier cambio de esquema seguirá el proceso manual del equipo.

## Verificación local opcional

Solo si tienes Docker instalado y decides descargar imágenes/restaurar paquetes:

```powershell
docker build -t pna-api .
docker run --rm --env-file .env.render.local -p 8080:8080 pna-api
```

Guarda las variables en un archivo local excluido de Git. No incluyas PORT de Render para esa prueba, o ajústalo junto al mapeo de puertos. El archivo .env.render.local está excluido del contexto Docker; verifica además que tu .gitignore lo excluya antes de crearlo.

No hacen falta nuevos paquetes NuGet. Docker Desktop es opcional para probar en tu PC; Render construye remotamente la imagen.

Fuentes: [Docker en Render](https://render.com/docs/docker), [Web Services y PORT](https://render.com/docs/web-services), [HTTPS administrado](https://render.com/docs/tls), [limitaciones de Free](https://render.com/docs/free).
