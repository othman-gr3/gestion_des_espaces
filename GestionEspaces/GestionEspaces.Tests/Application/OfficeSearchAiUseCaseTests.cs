using GestionEspaces.Application.DTOs.OfficeSearchAi;
using GestionEspaces.Application.Interfaces;
using GestionEspaces.Application.Interfaces.Repositories;
using GestionEspaces.Application.UseCases;
using GestionEspaces.Domain.Entities;
using GestionEspaces.Domain.ValueObjects;

namespace GestionEspaces.Tests.Application;

public class OfficeSearchAiUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenAssistantReturnsCriteria_FiltersByThem()
    {
        var (bureauRepository, batimentRepository, siteRepository) = BuildSeededRepositories();
        var assistant = new StubAssistant(new OfficeSearchCriteria(IdBatiment: 7, Statut: 0, Type: null, CapaciteMin: 4, EtageMin: null, Summary: "Bureaux disponibles au bâtiment A avec au moins 4 places."));
        var useCase = new OfficeSearchAiUseCase(assistant, bureauRepository, batimentRepository, siteRepository);

        var result = await useCase.ExecuteAsync("un bureau pour 4 personnes au bâtiment A", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.UsedAi);
        Assert.All(result.Value!.Results, bureau => Assert.True(bureau.Capacite >= 4));
        Assert.All(result.Value!.Results, bureau => Assert.Equal(StatutBureau.Disponible, bureau.Statut));
    }

    [Fact]
    public async Task ExecuteAsync_WhenAssistantUnavailable_FallsBackToKeywordSearch()
    {
        var (bureauRepository, batimentRepository, siteRepository) = BuildSeededRepositories();
        var assistant = new StubAssistant(null);
        var useCase = new OfficeSearchAiUseCase(assistant, bureauRepository, batimentRepository, siteRepository);

        var result = await useCase.ExecuteAsync("B-101", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.UsedAi);
        Assert.Contains(result.Value!.Results, bureau => bureau.Numero == "B-101");
    }

    [Fact]
    public async Task ExecuteAsync_WithBlankQuery_Fails()
    {
        var (bureauRepository, batimentRepository, siteRepository) = BuildSeededRepositories();
        var useCase = new OfficeSearchAiUseCase(new StubAssistant(null), bureauRepository, batimentRepository, siteRepository);

        var result = await useCase.ExecuteAsync("   ", CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    private static (InMemoryBureauRepository, InMemoryBatimentRepository, InMemorySiteRepository) BuildSeededRepositories()
    {
        var site = new Site("Siège", "SIEGE", new AdresseSite("1 rue Test", "Casablanca", "20000", "Maroc"), null, null, null);
        SetIdentity(site, nameof(Site.IdSite), 3);

        var batiment = new Batiment("Bâtiment A", 4, 500f, null, site.IdSite);
        SetIdentity(batiment, nameof(Batiment.IdBatiment), 7);

        var bureauA = new Bureau("B-101", TypeBureau.Individuel, 2, 12f, 1, null, batiment.IdBatiment);
        SetIdentity(bureauA, nameof(Bureau.IdBureau), 101);

        var bureauB = new Bureau("B-102", TypeBureau.OpenSpace, 6, 30f, 1, null, batiment.IdBatiment);
        SetIdentity(bureauB, nameof(Bureau.IdBureau), 102);

        var siteRepository = new InMemorySiteRepository();
        siteRepository.Seed(site);

        var batimentRepository = new InMemoryBatimentRepository();
        batimentRepository.Seed(batiment);

        var bureauRepository = new InMemoryBureauRepository();
        bureauRepository.Seed(bureauA);
        bureauRepository.Seed(bureauB);

        return (bureauRepository, batimentRepository, siteRepository);
    }

    private static void SetIdentity<TEntity>(TEntity entity, string propertyName, int value)
    {
        var property = typeof(TEntity).GetProperty(propertyName)
            ?? throw new InvalidOperationException($"Property '{propertyName}' was not found.");
        property.SetValue(entity, value);
    }

    private sealed class StubAssistant : IOfficeSearchAssistant
    {
        private readonly OfficeSearchCriteria? _criteria;

        public StubAssistant(OfficeSearchCriteria? criteria) => _criteria = criteria;

        public Task<OfficeSearchCriteria?> InterpretAsync(string query, IReadOnlyList<BatimentOption> availableBatiments, CancellationToken cancellationToken)
            => Task.FromResult(_criteria);
    }

    private sealed class InMemoryBureauRepository : IBureauRepository
    {
        private readonly Dictionary<int, Bureau> _bureaux = new();

        public void Seed(Bureau bureau) => _bureaux[bureau.IdBureau] = bureau;

        public Task<Bureau?> GetByIdAsync(int idBureau, CancellationToken cancellationToken)
        {
            _bureaux.TryGetValue(idBureau, out var bureau);
            return Task.FromResult(bureau);
        }

        public Task<IReadOnlyList<Bureau>> SearchAsync(int? idBatiment, string? searchText, StatutBureau? statut, int pageNumber, int pageSize, CancellationToken cancellationToken)
        {
            IEnumerable<Bureau> query = _bureaux.Values;
            if (idBatiment.HasValue) query = query.Where(x => x.IdBatiment == idBatiment.Value);
            if (statut.HasValue) query = query.Where(x => x.Statut == statut.Value);
            if (!string.IsNullOrWhiteSpace(searchText)) query = query.Where(x => x.Numero.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult<IReadOnlyList<Bureau>>(query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToArray());
        }

        public Task<int> CountAsync(int? idBatiment, string? searchText, StatutBureau? statut, CancellationToken cancellationToken)
            => Task.FromResult(_bureaux.Count);

        public Task<bool> ExistsByNumeroAsync(int idBatiment, string numero, int? excludingIdBureau, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task AddAsync(Bureau bureau, CancellationToken cancellationToken)
        {
            _bureaux[bureau.IdBureau] = bureau;
            return Task.CompletedTask;
        }

        public void Update(Bureau bureau) => _bureaux[bureau.IdBureau] = bureau;
        public void Remove(Bureau bureau) => _bureaux.Remove(bureau.IdBureau);
        public void SetOriginalVersion(Bureau bureau, byte[] version) { }
    }

    private sealed class InMemoryBatimentRepository : IBatimentRepository
    {
        private readonly Dictionary<int, Batiment> _batiments = new();

        public void Seed(Batiment batiment) => _batiments[batiment.IdBatiment] = batiment;

        public Task<Batiment?> GetByIdAsync(int idBatiment, CancellationToken cancellationToken)
        {
            _batiments.TryGetValue(idBatiment, out var batiment);
            return Task.FromResult(batiment);
        }

        public Task<IReadOnlyList<Batiment>> SearchAsync(int? idSite, string? searchText, int pageNumber, int pageSize, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Batiment>>(_batiments.Values.ToArray());

        public Task<int> CountAsync(int? idSite, string? searchText, CancellationToken cancellationToken)
            => Task.FromResult(_batiments.Count);

        public Task<bool> ExistsByNameForSiteAsync(int idSite, string nom, int? excludingIdBatiment, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task AddAsync(Batiment batiment, CancellationToken cancellationToken)
        {
            _batiments[batiment.IdBatiment] = batiment;
            return Task.CompletedTask;
        }

        public void Update(Batiment batiment) => _batiments[batiment.IdBatiment] = batiment;
        public void Remove(Batiment batiment) => _batiments.Remove(batiment.IdBatiment);
        public void SetOriginalVersion(Batiment batiment, byte[] version) { }
    }

    private sealed class InMemorySiteRepository : ISiteRepository
    {
        private readonly Dictionary<int, Site> _sites = new();

        public void Seed(Site site) => _sites[site.IdSite] = site;

        public Task<Site?> GetByIdAsync(int idSite, CancellationToken cancellationToken)
        {
            _sites.TryGetValue(idSite, out var site);
            return Task.FromResult(site);
        }

        public Task<IReadOnlyList<Site>> SearchAsync(string? searchText, int pageNumber, int pageSize, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Site>>(_sites.Values.ToArray());

        public Task<int> CountAsync(string? searchText, CancellationToken cancellationToken)
            => Task.FromResult(_sites.Count);

        public Task<bool> ExistsByCodeAsync(string code, int? excludingIdSite, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task AddAsync(Site site, CancellationToken cancellationToken)
        {
            _sites[site.IdSite] = site;
            return Task.CompletedTask;
        }

        public void Update(Site site) => _sites[site.IdSite] = site;
        public void Remove(Site site) => _sites.Remove(site.IdSite);
        public void SetOriginalVersion(Site site, byte[] version) { }
    }
}
