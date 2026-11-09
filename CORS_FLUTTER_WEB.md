# Conexión de Flutter web

El frontend indicado abre en `http://localhost:62226` y la API en `http://192.168.100.38:5236`. Son orígenes diferentes. La API ahora permite explícitamente el origen del frontend en Development mediante `Configuration/CorsFlutter.cs`.

## Probar el cambio

Reiniciar la API con el perfil local:

```powershell
dotnet run --project .\Vuelos_Backend.csproj --no-restore --launch-profile http --urls "http://0.0.0.0:5236"
```

En Flutter web mantener el puerto del navegador:

```powershell
flutter run -d chrome --web-port 62226
```

La URL base de la API en Flutter debe ser `http://192.168.100.38:5236`, **sin `/swagger/index.html`**. Login usa `/api/autenticacion/login`; Swagger es únicamente la interfaz de pruebas.

En Development se admite HTTP sin redirección automática para el perfil local. Fuera de Development se conserva la redirección HTTPS y no se agrega ningún origen local por defecto.

## Cambiar el origen permitido

La configuración `Cors:OrigenesPermitidos` reemplaza la lista inicial. Ejemplo en un archivo de configuración:

```json
{
  "Cors": {
    "OrigenesPermitidos": ["http://localhost:62226"]
  }
}
```

Si se usa `Properties/launchSettings.json`, agregar dentro de environmentVariables del perfil ejecutado:

```json
"Cors__OrigenesPermitidos__0": "http://localhost:62226"
```

No se modificaron credenciales ni archivos de secretos o launchSettings al implementar CORS. Para varios orígenes, configurar índices 0, 1... con direcciones explícitas. El puerto forma parte del origen: localhost:62227 o 127.0.0.1:62226 son distintos de localhost:62226. Si se fija otra lista, no se conserva automáticamente el valor inicial.

No aceptar comodines ni rutas/query dentro de un origen. No se utiliza AllowAnyOrigin ni se habilitan cookies cross-origin; las solicitudes siguen usando los tokens Bearer actuales. CORS se ejecuta entre routing y autenticación/autorización, de modo que OPTIONS puede completarse sin login y las respuestas de error autorizadas por CORS puedan leerse desde el frontend.

## Verificación y límites

Las pruebas HTTP usan la misma configuración/middleware que Program: origen admitido y denegados, OPTIONS con Authorization/Content-Type, GET con token, 401/403, origen configurable y ausencia del valor local por defecto en producción. No conectan a SQL Server ni prueban la instancia desplegada o el navegador del usuario.

Si persiste el mensaje genérico después de reiniciar, revisar F12 → Console y Network: URL exacta, OPTIONS, código HTTP y encabezado Access-Control-Allow-Origin. La configuración CORS no corrige firewall, API apagada, una IP incorrecta, permisos de red local del navegador o una página HTTPS llamando una API HTTP.

No requiere migraciones ni dependencias nuevas. Referencia: [CORS en ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-10.0).
