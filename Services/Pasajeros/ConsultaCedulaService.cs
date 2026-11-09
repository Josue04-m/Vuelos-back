using Vuelos_Backend.Integrations.EcuadorLegal;
using Vuelos_Backend.Interface.Pasajeros;
using Vuelos_Backend.Models.Pasajeros;
using Vuelos_Backend.Models.Pasajeros.DTOs;
namespace Vuelos_Backend.Services.Pasajeros;

public sealed class ConsultaCedulaService(HttpClient client) : IConsultaCedulaService
{
    public const string Endpoint = "https://www.ecuadorlegalonline.com/modulo/consultar-cedula.php";

    public async Task<ConsultaCedulaResponse> ConsultarAsync(
        string cedula, CancellationToken cancellationToken)
    {
        cedula = cedula.Trim();
        if (cedula.Length != 10 || cedula.Any(c => c is < '0' or > '9'))
            throw new ConsultaCedulaException(ErrorConsultaCedula.SolicitudInvalida);

        try
        {
            using var datos = new FormUrlEncodedContent(
                new Dictionary<string, string> { ["name"] = cedula, ["tipo"] = "1" });
            using var respuesta = await client.PostAsync(Endpoint, datos, cancellationToken);
            respuesta.EnsureSuccessStatusCode();
            var html = await respuesta.Content.ReadAsStringAsync(cancellationToken);
            var nombre = EcuadorLegalNombreExtractor.Extraer(html, cedula)
                ?? throw new ConsultaCedulaException(ErrorConsultaCedula.SinResultado);

            // Igual que el ejemplo: últimos dos términos como apellidos.
            // Es solo una sugerencia y siempre requiere revisión.
            var partes = nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return new ConsultaCedulaResponse(
                cedula, nombre, partes[0],
                partes.Length >= 3 ? string.Join(" ", partes.Skip(1).Take(partes.Length - 3)) : "",
                partes.Length >= 3 ? partes[^2] : partes.Length == 2 ? partes[1] : "",
                partes.Length >= 3 ? partes[^1] : "",
                RequiereRevision: true);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ConsultaCedulaException(ErrorConsultaCedula.TiempoAgotado);
        }
        catch (HttpRequestException)
        {
            throw new ConsultaCedulaException(ErrorConsultaCedula.ProveedorNoDisponible);
        }
    }
}
