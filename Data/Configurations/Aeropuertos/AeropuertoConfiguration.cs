using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vuelos_Backend.Models.Aeropuertos;

namespace Vuelos_Backend.Data.Configurations.Aeropuertos;

public sealed class AeropuertoConfiguration : IEntityTypeConfiguration<Aeropuerto>
{
    public void Configure(EntityTypeBuilder<Aeropuerto> builder)
    {
        builder.ToTable("aeropuertos", "dbo", t =>
        {
            t.HasCheckConstraint("CK_aeropuertos_latitud", "[latitud] IS NULL OR [latitud] BETWEEN -90 AND 90");
            t.HasCheckConstraint("CK_aeropuertos_longitud", "[longitud] IS NULL OR [longitud] BETWEEN -180 AND 180");
            t.HasCheckConstraint("CK_aeropuertos_datos_origen_json", "[datos_origen_json] IS NULL OR ISJSON([datos_origen_json]) = 1");
        });
        builder.HasKey(x => x.AeropuertoId).HasName("PK_aeropuertos");
        builder.Property(x => x.AeropuertoId).HasColumnName("aeropuerto_id").HasColumnType("int").IsRequired().UseIdentityColumn();
        builder.Property(x => x.CodigoIata).HasColumnName("codigo_iata").HasColumnType("char(3)").HasMaxLength(3).IsUnicode(false).IsFixedLength().IsRequired();
        builder.Property(x => x.Nombre).HasColumnName("nombre").HasColumnType("nvarchar(150)").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Ciudad).HasColumnName("ciudad").HasColumnType("nvarchar(100)").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Pais).HasColumnName("pais").HasColumnType("nvarchar(100)").HasMaxLength(100).IsRequired();
        builder.Property(x => x.IdOrigen).HasColumnName("id_origen").HasColumnType("bigint");
        builder.Property(x => x.Identificador).HasColumnName("identificador").HasMaxLength(50);
        builder.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(32);
        builder.Property(x => x.Latitud).HasColumnName("latitud").HasColumnType("float");
        builder.Property(x => x.Longitud).HasColumnName("longitud").HasColumnType("float");
        builder.Property(x => x.ElevacionPies).HasColumnName("elevacion_pies").HasColumnType("float");
        builder.Property(x => x.Continente).HasColumnName("continente").HasMaxLength(2);
        builder.Property(x => x.RegionIso).HasColumnName("region_iso").HasMaxLength(20);
        builder.Property(x => x.ServicioRegular).HasColumnName("servicio_regular").HasColumnType("bit");
        builder.Property(x => x.CodigoIcao).HasColumnName("codigo_icao").HasMaxLength(10);
        builder.Property(x => x.CodigoGps).HasColumnName("codigo_gps").HasMaxLength(20);
        builder.Property(x => x.CodigoLocal).HasColumnName("codigo_local").HasMaxLength(20);
        builder.Property(x => x.SitioWeb).HasColumnName("sitio_web").HasColumnType("nvarchar(max)");
        builder.Property(x => x.Wikipedia).HasColumnName("wikipedia").HasColumnType("nvarchar(max)");
        builder.Property(x => x.PalabrasClave).HasColumnName("palabras_clave").HasColumnType("nvarchar(max)");
        builder.Property(x => x.DatosOrigenJson).HasColumnName("datos_origen_json").HasColumnType("nvarchar(max)");
        builder.HasAlternateKey(x => x.CodigoIata).HasName("UQ_aeropuertos_codigo_iata");
    }
}

