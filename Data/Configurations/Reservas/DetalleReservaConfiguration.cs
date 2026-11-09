using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vuelos_Backend.Models.Asientos;
using Vuelos_Backend.Models.Reservas;

namespace Vuelos_Backend.Data.Configurations.Reservas;

public sealed class DetalleReservaConfiguration : IEntityTypeConfiguration<DetalleReserva>
{
    public void Configure(EntityTypeBuilder<DetalleReserva> builder)
    {
        builder.ToTable("detalle_reservas", "dbo", table =>
        {
            table.HasCheckConstraint("CK_detalle_reservas_estado", "[estado] IN ('activo', 'liberado')");
            table.HasCheckConstraint("CK_detalle_reservas_tipo_pasajero",
                "[tipo_pasajero] IS NULL OR [tipo_pasajero] IN ('adulto', 'joven', 'nino', 'bebe')");
        });
        builder.HasKey(x => x.DetalleId).HasName("PK_detalle_reservas");
        builder.Property(x => x.DetalleId).HasColumnName("detalle_id").HasColumnType("int").IsRequired().UseIdentityColumn();
        builder.Property(x => x.ReservaId).HasColumnName("reserva_id").HasColumnType("int").IsRequired();
        builder.Property(x => x.AsientoVueloId).HasColumnName("asiento_vuelo_id").HasColumnType("int").IsRequired();
        builder.Property(x => x.PrimerNombre).HasColumnName("primer_nombre").HasColumnType("varchar(100)").HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(x => x.SegundoNombre).HasColumnName("segundo_nombre").HasColumnType("varchar(100)").HasMaxLength(100).IsUnicode(false);
        builder.Property(x => x.PrimerApellido).HasColumnName("primer_apellido").HasColumnType("varchar(100)").HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(x => x.SegundoApellido).HasColumnName("segundo_apellido").HasColumnType("varchar(100)").HasMaxLength(100).IsUnicode(false);
        builder.Property(x => x.DocumentoIdentidad).HasColumnName("documento_identidad").HasColumnType("varchar(50)").HasMaxLength(50).IsUnicode(false).IsRequired();
        builder.Property(x => x.TipoPasajero).HasColumnName("tipo_pasajero").HasColumnType("varchar(10)").HasMaxLength(10).IsUnicode(false);
        builder.Property(x => x.PrecioCongelado).HasColumnName("precio_congelado").HasColumnType("decimal(10,2)").IsRequired().HasPrecision(10, 2);
        builder.Property(x => x.Estado).HasColumnName("estado").HasColumnType("varchar(20)").HasMaxLength(20).IsUnicode(false).IsRequired().HasDefaultValue("activo");
        builder.HasOne(x => x.Reserva).WithMany().HasForeignKey(x => x.ReservaId)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_detalle_reservas_reserva_id");
        builder.HasOne(x => x.AsientoVuelo).WithMany().HasForeignKey(x => x.AsientoVueloId)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_detalle_reservas_asiento_vuelo_id");
    }
}

