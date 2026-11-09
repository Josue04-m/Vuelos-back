using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vuelos_Backend.Models.Asientos;
using Vuelos_Backend.Models.Vuelos;

namespace Vuelos_Backend.Data.Configurations.Asientos;

public sealed class AsientoVueloConfiguration : IEntityTypeConfiguration<AsientoVuelo>
{
    public void Configure(EntityTypeBuilder<AsientoVuelo> builder)
    {
        builder.ToTable("asientos_vuelo", "dbo", table =>
        {
            table.HasCheckConstraint("CK_asientos_vuelo_precio", "[precio] >= 0");
            table.HasCheckConstraint("CK_asientos_vuelo_estado", "[estado] IN ('disponible', 'reservado', 'bloqueado')");
        });
        builder.HasKey(x => x.AsientoVueloId).HasName("PK_asientos_vuelo");
        builder.Property(x => x.AsientoVueloId).HasColumnName("asiento_vuelo_id").HasColumnType("int").IsRequired().UseIdentityColumn();
        builder.Property(x => x.VueloId).HasColumnName("vuelo_id").HasColumnType("int").IsRequired();
        builder.Property(x => x.AsientoFisicoId).HasColumnName("asiento_fisico_id").HasColumnType("int").IsRequired();
        builder.Property(x => x.Precio).HasColumnName("precio").HasColumnType("decimal(10,2)").IsRequired().HasPrecision(10, 2);
        builder.Property(x => x.Estado).HasColumnName("estado").HasColumnType("varchar(20)").HasMaxLength(20).IsUnicode(false).IsRequired().HasDefaultValue("disponible");
        builder.HasAlternateKey(x => new { x.VueloId, x.AsientoFisicoId }).HasName("UQ_asientos_vuelo_vuelo_id_asiento_fisico_id");
        builder.HasOne(x => x.Vuelo).WithMany().HasForeignKey(x => x.VueloId)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_asientos_vuelo_vuelo_id");
        builder.HasOne(x => x.AsientoFisico).WithMany().HasForeignKey(x => x.AsientoFisicoId)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_asientos_vuelo_asiento_fisico_id");
    }
}

