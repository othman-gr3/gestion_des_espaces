using FluentValidation;
using GestionEspaces.Application.Common;
using GestionEspaces.Application.DTOs.Agents;
using GestionEspaces.Application.Interfaces;
using GestionEspaces.Application.Interfaces.Repositories;
using GestionEspaces.Domain.Entities;

namespace GestionEspaces.Application.UseCases;

/// <summary>
/// Full CRUD use cases for agents (mirrors SiteUseCases / BureauUseCases pattern).
/// </summary>
public sealed class AgentUseCases
{
    private readonly IAgentRepository _agentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateAgentRequest> _createValidator;
    private readonly IValidator<UpdateAgentRequest> _updateValidator;
    private readonly IValidator<SearchAgentsRequest> _searchValidator;

    public AgentUseCases(
        IAgentRepository agentRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateAgentRequest> createValidator,
        IValidator<UpdateAgentRequest> updateValidator,
        IValidator<SearchAgentsRequest> searchValidator)
    {
        _agentRepository = agentRepository;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _searchValidator = searchValidator;
    }

    public async Task<Result<AgentDto>> CreateAsync(CreateAgentRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<AgentDto>.Failure(validation.Errors.Select(e => new ErrorDetail("ValidationError", e.ErrorMessage, e.PropertyName)));
        }

        if (await _agentRepository.ExistsByMatriculeAsync(request.Matricule, null, cancellationToken))
        {
            return Result<AgentDto>.Failure(new ErrorDetail("DuplicateMatricule", $"Un agent avec le matricule '{request.Matricule}' existe déjà.", nameof(request.Matricule)));
        }

        if (!string.IsNullOrWhiteSpace(request.Email) && await _agentRepository.ExistsByEmailAsync(request.Email, null, cancellationToken))
        {
            return Result<AgentDto>.Failure(new ErrorDetail("DuplicateEmail", $"Un agent avec l'email '{request.Email}' existe déjà.", nameof(request.Email)));
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

        await _agentRepository.AddAsync(agent, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AgentDto>.Success(agent.ToDto());
    }

    public async Task<Result<AgentDto>> UpdateAsync(int idAgent, UpdateAgentRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<AgentDto>.Failure(validation.Errors.Select(e => new ErrorDetail("ValidationError", e.ErrorMessage, e.PropertyName)));
        }

        byte[] tokenBytes;
        try
        {
            tokenBytes = Convert.FromBase64String(request.ConcurrencyToken);
        }
        catch (FormatException)
        {
            return Result<AgentDto>.Failure(new ErrorDetail("ValidationError", "Le jeton de concurrence est invalide.", nameof(request.ConcurrencyToken)));
        }

        var agent = await _agentRepository.GetByIdAsync(idAgent, cancellationToken);
        if (agent is null)
        {
            return Result<AgentDto>.Failure(new ErrorDetail("AgentNotFound", $"Agent {idAgent} introuvable."));
        }

        if (await _agentRepository.ExistsByMatriculeAsync(request.Matricule, idAgent, cancellationToken))
        {
            return Result<AgentDto>.Failure(new ErrorDetail("DuplicateMatricule", $"Un agent avec le matricule '{request.Matricule}' existe déjà.", nameof(request.Matricule)));
        }

        if (!string.IsNullOrWhiteSpace(request.Email) && await _agentRepository.ExistsByEmailAsync(request.Email, idAgent, cancellationToken))
        {
            return Result<AgentDto>.Failure(new ErrorDetail("DuplicateEmail", $"Un agent avec l'email '{request.Email}' existe déjà.", nameof(request.Email)));
        }

        _agentRepository.SetOriginalVersion(agent, tokenBytes);

        agent.MettreAJour(
            request.Nom.Trim(),
            request.Prenom.Trim(),
            request.Matricule.Trim(),
            request.Email?.Trim(),
            request.Telephone?.Trim(),
            request.Fonction?.Trim(),
            request.Departement?.Trim(),
            request.DateEmbauche,
            request.Image?.Trim());

        _agentRepository.Update(agent);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AgentDto>.Success(agent.ToDto());
    }

    public async Task<Result<AgentDto>> GetByIdAsync(int idAgent, CancellationToken cancellationToken)
    {
        var agent = await _agentRepository.GetByIdAsync(idAgent, cancellationToken);
        if (agent is null)
        {
            return Result<AgentDto>.Failure(new ErrorDetail("AgentNotFound", $"Agent {idAgent} introuvable."));
        }

        return Result<AgentDto>.Success(agent.ToDto());
    }

    public async Task<Result<PagedResult<AgentDto>>> SearchAsync(SearchAgentsRequest request, CancellationToken cancellationToken)
    {
        var validation = await _searchValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<PagedResult<AgentDto>>.Failure(validation.Errors.Select(e => new ErrorDetail("ValidationError", e.ErrorMessage, e.PropertyName)));
        }

        var items = await _agentRepository.SearchAsync(request.SearchText, request.PageNumber, request.PageSize, cancellationToken);
        var totalCount = await _agentRepository.CountAsync(request.SearchText, cancellationToken);

        return Result<PagedResult<AgentDto>>.Success(new PagedResult<AgentDto>(
            items.Select(item => item.ToDto()).ToArray(),
            request.PageNumber,
            request.PageSize,
            totalCount));
    }

    public async Task<Result> DeleteAsync(int idAgent, CancellationToken cancellationToken)
    {
        var agent = await _agentRepository.GetByIdAsync(idAgent, cancellationToken);
        if (agent is null)
        {
            return Result.Failure(new ErrorDetail("AgentNotFound", $"Agent {idAgent} introuvable."));
        }

        _agentRepository.SetOriginalVersion(agent, agent.Version);
        _agentRepository.Remove(agent);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
