using Microsoft.EntityFrameworkCore;
using Vuelos_Backend.Models.Asientos;
using Vuelos_Backend.Models.Aviones;
using Vuelos_Backend.Models.Reservas;
using Vuelos_Backend.Models.Vuelos;

namespace Vuelos_Backend.Data;

// El llamador mantiene una transacción explícita. El lock se limita a la tabla
// de la subconsulta, antes de que EF agregue Include/LEFT JOIN al SQL exterior.
internal static class ConsultasBloqueoPostgreSql
{
    public static IQueryable<Vuelo> Vuelo(AppDbContext db, int id) => db.Vuelos.FromSqlInterpolated(
        $"SELECT v.* FROM pna.vuelos AS v WHERE v.vuelo_id = {id} FOR UPDATE OF v");

    public static IQueryable<Reserva> ReservaPropia(AppDbContext db, int id, int usuarioId) => db.Reservas.FromSqlInterpolated(
        $"SELECT r.* FROM pna.reservas AS r WHERE r.reserva_id = {id} AND r.usuario_id = {usuarioId} FOR UPDATE OF r");

    public static IQueryable<Avion> Avion(AppDbContext db, int id) => db.Aviones.FromSqlInterpolated(
        $"SELECT a.* FROM pna.aviones AS a WHERE a.avion_id = {id} FOR UPDATE OF a");

    public static IQueryable<AsientoFisico> AsientosFisicos(AppDbContext db, int avionId) => db.AsientosFisicos.FromSqlInterpolated(
        $"SELECT f.* FROM pna.asientos_fisicos AS f WHERE f.avion_id = {avionId} ORDER BY f.asiento_fisico_id FOR SHARE OF f");

    public static IQueryable<AsientoVuelo> AsientoVuelo(AppDbContext db, int id, int vueloId) => db.AsientosVuelo.FromSqlInterpolated(
        $"SELECT a.* FROM pna.asientos_vuelo AS a WHERE a.asiento_vuelo_id = {id} AND a.vuelo_id = {vueloId} FOR UPDATE OF a");

    public static Task LimitarEsperaAsync(AppDbContext db, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("El límite de bloqueo requiere una transacción explícita.");
        return db.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '10s'", ct);
    }
}
