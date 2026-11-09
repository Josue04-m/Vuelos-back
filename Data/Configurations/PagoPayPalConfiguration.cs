using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vuelos_Backend.Models.Pagos;

namespace Vuelos_Backend.Data.Configurations;

public sealed class PagoPayPalConfiguration : IEntityTypeConfiguration<PagoPayPal>
{
    public void Configure(EntityTypeBuilder<PagoPayPal> b)
    {
        b.ToTable("pagos_paypal", "dbo", t =>
        {
            t.HasCheckConstraint("CK_pagos_paypal_importe", "[Monto] > 0 AND [Moneda] = 'USD'");
            t.HasCheckConstraint("CK_pagos_paypal_entorno", "[Entorno] IN ('Sandbox','Live')");
            t.HasCheckConstraint("CK_pagos_paypal_estado", "[Estado] IN ('creando','creada','verificando','capturando','pendiente','completado','fallido','revision')");
            t.HasCheckConstraint("CK_pagos_paypal_captura", "[FechaCaptura] IS NULL OR [CapturaId] IS NOT NULL");
        });
        b.HasKey(p => p.PagoId);
        b.Property(p => p.PagoId).ValueGeneratedNever();
        b.HasIndex(p => p.ReservaId).IsUnique();
        b.HasIndex(p => p.OrdenId).IsUnique().HasFilter("[OrdenId] IS NOT NULL");
        b.HasIndex(p => p.CapturaId).IsUnique().HasFilter("[CapturaId] IS NOT NULL");
        b.HasIndex(p => new { p.BloqueaReserva, p.FechaActualizacion });
        b.Property(p => p.Monto).HasPrecision(10, 2);
        b.Property(p => p.Moneda).HasMaxLength(3).IsUnicode(false);
        b.Property(p => p.Entorno).HasMaxLength(7).IsUnicode(false);
        b.Property(p => p.Estado).HasMaxLength(20).IsUnicode(false);
        b.Property(p => p.OrdenId).HasMaxLength(64).IsUnicode(false);
        b.Property(p => p.CapturaId).HasMaxLength(64).IsUnicode(false);
        b.Property(p => p.MerchantId).HasMaxLength(64).IsUnicode(false);
        b.Property(p => p.UrlAprobacion).HasMaxLength(2048);
        b.Property(p => p.RowVersion).IsRowVersion();
        b.HasOne(p => p.Reserva).WithMany().HasForeignKey(p => p.ReservaId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class EventoPayPalConfiguration : IEntityTypeConfiguration<EventoPayPal>
{
    public void Configure(EntityTypeBuilder<EventoPayPal> b)
    {
        b.ToTable("eventos_paypal", "dbo");
        b.HasKey(e => e.EventoId);
        b.Property(e => e.EventoId).HasMaxLength(100).IsUnicode(false);
        b.Property(e => e.Tipo).HasMaxLength(100).IsUnicode(false);
    }
}
