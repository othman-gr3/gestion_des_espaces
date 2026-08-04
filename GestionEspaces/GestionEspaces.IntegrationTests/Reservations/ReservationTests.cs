using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GestionEspaces.IntegrationTests.Infrastructure;

namespace GestionEspaces.IntegrationTests.Reservations;

/// <summary>
/// Integration tests for the Reservations workflow endpoints.
/// </summary>
public sealed class ReservationTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ReservationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task<int> CreateSiteAsync(HttpClient client, string code)
    {
        var resp = await client.PostAsJsonAsync("/api/sites", new
        {
            nom = $"Site {code}",
            code,
            rue = "1 rue Test",
            ville = "Paris",
            codePostal = "75002",
            pays = "France",
            image = (string?)null
        });
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return json.GetProperty("idSite").GetInt32();
    }

    private async Task<int> CreateBatimentAsync(HttpClient client, int idSite, string nom)
    {
        var resp = await client.PostAsJsonAsync("/api/batiments", new
        {
            nom,
            nombreEtages = 2,
            superficie = 300f,
            image = (string?)null,
            idSite
        });
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return json.GetProperty("idBatiment").GetInt32();
    }

    private async Task<int> CreateBureauAsync(HttpClient client, int idBatiment, string numero)
    {
        var resp = await client.PostAsJsonAsync("/api/bureaux", new
        {
            numero,
            type = "Salle de réunion",
            capacite = 10,
            superficie = 25f,
            etage = 1,
            image = (string?)null,
            idBatiment,
            statut = 0
        });
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return json.GetProperty("idBureau").GetInt32();
    }

    private async Task<int> CreateAgentAsync(HttpClient client, string matricule)
    {
        var resp = await client.PostAsJsonAsync("/api/agents", new
        {
            nom = "ResAgent",
            prenom = "Test",
            matricule,
            email = (string?)null,
            telephone = (string?)null,
            fonction = (string?)null,
            departement = (string?)null,
            dateEmbauche = (DateTime?)null,
            image = (string?)null
        });
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return json.GetProperty("idAgent").GetInt32();
    }

    // ── Happy Path workflow ────────────────────────────────────────────────────

    [Fact]
    public async Task Reservation_Workflow_HappyPath()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Gestionnaire));

        var idSite = await CreateSiteAsync(client, "RES01");
        var idBatiment = await CreateBatimentAsync(client, idSite, "Bâtiment RES");
        var idBureau = await CreateBureauAsync(client, idBatiment, "R-101");
        var idAgent = await CreateAgentAsync(client, "RES-AGT-01");

        // 1. Create Reservation (EnAttente)
        var dateDebut = DateTime.UtcNow.AddDays(1);
        var dateFin = dateDebut.AddHours(2);

        var createResponse = await client.PostAsJsonAsync($"/api/reservations/agents/{idAgent}", new
        {
            bureauId = idBureau,
            dateDebut,
            dateFin,
            motif = "Réunion d'équipe"
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var reservation = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var idRes = reservation.GetProperty("idReservation").GetInt32();
        var token = reservation.GetProperty("concurrencyToken").GetString();
        Assert.True(idRes > 0);
        Assert.NotNull(token);
        Assert.Equal("EnAttente", reservation.GetProperty("statut").GetString());

        // 2. Update Reservation (while EnAttente)
        var updatedDateFin = dateDebut.AddHours(3);
        var updateResponse = await client.PutAsJsonAsync($"/api/reservations/{idRes}", new
        {
            concurrencyToken = token,
            dateDebut,
            dateFin = updatedDateFin,
            motif = "Réunion d'équipe rallongée"
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedRes = await updateResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var tokenV2 = updatedRes.GetProperty("concurrencyToken").GetString();
        Assert.NotNull(tokenV2);
        Assert.NotEqual(token, tokenV2);

        // 3. Confirm Reservation
        var confirmResponse = await client.PostAsJsonAsync($"/api/reservations/{idRes}/confirmer", new
        {
            concurrencyToken = tokenV2
        });
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        var confirmedRes = await confirmResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var tokenV3 = confirmedRes.GetProperty("concurrencyToken").GetString();
        Assert.Equal("Confirmee", confirmedRes.GetProperty("statut").GetString());

        // 4. Cancel Reservation (can be done by Lecture policy users, i.e., readers can cancel their own, but since role-based we check endpoint auth)
        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Lecteur));

        var cancelResponse = await client.PostAsJsonAsync($"/api/reservations/{idRes}/annuler", new
        {
            concurrencyToken = tokenV3
        });
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
        var cancelledRes = await cancelResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("Annulee", cancelledRes.GetProperty("statut").GetString());
    }

    // ── Conflict: Overlap ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateReservation_OverlappingExisting_Returns409()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", AuthHelper.BearerFor(AuthHelper.Roles.Gestionnaire));

        var idSite = await CreateSiteAsync(client, "RES02");
        var idBatiment = await CreateBatimentAsync(client, idSite, "Bâtiment RES 2");
        var idBureau = await CreateBureauAsync(client, idBatiment, "R-102");
        var idAgent1 = await CreateAgentAsync(client, "RES-AGT-02");
        var idAgent2 = await CreateAgentAsync(client, "RES-AGT-03");

        var dateDebut = DateTime.UtcNow.AddDays(2);
        var dateFin = dateDebut.AddHours(2);

        // First booking — EnAttente
        var first = await client.PostAsJsonAsync($"/api/reservations/agents/{idAgent1}", new
        {
            bureauId = idBureau,
            dateDebut,
            dateFin,
            motif = "Conférence"
        });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        // Overlapping booking on same room during the window — should fail with 409
        var overlapStart = dateDebut.AddHours(1);
        var overlapEnd = overlapStart.AddHours(2);

        var second = await client.PostAsJsonAsync($"/api/reservations/agents/{idAgent2}", new
        {
            bureauId = idBureau,
            dateDebut = overlapStart,
            dateFin = overlapEnd,
            motif = "Autre réunion"
        });

        // 409 Conflict because overlap logic in Repository detects overlapping active bookings
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }
}
