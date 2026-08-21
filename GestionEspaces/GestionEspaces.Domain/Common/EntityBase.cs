namespace GestionEspaces.Domain.Common;

/// <summary>
/// Base for aggregates that raise domain events. Deliberately exposes the event list only
/// through methods (never a public property) so EF Core's convention-based model builder
/// never mistakes it for a mapped navigation — no [NotMapped]/Ignore() needed anywhere.
/// </summary>
public abstract class EntityBase
{
    private readonly List<IDomainEvent> _domainEvents = new();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();
}
