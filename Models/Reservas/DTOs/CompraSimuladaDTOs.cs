namespace Vuelos_Backend.Models.Reservas.DTOs;

/// <summary>Comprobante exclusivamente simulado. No acredita un cobro real.</summary>
public sealed record CompraSimuladaResponse(Guid CompraId, DateTimeOffset FechaCompra, decimal Monto, string Moneda, bool EsSimulada);
