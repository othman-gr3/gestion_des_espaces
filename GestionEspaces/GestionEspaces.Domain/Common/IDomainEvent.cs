namespace GestionEspaces.Domain.Common;

/// <summary>
/// Marker for something meaningful that happened to an aggregate, raised from within its
/// own business methods and dispatched (currently: logged) after a successful save.
/// </summary>
public interface IDomainEvent
{
}
