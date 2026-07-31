namespace GestionEspaces.Domain.Exceptions;

/// <summary>
/// Thrown when a write operation is rejected because the resource has been
/// modified by another request since the client last read it (optimistic concurrency).
/// </summary>
public sealed class ConcurrencyConflictException : DomainException
{
    public ConcurrencyConflictException(string resourceType, object id)
        : base($"La ressource '{resourceType}' ({id}) a été modifiée entre-temps. Veuillez recharger et réessayer.")
    {
    }
}
