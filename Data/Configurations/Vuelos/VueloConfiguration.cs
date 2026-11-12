using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vuelos_Backend.Models.Aviones;
using Vuelos_Backend.Models.Vuelos;

namespace Vuelos_Backend.Data.Configurations.Vuelos;

public sealed class VueloConfiguration : IEntityTypeConfiguration<Vuelo>
{
    public void Configure(EntityTypeBuilder<Vuelo> builder)
    {
        builder.ToTable("vuelos", "pna", table =>
        {
            table.HasCheckConstraint("CK_vuelos_origen_destino", "\"aeropuerto_origen_id\" <> \"aeropuerto_destino_id\"");
            table.HasCheckConstraint("CK_vuelos_horarios", "\"llegada_programada\" > \"salida_programada\"");
            table.HasCheckConstraint("CK_vuelos_estado", "\"estado\" IN ('programado', 'embarcando', 'retrasado', 'en vuelo', 'finalizado', 'cancelado')");
        });
        builder.HasKey(x => x.VueloId).HasName("PK_vuelos");
        builder.Property(x => x.VueloId).HasColumnName("vuelo_id").HasColumnType("integer").IsRequired().ValueGeneratedOnAdd();
        builder.Property(x => x.NumeroVuelo).HasColumnName("numero_vuelo").HasColumnType("character varying(20)").HasMaxLength(20).IsRequired();
        builder.Property(x => x.AvionId).HasColumnName("avion_id").HasColumnType("integer").IsRequired();
        builder.Property(x => x.AeropuertoOrigenId).HasColumnName("aeropuerto_origen_id").HasColumnType("integer").IsRequired();
        builder.Property(x => x.AeropuertoDestinoId).HasColumnName("aeropuerto_destino_id").HasColumnType("integer").IsRequired();
        builder.Property(x => x.SalidaProgramada).HasColumnName("salida_programada").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.LlegadaProgramada).HasColumnName("llegada_programada").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.SalidaReal).HasColumnName("salida_real").HasColumnType("timestamp with time zone");
        builder.Property(x => x.LlegadaReal).HasColumnName("llegada_real").HasColumnType("timestamp with time zone");
        builder.Property(x => x.Estado).HasColumnName("estado").HasColumnType("character varying(20)").HasMaxLength(20).IsRequired().HasDefaultValue("programado");
        builder.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.HasOne(x => x.Avion).WithMany().HasForeignKey(x => x.AvionId)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_vuelos_avion_id");
        builder.HasOne(x => x.AeropuertoOrigen).WithMany().HasForeignKey(x => x.AeropuertoOrigenId)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_vuelos_aeropuerto_origen_id");
        builder.HasOne(x => x.AeropuertoDestino).WithMany().HasForeignKey(x => x.AeropuertoDestinoId)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_vuelos_aeropuerto_destino_id");
    }
}

