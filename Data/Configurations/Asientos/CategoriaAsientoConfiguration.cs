using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vuelos_Backend.Models.Asientos;

namespace Vuelos_Backend.Data.Configurations.Asientos;

public sealed class CategoriaAsientoConfiguration : IEntityTypeConfiguration<CategoriaAsiento>
{
    public void Configure(EntityTypeBuilder<CategoriaAsiento> builder)
    {
        builder.ToTable("categorias_asiento", "pna");
        builder.HasKey(x => x.CategoriaId).HasName("PK_categorias_asiento");
        builder.Property(x => x.CategoriaId).HasColumnName("categoria_id").HasColumnType("integer").IsRequired().ValueGeneratedOnAdd();
        builder.Property(x => x.Nombre).HasColumnName("nombre").HasColumnType("character varying(50)").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Descripcion).HasColumnName("descripcion").HasColumnType("character varying(255)").HasMaxLength(255);
        builder.HasAlternateKey(x => x.Nombre).HasName("UQ_categorias_asiento_nombre");
    }
}

