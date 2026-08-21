using GestionEspaces.Domain.Common;

namespace GestionEspaces.Domain.Events;

public sealed record ActifAffecteEvent(int IdActif, int IdAgent, DateTime DateAffectation) : IDomainEvent;
