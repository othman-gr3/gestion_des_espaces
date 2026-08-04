using GestionEspaces.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionEspaces.Infrastructure.Persistence;

/// <summary>
/// EF Core database context for GestionEspaces.
/// </summary>
public sealed class GestionEspacesDbContext : DbContext
{
    public GestionEspacesDbContext(DbContextOptions<GestionEspacesDbContext> options)
        : base(options)
    {
    }

    public DbSet<Site> Sites => Set<Site>();

    public DbSet<Batiment> Batiments => Set<Batiment>();

    public DbSet<Bureau> Bureaux => Set<Bureau>();

    public DbSet<Agent> Agents => Set<Agent>();

    public DbSet<Actif> Actifs => Set<Actif>();

    public DbSet<AffectationPoste> AffectationsPoste => Set<AffectationPoste>();

    public DbSet<AffectationActif> AffectationsActif => Set<AffectationActif>();

    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GestionEspacesDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}