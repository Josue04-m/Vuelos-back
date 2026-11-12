using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vuelos_Backend.Models.Reservas;

namespace Vuelos_Backend.Data.Configurations.Reservas;

public sealed class CompraSimuladaConfiguration : IEntityTypeConfiguration<CompraSimulada>
{
    public void Configure(EntityTypeBuilder<CompraSimulada> builder)
    {
        builder.ToTable("compras_simuladas", "pna", table =>
            table.HasCheckConstraint("CK_compras_simuladas_laboratorio", "\"es_simulada\" = TRUE AND \"moneda\" = 'USD' AND \"monto\" >= 0"));
        builder.HasKey(x => x.CompraId).HasName("PK_compras_simuladas");
        builder.Property(x => x.CompraId).HasColumnName("compra_id").ValueGeneratedNever();
        builder.Property(x => x.ReservaId).HasColumnName("reserva_id");
        builder.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        builder.Property(x => x.FechaCompra).HasColumnName("fecha_compra").HasColumnType("timestamp with time zone");
        builder.Property(x => x.Monto).HasColumnName("monto").HasPrecision(10, 2);
        builder.Property(x => x.Moneda).HasColumnName("moneda").HasColumnType("character varying(3)");
        builder.Property(x => x.EsSimulada).HasColumnName("es_simulada");
        builder.HasIndex(x => x.ReservaId).IsUnique().HasDatabaseName("UX_compras_simuladas_reserva");
        builder.HasOne(x => x.Reserva).WithMany().HasForeignKey(x => x.ReservaId).OnDelete(DeleteBehavior.NoAction);
        // Sin navegación a Usuario para conservar el comprobante tras su eliminación lógica.
        builder.HasOne<Models.Usuarios.Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.NoAction);
    }
}
