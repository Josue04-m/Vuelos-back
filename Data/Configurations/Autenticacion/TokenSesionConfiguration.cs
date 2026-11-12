using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vuelos_Backend.Models.Autenticacion;
using Vuelos_Backend.Models.Usuarios;

namespace Vuelos_Backend.Data.Configurations.Autenticacion;

// Ampliación propuesta en la sección 15.2.
public sealed class TokenSesionConfiguration : IEntityTypeConfiguration<TokenSesion>
{
    public void Configure(EntityTypeBuilder<TokenSesion> builder)
    {
        builder.ToTable("Tokens", "pna", table =>
        {
            table.HasCheckConstraint("CK_Tokens_Fechas", "\"FechaExpiracion\" > \"FechaCreacion\"");
        });
        builder.HasKey(x => x.Id).HasName("PK_Tokens");
        builder.Property(x => x.Id).HasColumnName("Id").HasColumnType("integer").IsRequired().ValueGeneratedOnAdd();
        builder.Property(x => x.UsuarioId).HasColumnName("UsuarioId").HasColumnType("integer").IsRequired();
        builder.Property(x => x.Token).HasColumnName("Token").HasColumnType("character varying(200)").HasMaxLength(200).IsRequired();
        builder.Property(x => x.FechaCreacion).HasColumnName("FechaCreacion").HasColumnType("timestamp with time zone").IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(x => x.FechaExpiracion).HasColumnName("FechaExpiracion").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.Activo).HasColumnName("Activo").HasColumnType("boolean").IsRequired().HasDefaultValue(true).HasSentinel(true);
        builder.HasAlternateKey(x => x.Token).HasName("UQ_Tokens_Token");
        builder.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Tokens_usuarios");
    }
}

