using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vuelos_Backend.Models.Usuarios;

namespace Vuelos_Backend.Data.Configurations.Usuarios;

public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios", "dbo", table =>
        {
            table.HasCheckConstraint("CK_usuarios_rol", "[rol] IN ('cliente', 'admin')");
        });
        builder.HasKey(x => x.UsuarioId).HasName("PK_usuarios");
        builder.Property(x => x.UsuarioId).HasColumnName("usuario_id").HasColumnType("int").IsRequired().UseIdentityColumn();
        builder.Property(x => x.Nombre).HasColumnName("nombre").HasColumnType("varchar(100)").HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(x => x.Apellido).HasColumnName("apellido").HasColumnType("varchar(100)").HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(x => x.Email).HasColumnName("email").HasColumnType("varchar(150)").HasMaxLength(150).IsUnicode(false).IsRequired();
        builder.Property(x => x.PasswordHash).HasColumnName("password_hash").HasColumnType("varchar(255)").HasMaxLength(255).IsUnicode(false).IsRequired();
        builder.Property(x => x.Rol).HasColumnName("rol").HasColumnType("varchar(20)").HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("datetime2").IsRequired().HasDefaultValueSql("GETDATE()");
        builder.HasAlternateKey(x => x.Email).HasName("UQ_usuarios_email");
    }
}

