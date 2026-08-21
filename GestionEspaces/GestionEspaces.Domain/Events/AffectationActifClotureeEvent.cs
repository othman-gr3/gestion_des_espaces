using GestionEspaces.Domain.Common;

namespace GestionEspaces.Domain.Events;

public sealed record AffectationActifClotureeEvent(int IdAffectationActif, int IdAgent, int IdActif, DateTime DateFin) : IDomainEvent;
