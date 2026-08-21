using GestionEspaces.Domain.Common;

namespace GestionEspaces.Domain.Events;

public sealed record AgentAffecteAuBureauEvent(int IdAgent, int IdBureau, string NumeroBureau, DateTime DateAffectation) : IDomainEvent;
