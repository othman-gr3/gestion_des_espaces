using FluentValidation;
using GestionEspaces.Application.Common;
using GestionEspaces.Application.DTOs.Assignments;
using GestionEspaces.Application.Interfaces;
using GestionEspaces.Application.Interfaces.Repositories;
using GestionEspaces.Domain.Exceptions;

namespace GestionEspaces.Application.UseCases;

/// <summary>
/// Assigns an asset to an agent.
/// </summary>
public sealed class AssignAssetToAgentUseCase
{
    private readonly IAgentRepository _agentRepository;
    private readonly IActifRepository _actifRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<AssignAssetToAgentRequest> _validator;

    public AssignAssetToAgentUseCase(
        IAgentRepository agentRepository,
        IActifRepository actifRepository,
        IUnitOfWork unitOfWork,
        IValidator<AssignAssetToAgentRequest> validator)
    {
        _agentRepository = agentRepository;
        _actifRepository = actifRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<AssignmentUseCaseResult>> ExecuteAsync(AssignAssetToAgentRequest request, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<AssignmentUseCaseResult>.Failure(validation.Errors.Select(error => new ErrorDetail("ValidationError", error.ErrorMessage, error.PropertyName)));
        }

        var agent = await _agentRepository.GetByIdAsync(request.AgentId, cancellationToken);
        if (agent is null)
        {
            return Result<AssignmentUseCaseResult>.Failure(new ErrorDetail("AgentNotFound", $"Agent {request.AgentId} introuvable.", nameof(request.AgentId)));
        }

        var actif = await _actifRepository.GetByIdAsync(request.ActifId, cancellationToken);
        if (actif is null)
        {
            return Result<AssignmentUseCaseResult>.Failure(new ErrorDetail("ActifNotFound", $"Actif {request.ActifId} introuvable.", nameof(request.ActifId)));
        }

        try
        {
            var affectation = agent.AffecterActif(actif, request.DateAffectation);
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