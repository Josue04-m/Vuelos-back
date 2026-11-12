using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vuelos_Backend.Models.Usuarios;

namespace Vuelos_Backend.Data.Configurations.Usuarios;

public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios", "pna", table =>
        {
            table.HasCheckConstraint("CK_usuarios_rol", "\"rol\" IN ('cliente', 'admin')");
        });
        builder.HasKey(x => x.UsuarioId).HasName("PK_usuarios");
        builder.Property(x => x.UsuarioId).HasColumnName("usuario_id").HasColumnType("integer").IsRequired().ValueGeneratedOnAdd();
        builder.Property(x => x.Nombre).HasColumnName("nombre").HasColumnType("character varying(100)").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Apellido).HasColumnName("apellido").HasColumnType("character varying(100)").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Email).HasColumnName("email").HasColumnType("character varying(150)").HasMaxLength(150).IsRequired();
        builder.Property(x => x.PasswordHash).HasColumnName("password_hash").HasColumnType("character varying(255)").HasMaxLength(255).IsRequired();
        builder.Property(x => x.Rol).HasColumnName("rol").HasColumnType("character varying(20)").HasMaxLength(20).IsRequired();
        builder.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.HasAlternateKey(x => x.Email).HasName("UQ_usuarios_email");
    }
}

