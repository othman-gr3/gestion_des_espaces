using GestionEspaces.Application.Interfaces;
using GestionEspaces.Domain.Exceptions;
using GestionEspaces.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestionEspaces.Infrastructure.Repositories;

/// <summary>
/// Persists EF Core changes as a unit of work, translating <see cref="DbUpdateConcurrencyException"/>
/// into the domain-level <see cref="ConcurrencyConflictException"/> so callers stay EF-free.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly GestionEspacesDbContext _dbContext;

    public UnitOfWork(GestionEspacesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Extract the first conflicted entry for a readable error message.
            var entry = ex.Entries.FirstOrDefault();
            var resourceType = entry?.Metadata.ClrType.Name ?? "Resource";
            var id = entry?.CurrentValues.Properties
                .FirstOrDefault(p => p.IsPrimaryKey())
                ?.Name ?? "?";

            throw new ConcurrencyConflictException(resourceType, id);
        }
    }
}