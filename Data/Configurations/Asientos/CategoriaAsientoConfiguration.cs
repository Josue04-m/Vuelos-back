using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vuelos_Backend.Models.Asientos;

namespace Vuelos_Backend.Data.Configurations.Asientos;

public sealed class CategoriaAsientoConfiguration : IEntityTypeConfiguration<CategoriaAsiento>
{
    public void Configure(EntityTypeBuilder<CategoriaAsiento> builder)
    {
        builder.ToTable("categorias_asiento", "dbo");
        builder.HasKey(x => x.CategoriaId).HasName("PK_categorias_asiento");
        builder.Property(x => x.CategoriaId).HasColumnName("categoria_id").HasColumnType("int").IsRequired().UseIdentityColumn();
        builder.Property(x => x.Nombre).HasColumnName("nombre").HasColumnType("varchar(50)").HasMaxLength(50).IsUnicode(false).IsRequired();
        builder.Property(x => x.Descripcion).HasColumnName("descripcion").HasColumnType("varchar(255)").HasMaxLength(255).IsUnicode(false);
        builder.HasAlternateKey(x => x.Nombre).HasName("UQ_categorias_asiento_nombre");
    }
}

