using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Vuelos_Backend.Models.Reservas.DTOs;
namespace Vuelos_Backend.Swagger;

public sealed class ReservasSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema model, SchemaFilterContext context)
    {
        if (model is not OpenApiSchema schema) return;
        // Identificadores, versiones y total son placeholders: obtener los reales
        // del vuelo consultado. No se establecen tarifas ni descuentos de producción.
        if (context.Type == typeof(CrearReservaRequest))
            schema.Example = JsonNode.Parse("""
                {"vueloId":1,"totalEsperado":0,"pasajeros":[
                  {"cedula":"DEMO-001","primerNombre":"Sofia","segundoNombre":null,
                   "primerApellido":"Torres","segundoApellido":null,"tipoPasajero":"adulto",
                   "asientoVueloId":1,"rowVersionAsiento":"AAAAAAAAAAA="}]}
                """);
        if (context.Type == typeof(CambiarAsientosRequest))
            schema.Example = JsonNode.Parse("""
                {"rowVersion":"AAAAAAAAAAA=","totalEsperado":0,
                 "cambios":[{"detalleId":1,"asientoVueloId":2,"rowVersionAsiento":"AAAAAAAAAAA="}]}
                """);
        if (context.Type == typeof(CancelarReservaRequest) || context.Type == typeof(Vuelos_Backend.Models.Pagos.CrearOrdenPayPalRequest))
            schema.Example = JsonNode.Parse("""{"rowVersion":"AAAAAAAAAAA="}""");
    }
}
