namespace GestionEspaces.Application.Interfaces;

/// <summary>
/// Persists a unit of work.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}