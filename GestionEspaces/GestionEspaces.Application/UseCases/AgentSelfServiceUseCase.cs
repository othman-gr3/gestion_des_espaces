using FluentValidation;
using GestionEspaces.Application.Common;
using GestionEspaces.Application.DTOs.Actifs;
using GestionEspaces.Application.DTOs.Agents;
using GestionEspaces.Application.DTOs.Bureaux;
using GestionEspaces.Application.DTOs.Demandes;
using GestionEspaces.Application.DTOs.SelfService;
using GestionEspaces.Application.Interfaces;
using GestionEspaces.Application.Interfaces.Repositories;
using GestionEspaces.Domain.Entities;

namespace GestionEspaces.Application.UseCases;

/// <summary>
/// Self-service operations for the Agent role: an agent may only ever see or change data
/// tied to their own JWT-authenticated identity (matched by email), never an arbitrary
/// agent id from the URL.
/// </summary>
public sealed class AgentSelfServiceUseCase
{
    private readonly IAgentRepository _agentRepository;
    private readonly IAffectationRepository _affectationRepository;
    private readonly IDemandeRepository _demandeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateMyProfileRequest> _updateProfileValidator;
    private readonly IValidator<CreateDemandeRequest> _createDemandeValidator;

    public AgentSelfServiceUseCase(
        IAgentRepository agentRepository,
        IAffectationRepository affectationRepository,
        IDemandeRepository demandeRepository,
        IUnitOfWork unitOfWork,
        IValidator<UpdateMyProfileRequest> updateProfileValidator,
        IValidator<CreateDemandeRequest> createDemandeValidator)
    {
        _agentRepository = agentRepository;
        _affectationRepository = affectationRepository;
        _demandeRepository = demandeRepository;
        _unitOfWork = unitOfWork;
        _updateProfileValidator = updateProfileValidator;
        _createDemandeValidator = createDemandeValidator;
    }

    public async Task<Result<AgentDto>> GetMyProfileAsync(string email, CancellationToken cancellationToken)
    {
        var agent = await _agentRepository.GetByEmailAsync(email, cancellationToken);
        if (agent is null)
        {
            return Result<AgentDto>.Failure(new ErrorDetail("AgentNotFound", "Aucun agent associé à ce compte."));
        }

        return Result<AgentDto>.Success(agent.ToDto());
    }

    public async Task<Result<BureauDto?>> GetMyOfficeAsync(string email, CancellationToken cancellationToken)
    {
        var agent = await _agentRepository.GetByEmailAsync(email, cancellationToken);
        if (agent is null)
        {
            return Result<BureauDto?>.Failure(new ErrorDetail("AgentNotFound", "Aucun agent associé à ce compte."));
        }

        var postes = await _affectationRepository.GetPostesForAgentAsync(agent.IdAgent, cancellationToken);
        var activeOffice = postes.FirstOrDefault(p => p.EstActive);

        return Result<BureauDto?>.Success(activeOffice?.Bureau.ToDto());
    }

    public async Task<Result<IReadOnlyList<ActifDto>>> GetMyAssetsAsync(string email, CancellationToken cancellationToken)
    {
        var agent = await _agentRepository.GetByEmailAsync(email, cancellationToken);
        if (agent is null)
        {
            return Result<IReadOnlyList<ActifDto>>.Failure(new ErrorDetail("AgentNotFound", "Aucun agent associé à ce compte."));
        }

        var affectations = await _affectationRepository.GetActifsForAgentAsync(agent.IdAgent, cancellationToken);
        var activeAssets = affectations
            .Where(a => a.EstActive)
            .Select(a => a.Actif.ToDto())
            .ToArray();

        return Result<IReadOnlyList<ActifDto>>.Success(activeAssets);
    }

    public async Task<Result<MyHistoryResponse>> GetMyHistoryAsync(string email, CancellationToken cancellationToken)
    {
        var agent = await _agentRepository.GetByEmailAsync(email, cancellationToken);
        if (agent is null)
        {
            return Result<MyHistoryResponse>.Failure(new ErrorDetail("AgentNotFound", "Aucun agent associé à ce compte."));
        }

        var postes = await _affectationRepository.GetPostesForAgentAsync(agent.IdAgent, cancellationToken);
        var actifs = await _affectationRepository.GetActifsForAgentAsync(agent.IdAgent, cancellationToken);

        var posteHistory = postes
            .OrderByDescending(p => p.DateAffectation)
            .Select(p => new MyPosteHistoryDto(p.IdAffectationPoste, p.Bureau.ToDto(), p.DateAffectation, p.DateFin, p.Motif, p.EstActive))
            .ToArray();

        var actifHistory = actifs
            .OrderByDescending(a => a.DateAffectation)
            .Select(a => new MyActifHistoryDto(a.IdAffectationActif, a.Actif.ToDto(), a.DateAffectation, a.DateFin, a.EstActive))
            .ToArray();

        return Result<MyHistoryResponse>.Success(new MyHistoryResponse(posteHistory, actifHistory));
    }

    public async Task<Result<AgentDto>> UpdateMyProfileAsync(string email, UpdateMyProfileRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateProfileValidator.ValidateAsync(request, cancellationToken);
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

        var agent = await _agentRepository.GetByEmailAsync(email, cancellationToken);
        if (agent is null)
        {
            return Result<AgentDto>.Failure(new ErrorDetail("AgentNotFound", "Aucun agent associé à ce compte."));
        }

        _agentRepository.SetOriginalVersion(agent, tokenBytes);
        agent.MettreAJourTelephone(request.Telephone);

        _agentRepository.Update(agent);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<AgentDto>.Success(agent.ToDto());
    }

    public async Task<Result<DemandeDto>> CreateMyDemandeAsync(string email, CreateDemandeRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createDemandeValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<DemandeDto>.Failure(validation.Errors.Select(e => new ErrorDetail("ValidationError", e.ErrorMessage, e.PropertyName)));
        }

        var agent = await _agentRepository.GetByEmailAsync(email, cancellationToken);
        if (agent is null)
        {
            return Result<DemandeDto>.Failure(new ErrorDetail("AgentNotFound", "Aucun agent associé à ce compte."));
        }

        var demande = new DemandeAgent(agent, request.Type, request.Description, DateTime.UtcNow);

        await _demandeRepository.AddAsync(demande, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<DemandeDto>.Success(demande.ToDto());
    }

    public async Task<Result<IReadOnlyList<DemandeDto>>> GetMyDemandesAsync(string email, CancellationToken cancellationToken)
    {
        var agent = await _agentRepository.GetByEmailAsync(email, cancellationToken);
        if (agent is null)
        {
            return Result<IReadOnlyList<DemandeDto>>.Failure(new ErrorDetail("AgentNotFound", "Aucun agent associé à ce compte."));
        }

        var demandes = await _demandeRepository.GetByAgentIdAsync(agent.IdAgent, cancellationToken);
        var items = demandes
            .OrderByDescending(d => d.DateCreation)
            .Select(d => d.ToDto())
            .ToArray();

        return Result<IReadOnlyList<DemandeDto>>.Success(items);
    }
}
