using GestionEspaces.Domain.Common;

namespace GestionEspaces.Domain.Events;

public sealed record BureauRemisEnServiceEvent(int IdBureau, string Numero) : IDomainEvent;
