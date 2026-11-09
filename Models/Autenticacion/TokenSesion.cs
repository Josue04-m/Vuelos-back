using Vuelos_Backend.Models.Usuarios;

namespace Vuelos_Backend.Models.Autenticacion;

public class TokenSesion
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Token { get; set; } = null!;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaExpiracion { get; set; }
    public bool Activo { get; set; } = true;

    public Usuario Usuario { get; set; } = null!;
}

