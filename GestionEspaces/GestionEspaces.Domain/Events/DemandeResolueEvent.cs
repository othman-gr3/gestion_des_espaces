using GestionEspaces.Domain.Common;

namespace GestionEspaces.Domain.Events;

public sealed record DemandeResolueEvent(int IdDemande, int IdAgent, DateTime DateTraitement) : IDomainEvent;
