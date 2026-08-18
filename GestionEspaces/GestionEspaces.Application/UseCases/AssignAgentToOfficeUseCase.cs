using FluentValidation;
using GestionEspaces.Application.Common;
using GestionEspaces.Application.DTOs.Assignments;
using GestionEspaces.Application.Interfaces;
using GestionEspaces.Application.Interfaces.Repositories;
using GestionEspaces.Domain.Exceptions;

namespace GestionEspaces.Application.UseCases;

/// <summary>
/// Assigns an agent to an office.
/// </summary>
public sealed class AssignAgentToOfficeUseCase
{
    private readonly IAgentRepository _agentRepository;
    private readonly IBureauRepository _bureauRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<AssignAgentToOfficeRequest> _validator;

    public AssignAgentToOfficeUseCase(
        IAgentRepository agentRepository,
        IBureauRepository bureauRepository,
        IUnitOfWork unitOfWork,
        IValidator<AssignAgentToOfficeRequest> validator)
    {
        _agentRepository = agentRepository;
        _bureauRepository = bureauRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<AssignmentUseCaseResult>> ExecuteAsync(AssignAgentToOfficeRequest request, CancellationToken cancellationToken)
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

        var bureau = await _bureauRepository.GetByIdAsync(request.BureauId, cancellationToken);
        if (bureau is null)
        {
            return Result<AssignmentUseCaseResult>.Failure(new ErrorDetail("BureauNotFound", $"Bureau {request.BureauId} introuvable.", nameof(request.BureauId)));
        }

        try
        {
            var affectation = agent.AffecterAuBureau(bureau, request.DateAffectation, request.Motif);
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