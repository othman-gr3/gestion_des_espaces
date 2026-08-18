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
    private readonly IBureauRepository _bureauRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CloseAffectationPosteUseCase(IAgentRepository agentRepository, IBureauRepository bureauRepository, IUnitOfWork unitOfWork)
    {
        _agentRepository = agentRepository;
        _bureauRepository = bureauRepository;
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
            var affectation = agent.AffectationsPoste.Single(a => a.IdAffectationPoste == idAffectationPoste);

            // The Agent aggregate doesn't eagerly load the Bureau navigation property, so free
            // the office up (back to Disponible) via its own repository instead.
            var bureau = await _bureauRepository.GetByIdAsync(affectation.IdBureau, cancellationToken);
            if (bureau is not null)
            {
                bureau.RemettreEnService();
                _bureauRepository.Update(bureau);
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
