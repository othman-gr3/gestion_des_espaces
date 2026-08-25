using GestionEspaces.Domain.Common;

namespace GestionEspaces.Domain.Events;

public sealed record DemandeCreeeEvent(int IdAgent, string Type, DateTime DateCreation) : IDomainEvent;
