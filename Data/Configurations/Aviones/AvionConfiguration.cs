using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vuelos_Backend.Models.Aviones;

namespace Vuelos_Backend.Data.Configurations.Aviones;

public sealed class AvionConfiguration : IEntityTypeConfiguration<Avion>
{
    public void Configure(EntityTypeBuilder<Avion> builder)
    {
        builder.ToTable("aviones", "pna", table =>
        {
            table.HasCheckConstraint("CK_aviones_capacidad_total", "\"capacidad_total\" > 0");
            table.HasCheckConstraint("CK_aviones_datos_origen_json", "\"datos_origen_json\" IS NULL OR json_typeof(\"datos_origen_json\") IN ('object', 'array')");
            table.HasCheckConstraint("CK_aviones_distribucion_importada", "(\"cantidad_primera_clase\" IS NULL AND \"cantidad_segunda_clase\" IS NULL AND \"cantidad_tercera_clase\" IS NULL) OR (\"cantidad_primera_clase\" IS NOT NULL AND \"cantidad_segunda_clase\" IS NOT NULL AND \"cantidad_tercera_clase\" IS NOT NULL AND \"cantidad_primera_clase\" >= 0 AND \"cantidad_segunda_clase\" >= 0 AND \"cantidad_tercera_clase\" >= 0 AND CAST(\"cantidad_primera_clase\" AS bigint) + CAST(\"cantidad_segunda_clase\" AS bigint) + CAST(\"cantidad_tercera_clase\" AS bigint) = \"capacidad_total\")");
        });
        builder.HasKey(x => x.AvionId).HasName("PK_aviones");
        builder.Property(x => x.AvionId).HasColumnName("avion_id").HasColumnType("integer").IsRequired().ValueGeneratedOnAdd();
        builder.Property(x => x.Modelo).HasColumnName("modelo").HasColumnType("character varying(100)").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Matricula).HasColumnName("matricula").HasColumnType("character varying(50)").HasMaxLength(50).IsRequired();
        builder.Property(x => x.CapacidadTotal).HasColumnName("capacidad_total").HasColumnType("integer").IsRequired();
        builder.Property(x => x.CodigoIata).HasColumnName("codigo_iata").HasMaxLength(3);
        builder.Property(x => x.CodigoIcao).HasColumnName("codigo_icao").HasMaxLength(4);
        builder.Property(x => x.CantidadPrimeraClase).HasColumnName("cantidad_primera_clase").HasColumnType("integer");
        builder.Property(x => x.CantidadSegundaClase).HasColumnName("cantidad_segunda_clase").HasColumnType("integer");
        builder.Property(x => x.CantidadTerceraClase).HasColumnName("cantidad_tercera_clase").HasColumnType("integer");
        builder.Property(x => x.LetrasPorFila).HasColumnName("letras_por_fila").HasMaxLength(26);
        builder.Property(x => x.DatosOrigenJson).HasColumnName("datos_origen_json").HasColumnType("json");
        builder.HasAlternateKey(x => x.Matricula).HasName("UQ_aviones_matricula");
    }
}

