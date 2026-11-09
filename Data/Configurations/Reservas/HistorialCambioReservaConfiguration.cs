using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vuelos_Backend.Models.Reservas;

namespace Vuelos_Backend.Data.Configurations.Reservas;

public sealed class HistorialCambioReservaConfiguration : IEntityTypeConfiguration<HistorialCambioReserva>
{
    public void Configure(EntityTypeBuilder<HistorialCambioReserva> builder)
    {
        builder.ToTable("historial_cambios_reserva", "dbo");
        builder.HasKey(x => x.HistorialReservaId).HasName("PK_historial_cambios_reserva");
        builder.Property(x => x.HistorialReservaId).HasColumnName("historial_reserva_id").HasColumnType("int").IsRequired().UseIdentityColumn();
        builder.Property(x => x.ReservaId).HasColumnName("reserva_id").HasColumnType("int").IsRequired();
        builder.Property(x => x.AsientoVueloAnteriorId).HasColumnName("asiento_vuelo_anterior_id").HasColumnType("int");
        builder.Property(x => x.AsientoVueloNuevoId).HasColumnName("asiento_vuelo_nuevo_id").HasColumnType("int");
        builder.Property(x => x.TipoAccion).HasColumnName("tipo_accion").HasColumnType("varchar(50)").HasMaxLength(50).IsUnicode(false).IsRequired();
        builder.Property(x => x.FechaAccion).HasColumnName("fecha_accion").HasColumnType("datetime2").IsRequired().HasDefaultValueSql("GETDATE()");
        builder.Property(x => x.Observacion).HasColumnName("observacion").HasColumnType("varchar(255)").HasMaxLength(255).IsUnicode(false);
        builder.HasOne(x => x.Reserva).WithMany().HasForeignKey(x => x.ReservaId)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_historial_cambios_reserva_reserva_id");
        builder.HasOne(x => x.AsientoVueloAnterior).WithMany().HasForeignKey(x => x.AsientoVueloAnteriorId)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_historial_cambios_reserva_asiento_vuelo_anterior_id");
        builder.HasOne(x => x.AsientoVueloNuevo).WithMany().HasForeignKey(x => x.AsientoVueloNuevoId)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_historial_cambios_reserva_asiento_vuelo_nuevo_id");
    }
}

