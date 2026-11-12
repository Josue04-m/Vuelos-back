using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vuelos_Backend.Models.Vuelos;

namespace Vuelos_Backend.Data.Configurations.Vuelos;

public sealed class HistorialEstadoVueloConfiguration : IEntityTypeConfiguration<HistorialEstadoVuelo>
{
    public void Configure(EntityTypeBuilder<HistorialEstadoVuelo> builder)
    {
        builder.ToTable("historial_estados_vuelo", "pna");
        builder.HasKey(x => x.HistorialId).HasName("PK_historial_estados_vuelo");
        builder.Property(x => x.HistorialId).HasColumnName("historial_id").HasColumnType("integer").IsRequired().ValueGeneratedOnAdd();
        builder.Property(x => x.VueloId).HasColumnName("vuelo_id").HasColumnType("integer").IsRequired();
        builder.Property(x => x.EstadoAnterior).HasColumnName("estado_anterior").HasColumnType("character varying(20)").HasMaxLength(20);
        builder.Property(x => x.EstadoNuevo).HasColumnName("estado_nuevo").HasColumnType("character varying(20)").HasMaxLength(20).IsRequired();
        builder.Property(x => x.FechaCambio).HasColumnName("fecha_cambio").HasColumnType("timestamp with time zone").IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(x => x.UsuarioAdminId).HasColumnName("usuario_admin_id").HasColumnType("integer").IsRequired();
        builder.Property(x => x.Observacion).HasColumnName("observacion").HasColumnType("character varying(255)").HasMaxLength(255);
        builder.HasOne(x => x.Vuelo).WithMany().HasForeignKey(x => x.VueloId)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_historial_estados_vuelo_vuelo_id");
        builder.HasOne(x => x.UsuarioAdmin).WithMany().HasForeignKey(x => x.UsuarioAdminId)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_historial_estados_vuelo_usuario_admin_id");
    }
}

