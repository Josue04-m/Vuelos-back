using Vuelos_Backend.Models.Usuarios;

namespace Vuelos_Backend.Models.Vuelos;

public class HistorialEstadoVuelo
{
    public int HistorialId { get; set; }
    public int VueloId { get; set; }
    public string? EstadoAnterior { get; set; }
    public string EstadoNuevo { get; set; } = null!;
    public DateTime FechaCambio { get; set; }
    public int UsuarioAdminId { get; set; }
    public string? Observacion { get; set; }
    public Vuelo Vuelo { get; set; } = null!;
    public Usuario UsuarioAdmin { get; set; } = null!;
}

