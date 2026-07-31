using FluentValidation;
using GestionEspaces.Application.Common;
using GestionEspaces.Application.DTOs.Agents;
using GestionEspaces.Application.Interfaces;
using GestionEspaces.Application.Interfaces.Repositories;
using GestionEspaces.Domain.Entities;
using GestionEspaces.Domain.Exceptions;

namespace GestionEspaces.Application.UseCases;

/// <summary>
/// Creates a new agent.
/// </summary>
public sealed class CreateAgentUseCase
{
    private readonly IAgentRepository _agentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateAgentRequest> _validator;

    public CreateAgentUseCase(
        IAgentRepository agentRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateAgentRequest> validator)
    {
        _agentRepository = agentRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<AgentDto>> ExecuteAsync(CreateAgentRequest request, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<AgentDto>.Failure(validation.Errors.Select(error => new ErrorDetail("ValidationError", error.ErrorMessage, error.PropertyName)));
        }

        if (await _agentRepository.ExistsByMatriculeAsync(request.Matricule, cancellationToken))
        {
            return Result<AgentDto>.Failure(new ErrorDetail("DuplicateMatricule", $"Un agent avec le matricule '{request.Matricule}' existe déjà.", nameof(request.Matricule)));
        }

        var agent = new Agent(
            request.Nom.Trim(),
            request.Prenom.Trim(),
            request.Matricule.Trim(),
            request.Email?.Trim(),
            request.Telephone?.Trim(),
            request.Fonction?.Trim(),
            request.Departement?.Trim(),
            request.DateEmbauche,
            request.Image?.Trim());

        try
        {
            await _agentRepository.AddAsync(agent, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (BusinessRuleViolationException exception)
        {
            return Result<AgentDto>.Failure(new ErrorDetail("BusinessRuleViolation", exception.Message));
        }

        return Result<AgentDto>.Success(agent.ToDto());
    }
}