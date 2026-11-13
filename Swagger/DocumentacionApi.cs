using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Vuelos_Backend.Swagger;

public static class DocumentacionApi
{
    public static void Configurar(SwaggerGenOptions options)
    {
        options.TagActionsBy(api => [Etiqueta(api)]);
        options.OrderActionsBy(api => $"{Etiqueta(api)}:{api.RelativePath}:{api.HttpMethod}");
        options.OperationFilter<VuelosOperationFilter>();
        options.OperationFilter<SeguridadOperationFilter>();
        options.SchemaFilter<ReservasSchemaFilter>();
        options.SchemaFilter<FlujoSchemaFilter>();
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Pacific Nexus Airways API",
            Version = "v1",
            Description = """
                ### Flujo de prueba del laboratorio

                1. **Autenticación:** iniciar sesión y copiar el token. En **Authorize**, pegar solo el token, sin escribir Bearer.
                2. **Preparación con admin:** consultar/importar aeropuertos con POST /api/admin/aeropuertos/importacion/validar y /importacion; revisar filtros y omisiones antes de confirmar. Consultar el catálogo fijo de categorías (Primera, Segunda y Tercera Clase); solo GET, sin altas ni modificaciones. Copiar sus IDs existentes.
                3. **Aviones:** validar CSV en POST /api/admin/aviones/importacion/validar y confirmar en POST /api/admin/aviones/importacion. La importación es la única vía de alta; genera asientos automáticamente y no duplica matrículas.
                4. **Vuelos (admin):** crear el vuelo con un precio en USD por cada categoría presente en el avión. Los asientos del vuelo se generan automáticamente.
                5. **Ventas (admin):** consultar **GET /api/admin/ventas/resumen** y anotar el total inicial.
                6. **Vuelos:** consultar **GET /api/vuelos/{id}/asientos** y copiar asientoVueloId y rowVersion del asiento disponible.
                7. **Reservas:** crear la reserva con el total final, incluidos descuentos y el IVA del laboratorio (inicialmente 15%). Si devuelve 409 tarifa_actualizada, revisar montoTotalActual y volver a enviar ese total como totalEsperado. Quedará pendiente_pago; copiar reservaId y rowVersion de la reserva.
                8. **PayPal:** POST /api/reservas/{id}/paypal/orden con RowVersion. Abrir UrlAprobacion, aprobar con comprador Sandbox y ejecutar POST /api/reservas/{id}/paypal/captura. Solo COMPLETED acredita el pago. Consultar GET /api/reservas/{id}/paypal ante resultado incierto.
                9. **Ventas (admin):** volver a consultar el resumen. Debe aumentar en el importe del comprobante y una compra. Repetir la compra no debe incrementar el acumulado.

                El admin también puede probar reservas y compras propias con el mismo token. Para probar con un cliente, cambiar el token en Authorize; la reserva y compra deben usar la misma cuenta. Volver al token admin para consultar ventas globales.

                **Antes de probar:** aplicar manualmente las migraciones pendientes; configurar PayPal en secretos de usuario o variables de entorno (ver PAYPAL.md). Configurar los cuatro porcentajes en TarifasPasajero:Porcentajes para poder reservar (sin configuración devuelve 503). El plazo inicial es 720 minutos y se configura en Reservas:DuracionReservaMinutos.

                Los IDs, versiones e importes de los ejemplos son ilustrativos: sustituirlos por los datos reales consultados. El total de ventas es bruto, en USD, de comprobantes guardados; no suma reservas sin compra. Sandbox usa dinero de prueba; Live realiza cobros reales. El resumen separa ambos entornos; los reembolsos requieren gestión externa y revisión. La autenticación usa tokens opacos; no utiliza JWT.
                """
        });
        options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Vuelos_Backend.xml"));
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            Description = "Ingrese únicamente el token recibido al iniciar sesión. Para cambiar de usuario, sustituya el token en Authorize."
        });
        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
    }

    private static string Etiqueta(ApiDescription api) => (api.ActionDescriptor.RouteValues.TryGetValue("controller", out var nombre) ? nombre : null) switch
    {
        "Autenticacion" => "01. Autenticación",
        "Usuarios" => "02. Usuarios",
        "AeropuertosAdmin" => "03. Aeropuertos (admin)",
        "ImportacionAeropuertos" => "03. Aeropuertos (admin)",
        "CategoriasAsientoAdmin" => "04. Clases de asiento (admin)",
        "AvionesAdmin" => "05. Aviones (admin)",
        "ImportacionAviones" => "05. Aviones (admin)",
        "AsientosFisicosAdmin" => "06. Asientos físicos (admin)",
        "VuelosAdmin" => "07. Vuelos (admin)",
        "Vuelos" => "08. Consulta de vuelos",
        "Pasajeros" => "09. Consulta de pasajeros",
        "Reservas" => "10. Reservas",
        "PayPal" => "12. Pagos PayPal",
        "CatalogosBusqueda" => "13. Búsqueda de catálogos (selectores)",
        "Aeropuertos" => "14. Aeropuertos (consulta pública)",
        "VentasAdmin" => "11. Ventas acumuladas (admin)",
        var controller => controller ?? "API"
    };
}
