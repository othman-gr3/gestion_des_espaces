using GestionEspaces.Application.Common;
using GestionEspaces.Application.DTOs.Assignments;
using GestionEspaces.Application.Interfaces;
using GestionEspaces.Application.Interfaces.Repositories;
using GestionEspaces.Domain.Exceptions;

namespace GestionEspaces.Application.UseCases;

/// <summary>
/// Closes an active actif affectation for an agent.
/// </summary>
public sealed class CloseAffectationActifUseCase
{
    private readonly IAgentRepository _agentRepository;
    private readonly IActifRepository _actifRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CloseAffectationActifUseCase(IAgentRepository agentRepository, IActifRepository actifRepository, IUnitOfWork unitOfWork)
    {
        _agentRepository = agentRepository;
        _actifRepository = actifRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AssignmentUseCaseResult>> ExecuteAsync(int agentId, int idAffectationActif, CloseAffectationActifRequest request, CancellationToken cancellationToken)
    {
        if (request.DateFin == default)
        {
            return Result<AssignmentUseCaseResult>.Failure(new ErrorDetail("ValidationError", "La date de fin est obligatoire.", nameof(request.DateFin)));
        }

        var agent = await _agentRepository.GetByIdAsync(agentId, cancellationToken);
        if (agent is null)
        {
            return Result<AssignmentUseCaseResult>.Failure(new ErrorDetail("AgentNotFound", $"Agent {agentId} introuvable."));
        }

        try
        {
            agent.CloreAffectationActif(idAffectationActif, request.DateFin, request.EtatRetour);
            var affectation = agent.AffectationsActif.Single(a => a.IdAffectationActif == idAffectationActif);

            // EtatRetour is now persisted on the affectation itself (the historical record of
            // this specific handover) — this also updates the actif's current Etat, a separate
            // concern (what condition it's actually in right now, for future assignments).
            if (request.EtatRetour.HasValue)
            {
                var actif = await _actifRepository.GetByIdAsync(affectation.IdActif, cancellationToken);
                if (actif is not null)
                {
                    switch (request.EtatRetour.Value)
                    {
                        case Domain.Entities.EtatActif.Bon: actif.MarquerBonEtat(); break;
                        case Domain.Entities.EtatActif.ARepairer: actif.MarquerARepairer(); break;
                        case Domain.Entities.EtatActif.HorsService: actif.MarquerHorsService(); break;
                    }

                    _actifRepository.Update(actif);
                }
            }

            _agentRepository.Update(agent);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<AssignmentUseCaseResult>.Success(affectation.ToResult());
        }
        catch (BusinessRuleViolationException exception)
        {
            return Result<AssignmentUseCaseResult>.Failure(new ErrorDetail("BusinessRuleViolation", exception.Message));
        }
    }
}
