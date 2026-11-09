namespace Vuelos_Backend.Models.Pasajeros.DTOs;

/// <summary>Datos sugeridos para autocompletar. Revisar nombres y apellidos compuestos antes de guardar.</summary>
public sealed record ConsultaCedulaResponse(
    string Cedula, string NombreCompleto,
    string PrimerNombre, string SegundoNombre, string PrimerApellido, string SegundoApellido,
    bool RequiereRevision);
