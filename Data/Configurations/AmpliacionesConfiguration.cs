using Microsoft.EntityFrameworkCore;
using Vuelos_Backend.Models.Asientos;
using Vuelos_Backend.Models.Reservas;
using Vuelos_Backend.Models.Usuarios;

namespace Vuelos_Backend.Data.Configurations;

// Secciones 15.3 y 15.4: ampliaciones del esquema base.
// Tokens tiene su propia configuración en Autenticacion.
internal static class AmpliacionesConfiguration
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(builder =>
        {
            builder.Property(x => x.Eliminado)
                .HasColumnName("eliminado").HasColumnType("boolean")
                .IsRequired().HasDefaultValue(false);
            builder.Property(x => x.FechaEliminacion)
                .HasColumnName("fecha_eliminacion").HasColumnType("timestamp with time zone");
            builder.HasQueryFilter(x => !x.Eliminado);
        });

        modelBuilder.Entity<Reserva>().Property(x => x.RowVersion)
            .HasColumnName("row_version").HasColumnType("bytea")
            .IsRequired().IsConcurrencyToken().ValueGeneratedOnAddOrUpdate();
        modelBuilder.Entity<AsientoVuelo>().Property(x => x.RowVersion)
            .HasColumnName("row_version").HasColumnType("bytea")
            .IsRequired().IsConcurrencyToken().ValueGeneratedOnAddOrUpdate();

        modelBuilder.Entity<DetalleReserva>()
            .HasIndex(x => x.AsientoVueloId)
            .HasDatabaseName("UX_detalle_reservas_asiento_activo")
            .IsUnique().HasFilter("\"estado\" = 'activo'");
    }
}

