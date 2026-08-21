using GestionEspaces.Domain.Common;

namespace GestionEspaces.Domain.Events;

public sealed record AffectationPosteClotureeEvent(int IdAffectationPoste, int IdAgent, int IdBureau, DateTime DateFin) : IDomainEvent;
