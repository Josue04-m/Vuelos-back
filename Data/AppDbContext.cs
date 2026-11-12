using Microsoft.EntityFrameworkCore;
using Vuelos_Backend.Data.Configurations;
using Vuelos_Backend.Models.Aeropuertos;
using Vuelos_Backend.Models.Asientos;
using Vuelos_Backend.Models.Autenticacion;
using Vuelos_Backend.Models.Aviones;
using Vuelos_Backend.Models.Reservas;
using Vuelos_Backend.Models.Usuarios;
using Vuelos_Backend.Models.Vuelos;

namespace Vuelos_Backend.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Aeropuerto> Aeropuertos => Set<Aeropuerto>();
    public DbSet<Avion> Aviones => Set<Avion>();
    public DbSet<CategoriaAsiento> CategoriasAsiento => Set<CategoriaAsiento>();
    public DbSet<AsientoFisico> AsientosFisicos => Set<AsientoFisico>();
    public DbSet<Vuelo> Vuelos => Set<Vuelo>();
    public DbSet<HistorialEstadoVuelo> HistorialEstadosVuelo => Set<HistorialEstadoVuelo>();
    public DbSet<AsientoVuelo> AsientosVuelo => Set<AsientoVuelo>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<CompraSimulada> ComprasSimuladas => Set<CompraSimulada>();
    public DbSet<Vuelos_Backend.Models.Pagos.PagoPayPal> PagosPayPal => Set<Vuelos_Backend.Models.Pagos.PagoPayPal>();
    public DbSet<Vuelos_Backend.Models.Pagos.EventoPayPal> EventosPayPal => Set<Vuelos_Backend.Models.Pagos.EventoPayPal>();
    public DbSet<DetalleReserva> DetallesReserva => Set<DetalleReserva>();
    public DbSet<HistorialCambioReserva> HistorialCambiosReserva => Set<HistorialCambioReserva>();
    public DbSet<TokenSesion> Tokens => Set<TokenSesion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Configuration.BaseDatosPostgreSql.Esquema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        AmpliacionesConfiguration.Configure(modelBuilder);
        CatalogosConfiguration.Configure(modelBuilder);
        VersionesPostgreSqlConfiguration.Configure(modelBuilder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        RechazarBorradosFisicos();
        NormalizarFechasModificadas();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        RechazarBorradosFisicos();
        NormalizarFechasModificadas();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void NormalizarFechasModificadas()
    {
        foreach (var entry in ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified))
        foreach (var property in entry.Properties)
            if ((entry.State == EntityState.Added || property.IsModified) && property.CurrentValue is DateTime date)
                property.CurrentValue = FechasPostgreSql.Normalizar(date);
    }

    private void RechazarBorradosFisicos()
    {
        if (ChangeTracker.Entries().Any(entry => entry.State == EntityState.Deleted))
        {
            throw new InvalidOperationException(
                "No se permite eliminar registros físicamente. Utilice los estados de negocio o el borrado lógico.");
        }
    }
}

