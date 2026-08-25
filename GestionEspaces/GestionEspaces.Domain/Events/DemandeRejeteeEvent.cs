using GestionEspaces.Domain.Common;

namespace GestionEspaces.Domain.Events;

public sealed record DemandeRejeteeEvent(int IdDemande, int IdAgent, DateTime DateTraitement) : IDomainEvent;
