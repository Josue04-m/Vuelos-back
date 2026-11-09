namespace Vuelos_Backend.Models.Reservas;

/// <summary>Desglose del laboratorio en USD. Los precios de pasajeros ya incluyen sus descuentos, pero no este IVA.</summary>
public readonly record struct ImportesReserva(decimal Subtotal, decimal PorcentajeIva, decimal MontoIva, decimal Total)
{
    public static ImportesReserva Calcular(decimal subtotal, decimal porcentajeIva)
    {
        if (porcentajeIva is < 0 or > 100 || porcentajeIva != decimal.Round(porcentajeIva, 2))
            throw new ReservaOperacionException(ErrorReserva.ServicioNoDisponible, "iva_sin_configurar", "Reservas:PorcentajeIva debe estar entre 0 y 100, con hasta dos decimales.");
        if (subtotal is < 0 or > 99999999.99m || subtotal != decimal.Round(subtotal, 2)) throw FueraDeRango();
        var iva = decimal.Round(subtotal * porcentajeIva / 100m, 2, MidpointRounding.AwayFromZero);
        var total = subtotal + iva;
        if (total > 99999999.99m) throw FueraDeRango();
        return new(subtotal, porcentajeIva, iva, total);
    }

    public void Aplicar(Reserva reserva)
    {
        reserva.Subtotal = Subtotal; reserva.PorcentajeIva = PorcentajeIva;
        reserva.MontoIva = MontoIva; reserva.MontoTotal = Total;
    }

    public static bool EsConsistente(Reserva reserva, decimal subtotalDetalles)
    {
        // NULL no significa IVA cero: conserva el importe histórico sin reinterpretarlo.
        if (reserva.Subtotal is null && reserva.PorcentajeIva is null && reserva.MontoIva is null)
            return subtotalDetalles == reserva.MontoTotal;
        if (reserva.Subtotal is null || reserva.PorcentajeIva is null || reserva.MontoIva is null) return false;
        try
        {
            var esperado = Calcular(subtotalDetalles, reserva.PorcentajeIva.Value);
            return reserva.Subtotal == esperado.Subtotal && reserva.MontoIva == esperado.MontoIva && reserva.MontoTotal == esperado.Total;
        }
        catch (ReservaOperacionException) { return false; }
    }

    private static ReservaOperacionException FueraDeRango() => new(ErrorReserva.Conflicto, "importe_fuera_de_rango", "El subtotal o total con IVA supera el importe admitido o tiene decimales inválidos.");
}
