using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Vuelos_Backend.Data.Configurations;

internal static class VersionesPostgreSqlConfiguration
{
    public static void Configure(ModelBuilder model)
    {
        // El contrato HTTP continúa siendo Base64 de 8 bytes. No se mapea xmin.
        foreach (var entity in model.Model.GetEntityTypes().ToArray())
        {
            var property = entity.FindProperty("RowVersion");
            if (property is null) continue;
            if (property.ClrType != typeof(byte[]))
                throw new InvalidOperationException("RowVersion debe conservar su contrato de 8 bytes.");

            var version = model.Entity(entity.ClrType).Property<byte[]>("RowVersion")
                .HasColumnType("bytea").IsRequired().IsConcurrencyToken().ValueGeneratedOnAddOrUpdate();
            version.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
            version.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

            var tableName = entity.GetTableName()!;
            var table = StoreObjectIdentifier.Table(tableName, entity.GetSchema());
            var column = property.GetColumnName(table)!;
            model.Entity(entity.ClrType).ToTable(t =>
            {
                t.HasCheckConstraint($"CK_{tableName}_version_8_bytes", $"octet_length(\"{column}\") = 8");
                // Esta declaración NO crea el trigger. Su función, secuencia y DDL
                // se incluyen en la migración InicialPostgreSql, de aplicación manual.
                t.HasTrigger($"TR_{tableName}_version");
            });
        }
    }
}
