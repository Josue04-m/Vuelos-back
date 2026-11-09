using Microsoft.EntityFrameworkCore;
using Vuelos_Backend.Models.Aeropuertos;
using Vuelos_Backend.Models.Asientos;
using Vuelos_Backend.Models.Aviones;
using Vuelos_Backend.Models.Common;

namespace Vuelos_Backend.Data.Configurations;

public static class CatalogosConfiguration
{
    public static void Configure(ModelBuilder builder)
    {
        Configure<Aeropuerto>(builder);
        Configure<Avion>(builder);
        Configure<CategoriaAsiento>(builder);
        Configure<AsientoFisico>(builder);
    }

    private static void Configure<T>(ModelBuilder model) where T : class, ICatalogoAdministrable
    {
        var entity = model.Entity<T>();
        entity.Property(x => x.Activo).HasColumnName("activo").HasColumnType("bit").HasDefaultValue(true);
        entity.Property(x => x.FechaDesactivacion).HasColumnName("fecha_desactivacion").HasColumnType("datetime2");
        entity.Property(x => x.RowVersion).HasColumnName("row_version").IsRowVersion();
        // Sin filtro global: los vuelos y reservas conservan referencias a catálogos inactivos.
    }
}
