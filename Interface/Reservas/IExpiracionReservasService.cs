namespace Vuelos_Backend.Interface.Reservas;

public interface IExpiracionReservasService
{
    Task ExpirarVueloAsync(int vueloId, CancellationToken ct);
    Task ExpirarLoteAsync(int? usuarioId, CancellationToken ct);
}
