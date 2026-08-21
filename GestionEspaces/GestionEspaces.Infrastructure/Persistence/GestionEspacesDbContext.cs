using System.Text.Json;
using GestionEspaces.Application.Interfaces;
using GestionEspaces.Domain.Common;
using GestionEspaces.Domain.Entities;
using GestionEspaces.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GestionEspaces.Infrastructure.Persistence;

/// <summary>
/// EF Core database context for GestionEspaces.
/// </summary>
public sealed class GestionEspacesDbContext : DbContext
{
    private readonly ILogger<GestionEspacesDbContext> _logger;
    private readonly ICurrentUserContext _currentUserContext;

    public GestionEspacesDbContext(
        DbContextOptions<GestionEspacesDbContext> options,
        ILogger<GestionEspacesDbContext>? logger = null,
        ICurrentUserContext? currentUserContext = null)
        : base(options)
    {
        // Optional so EF design-time tooling and test fixtures can keep constructing this
        // context directly (single-argument call) without going through DI.
        _logger = logger ?? NullLogger<GestionEspacesDbContext>.Instance;
        _currentUserContext = currentUserContext ?? new AnonymousCurrentUserContext();
    }

    public DbSet<Site> Sites => Set<Site>();

    public DbSet<Batiment> Batiments => Set<Batiment>();

    public DbSet<Bureau> Bureaux => Set<Bureau>();

    public DbSet<Agent> Agents => Set<Agent>();

    public DbSet<Actif> Actifs => Set<Actif>();

    public DbSet<AffectationPoste> AffectationsPoste => Set<AffectationPoste>();

    public DbSet<AffectationActif> AffectationsActif => Set<AffectationActif>();

    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GestionEspacesDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Collects domain events raised on tracked aggregates during this unit of work and
    /// turns each into an <see cref="AuditLogEntry"/> added to the same save — so the audit
    /// trail commits atomically with the business change it describes, in one transaction.
    /// Console logging happens only once that save has actually succeeded, so an event is
    /// never reported for a change that didn't persist.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entitiesWithEvents = ChangeTracker.Entries<EntityBase>()
            .Select(entry => entry.Entity)
            .Where(entity => entity.GetDomainEvents().Count > 0)
            .ToList();

        if (entitiesWithEvents.Count > 0)
        {
            var userEmail = _currentUserContext.Email;
            var userRole = _currentUserContext.Role;
            var occurredOnUtc = DateTime.UtcNow;

            foreach (var entity in entitiesWithEvents)
            {
                foreach (var domainEvent in entity.GetDomainEvents())
                {
                    var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());
                    AuditLog.Add(new AuditLogEntry(occurredOnUtc, domainEvent.GetType().Name, payload, userEmail, userRole));
                }
            }
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var entity in entitiesWithEvents)
        {
            foreach (var domainEvent in entity.GetDomainEvents())
            {
                _logger.LogInformation("Domain event raised: {DomainEvent}", domainEvent);
            }

            entity.ClearDomainEvents();
        }

        return result;
    }
}