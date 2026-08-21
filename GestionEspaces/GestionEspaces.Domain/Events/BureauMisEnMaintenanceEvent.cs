using GestionEspaces.Domain.Common;

namespace GestionEspaces.Domain.Events;

public sealed record BureauMisEnMaintenanceEvent(int IdBureau, string Numero) : IDomainEvent;
