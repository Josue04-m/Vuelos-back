using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vuelos_Backend.Models.Vuelos;

namespace Vuelos_Backend.Data.Configurations.Vuelos;

public sealed class HistorialEstadoVueloConfiguration : IEntityTypeConfiguration<HistorialEstadoVuelo>
{
    public void Configure(EntityTypeBuilder<HistorialEstadoVuelo> builder)
    {
        builder.ToTable("historial_estados_vuelo", "dbo");
        builder.HasKey(x => x.HistorialId).HasName("PK_historial_estados_vuelo");
        builder.Property(x => x.HistorialId).HasColumnName("historial_id").HasColumnType("int").IsRequired().UseIdentityColumn();
        builder.Property(x => x.VueloId).HasColumnName("vuelo_id").HasColumnType("int").IsRequired();
        builder.Property(x => x.EstadoAnterior).HasColumnName("estado_anterior").HasColumnType("varchar(20)").HasMaxLength(20).IsUnicode(false);
        builder.Property(x => x.EstadoNuevo).HasColumnName("estado_nuevo").HasColumnType("varchar(20)").HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(x => x.FechaCambio).HasColumnName("fecha_cambio").HasColumnType("datetime2").IsRequired().HasDefaultValueSql("GETDATE()");
        builder.Property(x => x.UsuarioAdminId).HasColumnName("usuario_admin_id").HasColumnType("int").IsRequired();
        builder.Property(x => x.Observacion).HasColumnName("observacion").HasColumnType("varchar(255)").HasMaxLength(255).IsUnicode(false);
        builder.HasOne(x => x.Vuelo).WithMany().HasForeignKey(x => x.VueloId)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_historial_estados_vuelo_vuelo_id");
        builder.HasOne(x => x.UsuarioAdmin).WithMany().HasForeignKey(x => x.UsuarioAdminId)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_historial_estados_vuelo_usuario_admin_id");
    }
}

