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
    private readonly IUnitOfWork _unitOfWork;

    public CloseAffectationActifUseCase(IAgentRepository agentRepository, IUnitOfWork unitOfWork)
    {
        _agentRepository = agentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AssignmentUseCaseResult>> ExecuteAsync(int agentId, int idAffectationActif, CloseAffectationRequest request, CancellationToken cancellationToken)
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
            agent.CloreAffectationActif(idAffectationActif, request.DateFin);
            _agentRepository.Update(agent);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var affectation = agent.AffectationsActif.Single(a => a.IdAffectationActif == idAffectationActif);
            return Result<AssignmentUseCaseResult>.Success(affectation.ToResult());
        }
        catch (BusinessRuleViolationException exception)
        {
            return Result<AssignmentUseCaseResult>.Failure(new ErrorDetail("BusinessRuleViolation", exception.Message));
        }
    }
}
