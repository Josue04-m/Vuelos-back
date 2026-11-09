using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Vuelos_Backend.Controllers.Vuelos;
namespace Vuelos_Backend.Swagger;

// Se aplica únicamente al bloque Vuelos para respetar la configuración actual del equipo.
public sealed class VuelosOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(VuelosController)) return;
        foreach (var parameter in operation.Parameters ?? [])
        {
            if (parameter is not OpenApiParameter value) continue;
            value.Example = value.Name?.ToLowerInvariant() switch
            {
                "origen" => JsonValue.Create("GYE"),
                "destino" => JsonValue.Create("UIO"),
                "fecha" => JsonValue.Create("2026-11-15"),
                "pagina" => JsonValue.Create(1),
                "tamanopagina" => JsonValue.Create(20),
                "id" => JsonValue.Create(1),
                _ => null
            };
        }
    }
}
