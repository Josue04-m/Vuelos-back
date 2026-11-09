using Vuelos_Backend.Models.Pagos;

namespace Vuelos_Backend.Interface.Pagos;

public interface IPagoPayPalRepository
{
    Task<PagoPayPal?> PorReservaAsync(int reservaId, CancellationToken ct);
    Task<PagoPayPal?> PorIdAsync(Guid pagoId, CancellationToken ct);
    Task<PagoPayPal?> PorIdentificadorExternoAsync(string ordenId, string capturaId, string entorno, CancellationToken ct);
    void Agregar(PagoPayPal pago);
    void LimpiarSeguimiento();
    Task<bool> EventoExisteAsync(string eventoId, CancellationToken ct);
    Task RegistrarEventoAsync(EventoPayPal evento, CancellationToken ct);
}
