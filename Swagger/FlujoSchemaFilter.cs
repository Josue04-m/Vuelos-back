using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Vuelos_Backend.Models.Ventas.DTOs;
using Vuelos_Backend.Models.Vuelos.DTOs;

namespace Vuelos_Backend.Swagger;

public sealed class FlujoSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema model, SchemaFilterContext context)
    {
        if (model is not OpenApiSchema schema) return;
        // Solo ejemplos de documentación: no establecen valores predeterminados ni datos en la base.
        if (context.Type == typeof(CrearVueloAdminRequest))
        {
            var fecha = DateTime.UtcNow.Date.AddDays(1);
            schema.Example = new JsonObject
            {
                ["numeroVuelo"] = "PN100", ["avionId"] = 1, ["aeropuertoOrigenId"] = 1, ["aeropuertoDestinoId"] = 2,
                ["salidaProgramada"] = fecha.AddHours(10).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
                ["llegadaProgramada"] = fecha.AddHours(11).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
                ["tarifasPorClase"] = JsonNode.Parse("""[{"categoriaId":1,"precio":150},{"categoriaId":2,"precio":100},{"categoriaId":3,"precio":70}]""")
            };
        }
        if (context.Type == typeof(ResumenVentasResponse))
            schema.Example = JsonNode.Parse("""{"totalVentas":150,"cantidadCompras":1,"moneda":"USD","proveedor":"PayPal","entorno":"Sandbox","esSandbox":true,"desde":null,"hasta":null}""");
    }
}
