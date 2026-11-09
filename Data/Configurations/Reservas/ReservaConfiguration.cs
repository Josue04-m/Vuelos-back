using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vuelos_Backend.Models.Reservas;
using Vuelos_Backend.Models.Usuarios;
using Vuelos_Backend.Models.Vuelos;

namespace Vuelos_Backend.Data.Configurations.Reservas;

public sealed class ReservaConfiguration : IEntityTypeConfiguration<Reserva>
{
    public void Configure(EntityTypeBuilder<Reserva> builder)
    {
        builder.ToTable("reservas", "dbo", table =>
        {
            table.HasCheckConstraint("CK_reservas_estado", "[estado] IN ('confirmada', 'cancelada', 'modificada', 'pendiente_pago', 'pagada', 'expirada')");
            table.HasCheckConstraint("CK_reservas_vencimiento", "[estado] NOT IN ('pendiente_pago', 'pagada', 'expirada') OR [fecha_expiracion] IS NOT NULL");
            table.HasCheckConstraint("CK_reservas_desglose_iva", "([subtotal] IS NULL AND [porcentaje_iva] IS NULL AND [monto_iva] IS NULL) OR ([subtotal] IS NOT NULL AND [porcentaje_iva] IS NOT NULL AND [monto_iva] IS NOT NULL AND [subtotal] >= 0 AND [porcentaje_iva] BETWEEN 0 AND 100 AND [monto_iva] >= 0 AND [monto_iva] = ROUND([subtotal] * [porcentaje_iva] / 100.0, 2) AND [monto_total] = [subtotal] + [monto_iva])");
        });
        builder.HasKey(x => x.ReservaId).HasName("PK_reservas");
        builder.Property(x => x.ReservaId).HasColumnName("reserva_id").HasColumnType("int").IsRequired().UseIdentityColumn();
        builder.Property(x => x.CodigoReserva).HasColumnName("codigo_reserva").HasColumnType("varchar(10)").HasMaxLength(10).IsUnicode(false).IsRequired();
        builder.Property(x => x.UsuarioId).HasColumnName("usuario_id").HasColumnType("int").IsRequired();
        builder.Property(x => x.VueloId).HasColumnName("vuelo_id").HasColumnType("int").IsRequired();
        builder.Property(x => x.FechaReserva).HasColumnName("fecha_reserva").HasColumnType("datetime2").IsRequired().HasDefaultValueSql("GETDATE()");
        builder.Property(x => x.FechaExpiracion).HasColumnName("fecha_expiracion").HasColumnType("datetime2");
        builder.HasIndex(x => new { x.Estado, x.FechaExpiracion, x.VueloId }).HasDatabaseName("IX_reservas_vencimiento");
        builder.Property(x => x.MontoTotal).HasColumnName("monto_total").HasColumnType("decimal(10,2)").IsRequired().HasPrecision(10, 2).HasDefaultValue(0m);
        builder.Property(x => x.Subtotal).HasColumnName("subtotal").HasPrecision(10, 2);
        builder.Property(x => x.PorcentajeIva).HasColumnName("porcentaje_iva").HasPrecision(5, 2);
        builder.Property(x => x.MontoIva).HasColumnName("monto_iva").HasPrecision(10, 2);
        builder.Property(x => x.Estado).HasColumnName("estado").HasColumnType("varchar(20)").HasMaxLength(20).IsUnicode(false).IsRequired().HasDefaultValue("confirmada");
        builder.HasAlternateKey(x => x.CodigoReserva).HasName("UQ_reservas_codigo_reserva");
        builder.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_reservas_usuario_id");
        builder.HasOne(x => x.Vuelo).WithMany().HasForeignKey(x => x.VueloId)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_reservas_vuelo_id");
    }
}

