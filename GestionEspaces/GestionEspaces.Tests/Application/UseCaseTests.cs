using GestionEspaces.Application.DTOs.Agents;
using GestionEspaces.Application.DTOs.Assignments;
using GestionEspaces.Application.Interfaces;
using GestionEspaces.Application.Interfaces.Repositories;
using GestionEspaces.Application.UseCases;
using GestionEspaces.Application.Validation;
using GestionEspaces.Domain.Entities;

namespace GestionEspaces.Tests.Application;

public class UseCaseTests
{
    [Fact]
    public async Task CreateAgentUseCase_CreatesAnAgent()
    {
        var repository = new InMemoryAgentRepository();
        var unitOfWork = new InMemoryUnitOfWork();
        var useCase = new CreateAgentUseCase(repository, unitOfWork, new CreateAgentRequestValidator());

        var result = await useCase.ExecuteAsync(
            new CreateAgentRequest("Doe", "Jane", "MAT-100", "jane.doe@gestionespaces.local", null, null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("MAT-100", result.Value!.Matricule);
        Assert.Single(repository.StoredAgents);
        Assert.Equal(1, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task CreateAgentUseCase_RejectsDuplicateMatricule()
    {
        var repository = new InMemoryAgentRepository();
        repository.Seed(new Agent("Doe", "Jane", "MAT-100", null, null, null, null, null, null));

        var unitOfWork = new InMemoryUnitOfWork();
        var useCase = new CreateAgentUseCase(repository, unitOfWork, new CreateAgentRequestValidator());

        var result = await useCase.ExecuteAsync(
            new CreateAgentRequest("Smith", "John", "MAT-100", null, null, null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Code == "DuplicateMatricule");
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task AssignAgentToOfficeUseCase_AssignsAnOffice()
    {
        var agent = new Agent("Doe", "Jane", "MAT-100", null, null, null, null, null, null);
        var bureau = new Bureau("B-101", null, 1, 12.5f, 1, null, 7);
        SetIdentity(agent, nameof(Agent.IdAgent), 1);
        SetIdentity(bureau, nameof(Bureau.IdBureau), 10);

        var agentRepository = new InMemoryAgentRepository();
        agentRepository.Seed(agent);

        var bureauRepository = new InMemoryBureauRepository();
        bureauRepository.Seed(bureau);

        var unitOfWork = new InMemoryUnitOfWork();
        var useCase = new AssignAgentToOfficeUseCase(agentRepository, bureauRepository, unitOfWork, new AssignAgentToOfficeRequestValidator());

        var result = await useCase.ExecuteAsync(new AssignAgentToOfficeRequest(agent.IdAgent, bureau.IdBureau, DateTime.UtcNow), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(agent.AffectationsPoste);
        Assert.Equal(1, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task AssignAssetToAgentUseCase_AssignsAnAsset()
    {
        var agent = new Agent("Doe", "Jane", "MAT-100", null, null, null, null, null, null);
        var actif = new Actif("Laptop", "IT", "Dell", "Latitude", "SER-100", null, null);
        SetIdentity(agent, nameof(Agent.IdAgent), 1);
        SetIdentity(actif, nameof(Actif.IdActif), 20);

        var agentRepository = new InMemoryAgentRepository();
        agentRepository.Seed(agent);

        var actifRepository = new InMemoryActifRepository();
        actifRepository.Seed(actif);

        var unitOfWork = new InMemoryUnitOfWork();
        var useCase = new AssignAssetToAgentUseCase(agentRepository, actifRepository, unitOfWork, new AssignAssetToAgentRequestValidator());

        var result = await useCase.ExecuteAsync(new AssignAssetToAgentRequest(agent.IdAgent, actif.IdActif, DateTime.UtcNow), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(agent.AffectationsActif);
        Assert.Equal(1, unitOfWork.SaveCalls);
    }

    private sealed class InMemoryUnitOfWork : IUnitOfWork
    {
        public int SaveCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCalls += 1;
            return Task.FromResult(1);
        }
    }

    private sealed class InMemoryAgentRepository : IAgentRepository
    {
        private readonly Dictionary<int, Agent> _agents = new();

        public IReadOnlyCollection<Agent> StoredAgents => _agents.Values.ToArray();

        public void Seed(Agent agent)
        {
            _agents[agent.IdAgent] = agent;
        }

        public Task<Agent?> GetByIdAsync(int idAgent, CancellationToken cancellationToken)
        {
            _agents.TryGetValue(idAgent, out var agent);
            return Task.FromResult(agent);
        }

        public Task<Agent?> GetByEmailAsync(string email, CancellationToken cancellationToken)
        {
            var agent = _agents.Values.SingleOrDefault(a => string.Equals(a.Email, email, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(agent);
        }

        public Task<bool> ExistsByMatriculeAsync(string matricule, CancellationToken cancellationToken)
        {
            return Task.FromResult(_agents.Values.Any(agent => agent.Matricule == matricule));
        }

        public Task<bool> ExistsByMatriculeAsync(string matricule, int? excludingIdAgent, CancellationToken cancellationToken)
        {
            var exists = _agents.Values.Any(a =>
                a.Matricule == matricule && (!excludingIdAgent.HasValue || a.IdAgent != excludingIdAgent.Value));
            return Task.FromResult(exists);
        }

        public Task<IReadOnlyList<Agent>> SearchAsync(string? searchText, int pageNumber, int pageSize, CancellationToken cancellationToken)
        {
            IEnumerable<Agent> query = _agents.Values;
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var f = searchText.Trim();
                query = query.Where(a =>
                    a.Nom.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                    a.Prenom.Contains(f, StringComparison.OrdinalIgnoreCase));
            }

            var paged = query.OrderBy(a => a.Nom).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToArray();
            return Task.FromResult<IReadOnlyList<Agent>>(paged);
        }

        public Task<int> CountAsync(string? searchText, CancellationToken cancellationToken)
        {
            IEnumerable<Agent> query = _agents.Values;
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var f = searchText.Trim();
                query = query.Where(a =>
                    a.Nom.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                    a.Prenom.Contains(f, StringComparison.OrdinalIgnoreCase));
            }

            return Task.FromResult(query.Count());
        }

        public Task AddAsync(Agent agent, CancellationToken cancellationToken)
        {
            _agents[agent.IdAgent] = agent;
            return Task.CompletedTask;
        }

        public void Update(Agent agent)
        {
            _agents[agent.IdAgent] = agent;
        }

        public void Remove(Agent agent)
        {
            _agents.Remove(agent.IdAgent);
        }

        public void SetOriginalVersion(Agent agent, byte[] version)
        {
            // No-op for in-memory fakes — concurrency is not tested at this layer.
        }
    }

    private sealed class InMemoryBureauRepository : IBureauRepository
    {
        private readonly Dictionary<int, Bureau> _bureaux = new();

        public void Seed(Bureau bureau)
        {
            _bureaux[bureau.IdBureau] = bureau;
        }

        public Task<Bureau?> GetByIdAsync(int idBureau, CancellationToken cancellationToken)
        {
            _bureaux.TryGetValue(idBureau, out var bureau);
            return Task.FromResult(bureau);
        }

        public Task<IReadOnlyList<Bureau>> SearchAsync(int? idBatiment, string? searchText, StatutBureau? statut, int pageNumber, int pageSize, CancellationToken cancellationToken)
        {
            IEnumerable<Bureau> query = _bureaux.Values;
            if (idBatiment.HasValue)
            {
                query = query.Where(x => x.IdBatiment == idBatiment.Value);
            }

            if (statut.HasValue)
            {
                query = query.Where(x => x.Statut == statut.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(x => x.Numero.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }

            var paged = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToArray();
            return Task.FromResult<IReadOnlyList<Bureau>>(paged);
        }

        public Task<int> CountAsync(int? idBatiment, string? searchText, StatutBureau? statut, CancellationToken cancellationToken)
        {
            IEnumerable<Bureau> query = _bureaux.Values;
            if (idBatiment.HasValue)
            {
                query = query.Where(x => x.IdBatiment == idBatiment.Value);
            }

            if (statut.HasValue)
            {
                query = query.Where(x => x.Statut == statut.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(x => x.Numero.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }

            return Task.FromResult(query.Count());
        }

        public Task<bool> ExistsByNumeroAsync(int idBatiment, string numero, int? excludingIdBureau, CancellationToken cancellationToken)
        {
            var exists = _bureaux.Values.Any(x =>
                x.IdBatiment == idBatiment
                && string.Equals(x.Numero, numero, StringComparison.Ordinal)
                && (!excludingIdBureau.HasValue || x.IdBureau != excludingIdBureau.Value));

            return Task.FromResult(exists);
        }

        public Task AddAsync(Bureau bureau, CancellationToken cancellationToken)
        {
            _bureaux[bureau.IdBureau] = bureau;
            return Task.CompletedTask;
        }

        public void Update(Bureau bureau)
        {
            _bureaux[bureau.IdBureau] = bureau;
        }

        public void Remove(Bureau bureau)
        {
            _bureaux.Remove(bureau.IdBureau);
        }

        public void SetOriginalVersion(Bureau bureau, byte[] version)
        {
            // No-op for in-memory fakes — concurrency is not tested at this layer.
        }
    }

    private sealed class InMemoryActifRepository : IActifRepository
    {
        private readonly Dictionary<int, Actif> _actifs = new();

        public void Seed(Actif actif)
        {
            _actifs[actif.IdActif] = actif;
        }

        public Task<Actif?> GetByIdAsync(int idActif, CancellationToken cancellationToken)
        {
            _actifs.TryGetValue(idActif, out var actif);
            return Task.FromResult(actif);
        }

        public Task<IReadOnlyList<Actif>> SearchAsync(string? searchText, EtatActif? etat, int pageNumber, int pageSize, CancellationToken cancellationToken)
        {
            IEnumerable<Actif> query = _actifs.Values;
            if (etat.HasValue)
            {
                query = query.Where(x => x.Etat == etat.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(x => x.Nom.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }

            var paged = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToArray();
            return Task.FromResult<IReadOnlyList<Actif>>(paged);
        }

        public Task<int> CountAsync(string? searchText, EtatActif? etat, CancellationToken cancellationToken)
        {
            IEnumerable<Actif> query = _actifs.Values;
            if (etat.HasValue)
            {
                query = query.Where(x => x.Etat == etat.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(x => x.Nom.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }

            return Task.FromResult(query.Count());
        }

        public Task<bool> ExistsByNumeroSerieAsync(string numeroSerie, int? excludingIdActif, CancellationToken cancellationToken)
        {
            var exists = _actifs.Values.Any(x =>
                string.Equals(x.NumeroSerie, numeroSerie, StringComparison.Ordinal)
                && (!excludingIdActif.HasValue || x.IdActif != excludingIdActif.Value));

            return Task.FromResult(exists);
        }

        public Task AddAsync(Actif actif, CancellationToken cancellationToken)
        {
            _actifs[actif.IdActif] = actif;
            return Task.CompletedTask;
        }

        public void Update(Actif actif)
        {
            _actifs[actif.IdActif] = actif;
        }

        public void Remove(Actif actif)
        {
            _actifs.Remove(actif.IdActif);
        }

        public void SetOriginalVersion(Actif actif, byte[] version)
        {
            // No-op for in-memory fakes — concurrency is not tested at this layer.
        }
    }

    private static void SetIdentity<TEntity>(TEntity entity, string propertyName, int value)
    {
        var property = typeof(TEntity).GetProperty(propertyName);
        if (property is null)
        {
            throw new InvalidOperationException($"Property '{propertyName}' was not found.");
        }

        property.SetValue(entity, value);
    }
}