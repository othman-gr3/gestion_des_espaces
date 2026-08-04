using FluentValidation;
using GestionEspaces.Application.Common;
using GestionEspaces.Application.DTOs.Assignments;
using GestionEspaces.Application.Interfaces;
using GestionEspaces.Application.Interfaces.Repositories;
using GestionEspaces.Domain.Exceptions;

namespace GestionEspaces.Application.UseCases;

/// <summary>
/// Closes an active poste affectation for an agent.
/// </summary>
public sealed class CloseAffectationPosteUseCase
{
    private readonly IAgentRepository _agentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CloseAffectationPosteUseCase(IAgentRepository agentRepository, IUnitOfWork unitOfWork)
    {
        _agentRepository = agentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AssignmentUseCaseResult>> ExecuteAsync(int agentId, int idAffectationPoste, CloseAffectationRequest request, CancellationToken cancellationToken)
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
            agent.CloreAffectationPoste(idAffectationPoste, request.DateFin);
            _agentRepository.Update(agent);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var affectation = agent.AffectationsPoste.Single(a => a.IdAffectationPoste == idAffectationPoste);
            return Result<AssignmentUseCaseResult>.Success(affectation.ToResult());
        }
        catch (BusinessRuleViolationException exception)
        {
            return Result<AssignmentUseCaseResult>.Failure(new ErrorDetail("BusinessRuleViolation", exception.Message));
        }
    }
}
