using System.ComponentModel.DataAnnotations;

namespace Vuelos_Backend.Models.Reservas;

public sealed class OpcionesReservas
{
    public const string Seccion = "Reservas";
    [Range(1, int.MaxValue)] public int DuracionReservaMinutos { get; set; } = 720;
    [Range(1, 3600)] public int IntervaloLiberacionSegundos { get; set; } = 60;
    [Range(typeof(decimal), "0", "100", ParseLimitsInInvariantCulture = true)]
    public decimal PorcentajeIva { get; set; } = 15m;
}
