using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vuelos_Backend.Models.Asientos;
using Vuelos_Backend.Models.Aviones;

namespace Vuelos_Backend.Data.Configurations.Asientos;

public sealed class AsientoFisicoConfiguration : IEntityTypeConfiguration<AsientoFisico>
{
    public void Configure(EntityTypeBuilder<AsientoFisico> builder)
    {
        builder.ToTable("asientos_fisicos", "pna");
        builder.HasKey(x => x.AsientoFisicoId).HasName("PK_asientos_fisicos");
        builder.Property(x => x.AsientoFisicoId).HasColumnName("asiento_fisico_id").HasColumnType("integer").IsRequired().ValueGeneratedOnAdd();
        builder.Property(x => x.AvionId).HasColumnName("avion_id").HasColumnType("integer").IsRequired();
        builder.Property(x => x.CategoriaId).HasColumnName("categoria_id").HasColumnType("integer").IsRequired();
        builder.Property(x => x.CodigoAsiento).HasColumnName("codigo_asiento").HasColumnType("character varying(10)").HasMaxLength(10).IsRequired();
        builder.HasAlternateKey(x => new { x.AvionId, x.CodigoAsiento }).HasName("UQ_asientos_fisicos_avion_id_codigo_asiento");
        builder.HasOne(x => x.Avion).WithMany().HasForeignKey(x => x.AvionId)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_asientos_fisicos_avion_id");
        builder.HasOne(x => x.Categoria).WithMany().HasForeignKey(x => x.CategoriaId)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_asientos_fisicos_categoria_id");
    }
}

