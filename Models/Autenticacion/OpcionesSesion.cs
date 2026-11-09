using System.ComponentModel.DataAnnotations;
namespace Vuelos_Backend.Models.Autenticacion;

public sealed class OpcionesSesion
{
    public const string Seccion = "Autenticacion";
    [Range(1, int.MaxValue)]
    public int DuracionTokenMinutos { get; set; }
}
