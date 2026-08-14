# Reference Manual: GestionEspaces Codebase

Welcome to the comprehensive reference manual and documentation guide for the **GestionEspaces** project. This document describes the architecture, model structures, validation mechanisms, security rules, concurrency handling, and testing strategies built into the solution.

---

## 1. Quick Start & Developer Commands

All paths are relative to the repository solution root: `c:\Users\lamda\Downloads\GestionEspaces\GestionEspaces`.

```powershell
# 1. Restore and Build the Entire Solution
dotnet build GestionEspaces.slnx

# 2. Run Unit Tests (Execute in memory, does not require Docker or Database)
dotnet test GestionEspaces.Tests\GestionEspaces.Tests.csproj

# 3. Run Integration Tests (Requires Docker Desktop to run SQL Server Testcontainers)
dotnet test GestionEspaces.IntegrationTests\GestionEspaces.IntegrationTests.csproj

# 4. Start the API Local Development Server (Listening on ports defined in launchSettings.json)
dotnet run --project GestionEspaces.Api\GestionEspaces.Api.csproj

# 5. Database Schema Migrations (Run from solution root to update LocalDB)
dotnet ef database update `
  --project GestionEspaces.Infrastructure\GestionEspaces.Infrastructure.csproj `
  --startup-project GestionEspaces.Api\GestionEspaces.Api.csproj
```

---

## 2. Architecture & Layering

The codebase follows the principles of **Clean Architecture** (ports & adapters) to isolate core business rules from external framework dependencies:

```
                  ┌──────────────────────────────┐
                  │     GestionEspaces.Domain    │ (Entities, Value Objects, Domain Exceptions)
                  └──────────────▲───────────────┘
                                 │
                  ┌──────────────┴───────────────┐
                  │  GestionEspaces.Application  │ (Use Cases, DTOs, Repository interfaces, Validators)
                  └──────────────▲───────────────┘
                                 │
         ┌───────────────────────┴───────────────────────┐
         │                                               │
┌────────┴─────────────────────┐                ┌────────┴─────────────────────┐
│ GestionEspaces.Infrastructure│                │      GestionEspaces.Api      │
│ (EF Core, Repositories,      │                │ (Controllers, Middlewares,   │
│  Migrations, UnitOfWork)     │                │  JWT Auth configuration)     │
└──────────────────────────────┘                └──────────────────────────────┘
```

### Layer Directories & Responsibility Matrix

1. **GestionEspaces.Domain**: 
   * Holds the heart of the business model. 
   * Contains rich domain models with private setters to prevent direct state mutations outside encapsulation.
   * Relies on no external packages or ORM concepts.
2. **GestionEspaces.Application**:
   * Orchestrates domain entities to execute user stories (Use Cases).
   * Declares interfaces for repositories (`ISiteRepository`, `IBureauRepository`, etc.) and unit of work (`IUnitOfWork`).
   * Contains data transfer records (DTOs) and input sanitization/validation rules using FluentValidation.
   * Communicates execution outcomes using a functional `Result` / `Result<T>` monad rather than raising controlled exceptions.
3. **GestionEspaces.Infrastructure**:
   * Implements interfaces defined in the Application layer.
   * Configures EF Core database contexts, schema configurations, navigation mappings, indexes, and migrations.
   * Implements data access repositories targeting Microsoft SQL Server.
   * Manages concurrency exception translations inside the unit of work implementation.
4. **GestionEspaces.Api**:
   * Entry point of the web service.
   * Translates incoming HTTP requests to Application requests, executes the use cases, and maps the output back to REST HTTP status codes.
   * Provides authentication/authorization middlewares, routing, Swagger documentation, and centralized error handling (RFC 7807).

---

## 3. Database Schema & Entities

The relational database schema mapped via EF Core contains 7 entities. Every entity exposes a `Version` rowversion token used for optimistic concurrency control (except junction tables where concurrency is guarded on parent updates).

```
 ┌──────────────┐          ┌─────────────────┐          ┌───────────────┐
 │     Site     │1        *│    Batiment     │1        *│    Bureau     │
 ├──────────────┤─────────>├─────────────────┤─────────>├───────────────┤
 │ IdSite (PK)  │          │ IdBatiment (PK) │          │IdBureau (PK)  │
 │ Nom          │          │ Nom             │          │Numero         │
 │ Code (UQ)    │          │ NombreEtages    │          │Capacite       │
 │ Adresse(Owned│          │ Superficie      │          │Superficie     │
 │ Version      │          │ IdSite (FK)     │          │Statut         │
 └──────────────┘          │ Version         │          │IdBatiment (FK)│
                           └─────────────────┘          │Version        │
                                                        └───────┬───────┘
                                                                │1
                                                                │
 ┌──────────────┐          ┌─────────────────┐                  │*
 │    Agent     │1        *│AffectationPoste │                  │
 ├──────────────┤─────────>├─────────────────┤<─────────────────┘
 │ IdAgent (PK) │          │IdAffectation(PK)│
 │ Nom          │          │IdAgent (FK)     │
 │ Prenom       │          │IdBureau (FK)    │
 │ Matricule(UQ)│          │DateAffectation  │
 │ Version      │          │DateFin          │
 └──────┬───────┘          └─────────────────┘
        │1
        │
        │*                 ┌─────────────────┐          ┌───────────────┐
        └─────────────────>│AffectationActif │*        1│     Actif     │
                           ├─────────────────┤<─────────├───────────────┤
                           │IdAffectation(PK)│          │IdActif (PK)   │
                           │IdAgent (FK)     │          │Nom            │
                           │IdActif (FK)     │          │NumeroSerie(UQ)│
                           │DateAffectation  │          │Etat           │
                           │DateFin          │          │Version        │
                           └─────────────────┘          └───────────────┘
```

### Column Metadata & Constraints

* **`Site`**
  * `IdSite` (int, Primary Key, Identity)
  * `Version` (byte[], mapped to `rowversion` / `IsRowVersion()`)
  * `Nom` (nvarchar(200), Required)
  * `Code` (nvarchar(50), Required, Unique Index)
  * `Adresse` (Owned entity mapping):
    * `AdresseRue` (nvarchar(250), Required)
    * `AdresseVille` (nvarchar(150), Required)
    * `AdresseCodePostal` (nvarchar(20), Required)
    * `AdressePays` (nvarchar(100), Required)
  * `Image` (nvarchar(500), Nullable)
  * *Relationship*: Has many `Batiment` instances (Restrict delete behavior).
* **`Batiment`**
  * `IdBatiment` (int, Primary Key, Identity)
  * `Version` (byte[], mapped to `rowversion`)
  * `Nom` (nvarchar(150), Required)
  * `NombreEtages` (int, Required, Non-negative)
  * `Superficie` (real, Required)
  * `Image` (nvarchar(500), Nullable)
  * `IdSite` (int, Foreign Key)
  * *Relationship*: Has many `Bureau` instances (Restrict delete behavior).
* **`Bureau`**
  * `IdBureau` (int, Primary Key, Identity)
  * `Version` (byte[], mapped to `rowversion`)
  * `Numero` (nvarchar(50), Required)
  * `Type` (nvarchar(100), Nullable)
  * `Capacite` (int, Required, positive)
  * `Superficie` (real, ColumnType: `"real"`, Required)
  * `Etage` (int, Required)
  * `Statut` (StatutBureau Enum: `Disponible` = 0, `EnMaintenance` = 1, `HorsService` = 2)
  * `Image` (nvarchar(500), Nullable)
  * `IdBatiment` (int, Foreign Key)
  * *Relationship*: Has unique composite index on `{ IdBatiment, Numero }`. Has many `AffectationPoste` records (NoAction delete behavior).
* **`Agent`**
  * `IdAgent` (int, Primary Key, Identity)
  * `Version` (byte[], mapped to `rowversion`)
  * `Nom` (nvarchar(150), Required)
  * `Prenom` (nvarchar(150), Required)
  * `Matricule` (nvarchar(50), Required, Unique Index)
  * `Email` (nvarchar(250), Nullable)
  * `Telephone` (nvarchar(50), Nullable)
  * `Fonction` (nvarchar(150), Nullable)
  * `Departement` (nvarchar(150), Nullable)
  * `DateEmbauche` (datetime2, Nullable)
  * `Image` (nvarchar(500), Nullable)
  * *Relationship*: Has many `AffectationPoste` and `AffectationActif` entities.
* **`Actif`** (Assets assigned to agents)
  * `IdActif` (int, Primary Key, Identity)
  * `Version` (byte[], mapped to `rowversion`)
  * `Nom` (nvarchar(150), Required)
  * `Type` (nvarchar(100), Nullable)
  * `Marque` (nvarchar(100), Nullable)
  * `Modele` (nvarchar(100), Nullable)
  * `NumeroSerie` (nvarchar(100), Nullable, Unique Index)
  * `DateAchat` (datetime2, Nullable)
  * `Etat` (EtatActif Enum: `Neuf` = 0, `Bon` = 1, `ARepairer` = 2, `HorsService` = 3)
  * `Image` (nvarchar(500), Nullable)
* **`AffectationPoste`** (Agent to Office assignment)
  * `IdAffectationPoste` (int, Primary Key, Identity)
  * `IdAgent` (int, Foreign Key)
  * `IdBureau` (int, Foreign Key)
  * `DateAffectation` (datetime2, UTC format required)
  * `DateFin` (datetime2, Nullable, UTC format required)
* **`AffectationActif`** (Agent to Asset assignment)
  * `IdAffectationActif` (int, Primary Key, Identity)
  * `IdAgent` (int, Foreign Key)
  * `IdActif` (int, Foreign Key)
  * `DateAffectation` (datetime2, UTC format required)
  * `DateFin` (datetime2, Nullable, UTC format required)

---

## 4. Business Logic & Domain Invariants

Domain entities enforce valid data mutations through internal invariant validations:

### Space Planning Rules
* **Duplicate Buildings**: A building cannot be added to a Site if another building on the same site already shares the exact same name (case-insensitive check: `existingBatiment.Nom.Equals(batiment.Nom, StringComparison.OrdinalIgnoreCase)`).
* **Duplicate Offices**: Office numbers must be unique within a given building. Checked via the composite index constraint on `IdBatiment` + `Numero`.
* **Office Maintenance**: Offices can transition through statuses using `MettreEnMaintenance()`, `RemettreEnService()`, and `MettreHorsService()`. Only offices in `StatutBureau.Disponible` are available for assignment (`EstDisponible()`).

### Assignment Invariants
* **Agent Office Constraint**: An agent can have at most **one active office assignment** at any given time. Attempting to assign an office to an agent with an active slot throws a `BusinessRuleViolationException` ("L'agent possède déjà une affectation de poste active.").
* **Office Occupancy Constraint**: A single office can support at most **one active assignment** (capacity constraint managed per assignment). If an office has a currently active assignment, it throws a `BusinessRuleViolationException` ("Le bureau B-XXX possède déjà une affectation de poste active.").
* **Asset Availability**: Assets (Actifs) must be in a functional state to be assigned (cannot be assigned if they are marked `EtatActif.HorsService`). Throws a violation exception if the state is not available.
* **Asset Assignment Overlap**: An asset can have at most **one active assignment** at a time. It cannot be assigned to another agent unless the previous active allocation has been closed.
* **Closure Rules**: Closing an office or asset assignment requires specifying a `DateFin` which **cannot be earlier** than the original `DateAffectation`. It throws a `BusinessRuleViolationException` if this rule or closing an already-closed assignment is violated.

---

## 5. Use Cases & Application Flow

Use cases handle business workflows cleanly without leaking EF Core abstractions.

```
Request DTO ──> Validator (FluentValidation) ──> Fetch from Repository ──> Apply Domain Logic ──> Save ──> Map to DTO ──> Result
```

### Use Case Directory & Input Definitions

1. **`SiteUseCases`**
   * `CreateAsync(CreateSiteRequest)`: Validates that `Code` is unique. Creates the `Site` aggregate with its owned `AdresseSite`.
   * `UpdateAsync(idSite, UpdateSiteRequest)`: Loads the site, decodes the concurrency token, applies update properties, sets the original version, and updates.
   * `DeleteAsync(idSite)`: Deletes the site if found, matching its concurrency version before dropping.
   * `SearchAsync(SearchSitesRequest)`: Performs text searches across `Nom`, `Code`, and `Adresse.Ville` with page pagination constraints.
2. **`BatimentUseCases`**
   * `CreateAsync(CreateBatimentRequest)`: Ensures parent site exists and the building name is unique for that site.
   * `UpdateAsync(id, UpdateBatimentRequest)`: Updates building metrics with concurrency check.
   * `DeleteAsync(id)`: Removes the building under optimistic lock rules.
3. **`BureauUseCases`**
   * `CreateAsync(CreateBureauRequest)`: Ensures parent building exists and the office number is unique inside it.
   * `UpdateAsync(id, UpdateBureauRequest)`: Updates office state and status under concurrency checks.
   * `DeleteAsync(id)`: Removes the office.
4. **`CreateAgentUseCase`**
   * `ExecuteAsync(CreateAgentRequest)`: Sanitizes input fields and ensures the `Matricule` is unique across all agents.
5. **`AssignAgentToOfficeUseCase`**
   * `ExecuteAsync(AssignAgentToOfficeRequest)`: Validates references, runs the domain assignment rules, updates state, and saves.
6. **`AssignAssetToAgentUseCase`**
   * `ExecuteAsync(AssignAssetToAgentRequest)`: Validates references, checks if asset state is available, runs assignment invariants, and saves.
7. **`AgentSelfServiceUseCase`**
   * `GetMyOfficeAsync(email)` / `GetMyAssetsAsync(email)`: Resolves the calling Agent by the email carried in their JWT identity claim, then returns only their own active office assignment and assigned assets.

### Validator Constraints (FluentValidation)

All incoming requests are validated:
* `Nom`, `Code`, `Matricule`, `Numero` must not be empty.
* Maximum string lengths are verified before database insertions (`Nom` ≤ 200, `Code` ≤ 50, etc.).
* Foreign key IDs (`IdSite`, `IdBatiment`, `IdBureau`) must be greater than zero.
* `Superficie` and `Capacite` metrics are strictly validated (e.g. `Capacite > 0`).
* Stale or invalid base-64 concurrency strings are checked before execution.

---

## 6. API Layer & Custom Middlewares

The Web API project translates operational outcomes into RFC-compliant responses.

### Operational Outcome Translation (`ControllerResultExtensions`)

Rather than exposing database exceptions or forcing endpoints to catch errors, actions return `ToActionResult` or `ToActionResult<T>`. This translates the Application layer's functional `Result` into appropriate HTTP statuses:

```csharp
// Returns:
// - 200 OK with data if success
// - 400 Bad Request if validation errors are found
// - 404 Not Found if resourceNotFound codes are matched
// - 409 Conflict if duplicate or business violations occur
return this.ToActionResult(result, Ok);
```

### Centralized Exception Handler (`ExceptionHandlingMiddleware`)

Catches unhandled errors globally and converts them into standardized **RFC 7807 Problem Details** format:

* **`ConcurrencyConflictException`** (Optimistic concurrency error) → **409 Conflict**
* **`BusinessRuleViolationException`** (Domain rule violation) → **409 Conflict**
* **`DomainException`** (Generic domain errors) → **400 Bad Request**
* **`Exception`** (Unhandled bug or DB connection loss) → **500 Internal Server Error** with details hidden in production environments.

---

## 7. Role-Based Access Control (RBAC)

The REST API implements JSON Web Token (JWT) Bearer authentication to secure access. There are exactly three roles, matching the three actors of the validated spec:

* **Administrateur**: manages the full referentiel (Sites, Batiments, Bureaux, Agents, Actifs) and views the global dashboard.
* **Gestionnaire**: handles day-to-day assignments — creates and closes `AffectationPoste` and `AffectationActif` records. No rights on the referentiel itself.
* **Agent**: read-only access to their own data (current office, assigned assets), resolved from the JWT identity claim — never from an id in the URL.

### Role Privileges

| Endpoints | Required Policy | Permitted Roles | Description |
|---|---|---|---|
| Sites, Batiments, Bureaux, Agents, Actifs — full CRUD | `ReferentielAdmin` | `Administrateur` | Full read/write access to the referentiel |
| `POST`/`DELETE` on `AffectationPoste` and `AffectationActif` (via `AgentsController`) | `GestionAffectations` | `Administrateur`, `Gestionnaire` | Create and close office/asset assignments |
| `GET /api/agents/me/office`, `GET /api/agents/me/assets` | `LectureAgent` | `Agent` | Self-service — returns only the caller's own office/assets |

### Test Accounts (seeded in `appsettings.json` → `Users`, matched by email)

| Email | Role | Password | Notes |
|---|---|---|---|
| `admin@onee.ma` | `Administrateur` | `Admin123!` | Full referentiel access |
| `gestionnaire@onee.ma` | `Gestionnaire` | `Gestion123!` | Assignment management only |
| `y.elamrani@onee.ma` | `Agent` | `Agent123!` | Linked to the seeded `Agent` record "Youssef El Amrani" (`DbInitializer.cs`), which has an active office and asset assignments for self-service testing |

### Known Limitation: No Agent-Facing Frontend Yet

The `/api/agents/me/office` and `/api/agents/me/assets` endpoints are fully implemented and verified (both automated tests and manual API calls), but `GestionEspaces.Web` has no page consuming them yet. Logging in as `Agent` currently lands on the Admin `Dashboard`, which calls `/api/sites`, `/api/batiments`, `/api/bureaux` — all `Administrateur`-only — resulting in `403` responses. The frontend's global Axios response interceptor (`services/api.js`) treats any `403` as an expired session and force-logs-out, so an Agent login currently bounces straight back to `/login`. Building an Agent-facing "my office / my assets" page and adjusting that interceptor is a follow-up task, not yet done.

### JWT Verification Pipeline

Token verification requires:
1. **Issuer Signing Key**: The token must be signed with a matching `SymmetricSecurityKey` configured in `appsettings.json`.
2. **Issuer & Audience**: Must match defined values (`Issuer`: `GestionEspaces`, `Audience`: `GestionEspacesApi`).
3. **Expiration check**: Rejects expired tokens (includes a default clock skew of 1 minute).

### OpenAPI / Swagger Security Integration

The Swagger documentation UI `/swagger` is configured with a Bearer authentication scheme definition. Developers can test endpoints directly by clicking "Authorize" and pasting their JWT bearer credentials (`Bearer <JWT_TOKEN>`).

---

## 8. Optimistic Concurrency & Conflict Handling

To prevent data loss from concurrent updates (last-write-wins issues), the API implements a strict optimistic locking pipeline:

```
[Client]                [API/Use Case]              [EF / Database]
   │                          │                            │
   │─── GET /api/sites/1 ────>│                            │
   │                          │─── Fetch site ────────────>│
   │<── returns SiteDto ──────│<── Return version bytes ───│ (e.g. Version = 0x01)
   │    (concurrencyToken =   │                            │
   │     "base64String")      │                            │
   │                          │                            │
   │─── PUT /api/sites/1 ────>│                            │
   │    (UpdateSiteRequest with│                           │
   │     concurrencyToken)    │                            │
   │                          │─── Decode base64 bytes ────│
   │                          │─── Set OriginalValue ─────>│ (Tells EF: expect 0x01)
   │                          │─── SaveChangesAsync() ────>│ (Checks Version == 0x01)
   │                          │                            │
   │                          │<── Version mismatch ───────│ (throws DbUpdateConcurrencyException)
   │                          │    (Throws Concurrency-    │
   │                          │     ConflictException)     │
   │<── HTTP 409 Conflict ────│                            │
```

1. **Token Mapping**: When an entity is mapped to a DTO, the `Version` binary is converted to base-64 using `Convert.ToBase64String()` and sent to the client as `ConcurrencyToken`.
2. **Request Submission**: Update requests must include the `ConcurrencyToken` received during read time.
3. **Original Value Injection**: The use case decodes the token and passes it to the repository's `SetOriginalVersion` method.
4. **EF Core Original Value Setup**: The repository injects the bytes into EF Core's change tracker:
   ```csharp
   _dbContext.Entry(entity).Property(e => e.Version).OriginalValue = version;
   ```
5. **Database Check**: During `SaveChangesAsync()`, EF Core checks if the database's current version matches the tracked `OriginalValue`. If another process has updated the database in the meantime, the version check fails, updating 0 rows.
6. **Exception Translation**: The `UnitOfWork` catches EF's `DbUpdateConcurrencyException`, extracts the conflicted resource's name and primary key, and throws a domain-level `ConcurrencyConflictException`.
7. **HTTP Status Code Mapping**: The API middleware catches the exception and returns a **409 Conflict** response, notifying the user to reload the resource.

---

## 9. Testing Strategies

The testing suite contains two projects designed to isolate and test different parts of the application:

### Unit Tests (`GestionEspaces.Tests`)

Tests only the business rules in memory without external infrastructure dependencies:
* **Fakes**: Implements light, in-memory repository fakes (`InMemoryAgentRepository`, `InMemoryBureauRepository`, etc.) rather than using mocking frameworks.
* **Coverage**: Tests agent creations, validation checks, duplicate matricule rejections, agent-to-office allocations, asset assignments, and date validations.
* **Speed**: Runs instantly in milliseconds.

### Integration Tests (`GestionEspaces.IntegrationTests`)

Tests the entire HTTP stack and SQL database integration using actual containers:
* **SQL Server Container**: Uses `Testcontainers.MsSql` to launch a real SQL Server instance.
* **Schema Migration**: Automatically runs EF Core migrations before running the test classes to build a fresh, realistic schema.
* **App Sandbox**: Uses `WebApplicationFactory<Program>` to build the API pipeline in memory, overriding the DB connection string to point to the test container.
* **Authorization Mocking**: `AuthHelper` mints realistic tokens for tests, signing them with the validation key configured in the test factory to verify authorization rules.

#### Integration Test Scenarios

1. **Security Checks**: Ensures calling protected endpoints without a token returns `401 Unauthorized`, and using a token with the wrong role returns `403 Forbidden`. `Authorization/RoleBasedAccessTests.cs` covers the 3-role RBAC model specifically: `Agent` gets `403` on referentiel CRUD, `Gestionnaire` gets `403` on Administrateur-only CRUD, and `Agent` self-service endpoints return `200 OK` scoped strictly to the caller's own data.
2. **CRUD Flow Validation**: Tests creating a resource, querying it to verify persistence, updating properties, and deleting it, checking for correct HTTP status codes throughout.
3. **Concurrency Conflict Verification**: Creates a record, triggers an update to increment the version, and then attempts a second update with the initial stale token to verify that the system returns the expected `409 Conflict`.
4. **Search and Pagination**: Verifies query filters and pagination page counts work as expected.
