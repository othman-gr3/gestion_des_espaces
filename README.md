# 🏢 GestionEspaces — Système de Gestion des Espaces de Travail

> Application full-stack de gestion des espaces de travail, des agents et des actifs.
> Backend : **ASP.NET Core 8** (Clean Architecture) | Frontend : **React + Vite**

---

## 📁 Structure Globale du Projet

```
GestionEspaces/                          ← Racine du dépôt
├── GestionEspaces/                      ← Solution .NET (backend)
│   ├── GestionEspaces.slnx              ← Fichier solution Visual Studio
│   ├── GestionEspaces.Api/              ← Couche API (point d'entrée HTTP)
│   ├── GestionEspaces.Application/      ← Couche Application (logique métier)
│   ├── GestionEspaces.Domain/           ← Couche Domaine (entités & règles)
│   ├── GestionEspaces.Infrastructure/   ← Couche Infrastructure (BDD, repos)
│   ├── GestionEspaces.Tests/            ← Tests unitaires
│   └── GestionEspaces.IntegrationTests/ ← Tests d'intégration
├── GestionEspaces.Web/                  ← Application frontend (React)
├── global.json                          ← Version du SDK .NET ciblée
└── .gitignore                           ← Fichiers ignorés par Git
```

---

## 🔷 Backend — Clean Architecture (.NET 8)

Le backend suit la **Clean Architecture** avec une séparation stricte en 4 couches.

---

### 1️⃣ `GestionEspaces.Domain` — Couche Domaine

> Coeur de l'application. Contient les règles métier pures, sans dépendance externe.

```
GestionEspaces.Domain/
├── GestionEspaces.Domain.csproj
├── Entities/
│   ├── Actif.cs                  ← Entité représentant un actif (équipement, matériel)
│   ├── AffectationActif.cs       ← Entité d'affectation d'un actif à un agent
│   ├── AffectationPoste.cs       ← Entité d'affectation d'un agent à un poste/bureau
│   ├── Agent.cs                  ← Entité Agent (employé ou utilisateur du système)
│   ├── Batiment.cs               ← Entité Bâtiment (appartient à un Site)
│   ├── Bureau.cs                 ← Entité Bureau (espace de travail dans un Bâtiment)
│   └── Site.cs                   ← Entité Site (lieu géographique principal)
├── Exceptions/
│   ├── DomainException.cs              ← Exception de base pour le domaine
│   ├── BusinessRuleViolationException.cs ← Levée quand une règle métier est violée
│   └── ConcurrencyConflictException.cs   ← Levée en cas de conflit de concurrence (DB)
├── ValueObjects/
│   └── AdresseSite.cs            ← Value Object représentant l'adresse d'un Site
├── Repositories/                 ← (vide) Interfaces de repos définies dans Application
└── Services/                     ← (vide) Services domaine futurs
```

---

### 2️⃣ `GestionEspaces.Application` — Couche Application

> Orchestre les cas d'usage. Définit les contrats (interfaces), les DTOs et la logique applicative.

```
GestionEspaces.Application/
├── GestionEspaces.Application.csproj
├── Common/
│   ├── Result.cs                 ← Wrapper générique de résultat (succès / erreur)
│   ├── PagedResult.cs            ← Wrapper de résultat paginé
│   ├── ErrorDetail.cs            ← Objet de détail d'une erreur
│   └── MappingExtensions.cs      ← Extensions de mapping Entités <-> DTOs
├── Interfaces/
│   ├── IUnitOfWork.cs            ← Interface du patron Unit of Work (transaction DB)
│   └── Repositories/             ← Interfaces des repositories par entité
│       ├── IActifRepository.cs
│       ├── IAgentRepository.cs
│       ├── IBatimentRepository.cs
│       ├── IBureauRepository.cs
│       ├── ISiteRepository.cs
│       └── IAffectationRepository.cs
├── DTOs/                         ← Objets de Transfert de Données (Requêtes & Réponses)
│   ├── Actifs/
│   │   ├── ActifDto.cs               ← DTO de réponse pour un Actif
│   │   ├── CreateActifRequest.cs     ← DTO de création d'un Actif
│   │   ├── UpdateActifRequest.cs     ← DTO de mise à jour d'un Actif
│   │   └── SearchActifsRequest.cs    ← DTO de recherche/filtrage d'Actifs
│   ├── Agents/
│   │   ├── AgentDto.cs               ← DTO de réponse pour un Agent
│   │   ├── CreateAgentRequest.cs     ← DTO de création d'un Agent
│   │   ├── UpdateAgentRequest.cs     ← DTO de mise à jour d'un Agent
│   │   └── SearchAgentsRequest.cs    ← DTO de recherche d'Agents
│   ├── Assignments/                  ← DTOs pour les affectations
│   ├── Batiments/                    ← DTOs pour les Bâtiments
│   ├── Bureaux/                      ← DTOs pour les Bureaux
│   └── Sites/                        ← DTOs pour les Sites
├── UseCases/                     ← Cas d'usage (logique applicative par fonctionnalité)
│   ├── ActifUseCases.cs              ← CRUD et recherche des Actifs
│   ├── AgentUseCases.cs              ← CRUD et recherche des Agents
│   ├── BatimentUseCases.cs           ← CRUD et recherche des Bâtiments
│   ├── BureauUseCases.cs             ← CRUD et recherche des Bureaux
│   ├── SiteUseCases.cs               ← CRUD et recherche des Sites
│   ├── CreateAgentUseCase.cs         ← Cas d'usage dédié à la création d'un Agent
│   ├── AssignAgentToOfficeUseCase.cs ← Affecte un Agent à un Bureau
│   ├── AssignAssetToAgentUseCase.cs  ← Affecte un Actif à un Agent
│   ├── CloseAffectationActifUseCase.cs  ← Clôture une affectation d'actif
│   ├── CloseAffectationPosteUseCase.cs  ← Clôture une affectation de poste
│   ├── QueryAffectationsUseCase.cs   ← Interroge/liste les affectations
│   └── AgentSelfServiceUseCase.cs    ← Self-service Agent : son bureau actuel, ses actifs affectés
├── Validation/                   ← Validateurs FluentValidation pour chaque requête
│   ├── CreateAgentRequestValidator.cs
│   ├── UpdateAgentRequestValidator.cs
│   ├── CreateActifRequestValidator.cs
│   ├── UpdateActifRequestValidator.cs
│   ├── CreateBatimentRequestValidator.cs
│   ├── UpdateBatimentRequestValidator.cs
│   ├── CreateBureauRequestValidator.cs
│   ├── UpdateBureauRequestValidator.cs
│   ├── CreateSiteRequestValidator.cs
│   ├── UpdateSiteRequestValidator.cs
│   ├── AssignAgentToOfficeRequestValidator.cs
│   ├── AssignAssetToAgentRequestValidator.cs
│   ├── SearchActifsRequestValidator.cs
│   ├── SearchAgentsRequestValidator.cs
│   ├── SearchBatimentsRequestValidator.cs
│   ├── SearchBureauxRequestValidator.cs
│   └── SearchSitesRequestValidator.cs
└── DependencyInjection/
    └── ServiceCollectionExtensions.cs  ← Enregistrement des services Application dans le conteneur DI
```

---

### 3️⃣ `GestionEspaces.Infrastructure` — Couche Infrastructure

> Implémente les interfaces définies dans Application. Gère la base de données, l'ORM (EF Core) et la sécurité.

```
GestionEspaces.Infrastructure/
├── GestionEspaces.Infrastructure.csproj
├── Persistence/
│   ├── GestionEspacesDbContext.cs          ← DbContext Entity Framework Core (accès BDD)
│   ├── GestionEspacesDbContextFactory.cs   ← Factory pour créer le DbContext (migrations CLI)
│   ├── DbInitializer.cs                    ← Seeding initial des données (données de démo)
│   ├── Configurations/                     ← Configuration EF Core par entité (Fluent API)
│   │   ├── ActifConfiguration.cs
│   │   ├── AffectationActifConfiguration.cs
│   │   ├── AffectationPosteConfiguration.cs
│   │   ├── AgentConfiguration.cs
│   │   ├── BatimentConfiguration.cs
│   │   ├── BureauConfiguration.cs
│   │   └── SiteConfiguration.cs
│   ├── Migrations/                         ← Migrations EF Core (historique du schéma BDD)
│   └── Scripts/                            ← Scripts SQL additionnels
├── Repositories/                           ← Implémentations concrètes des interfaces de repos
│   ├── ActifRepository.cs
│   ├── AgentRepository.cs
│   ├── AffectationRepository.cs
│   ├── BatimentRepository.cs
│   ├── BureauRepository.cs
│   ├── SiteRepository.cs
│   └── UnitOfWork.cs                       ← Implémentation du Unit of Work (commit de transaction)
├── Security/                               ← (réservé) Sécurité, JWT, hashage...
└── DependencyInjection/
    └── ServiceCollectionExtensions.cs      ← Enregistrement des services Infrastructure (DbContext, repos...)
```

---

### 4️⃣ `GestionEspaces.Api` — Couche API (Point d'entrée)

> Expose les endpoints REST HTTP. Reçoit les requêtes, délègue aux cas d'usage, retourne les réponses.

```
GestionEspaces.Api/
├── GestionEspaces.Api.csproj
├── Program.cs                        ← Point d'entrée de l'application, configuration du pipeline HTTP
├── appsettings.json                  ← Configuration générale (connexion BDD, JWT...)
├── appsettings.Development.json      ← Surcharge de configuration pour le développement
├── GestionEspaces.Api.http           ← Fichier de tests HTTP (VS Code REST Client / Rider)
├── Controllers/
│   ├── AuthController.cs             ← Endpoints d'authentification (login, token JWT)
│   ├── AgentsController.cs           ← CRUD Agents, affectations de postes/actifs, self-service /me
│   ├── ActifsController.cs           ← CRUD Actifs (équipements)
│   ├── BatimentsController.cs        ← CRUD Bâtiments
│   ├── BureauxController.cs          ← CRUD Bureaux
│   └── SitesController.cs            ← CRUD Sites
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs ← Intercepte les exceptions non gérées, retourne JSON structuré
├── Common/
│   └── ControllerResultExtensions.cs  ← Extensions pour convertir Result<T> en IActionResult HTTP
└── Properties/
    └── launchSettings.json            ← Profils de lancement (ports, HTTPS, variables d'env)
```

---

### 5️⃣ `GestionEspaces.Tests` — Tests Unitaires

```
GestionEspaces.Tests/
├── GestionEspaces.Tests.csproj
├── Application/
│   └── UseCaseTests.cs           ← Tests unitaires des cas d'usage (avec mocks des repositories)
└── Domain/
    └── AssignmentRulesTests.cs   ← Tests des règles métier d'affectation (logique domaine pure)
```

---

### 6️⃣ `GestionEspaces.IntegrationTests` — Tests d'Intégration

```
GestionEspaces.IntegrationTests/
├── GestionEspaces.IntegrationTests.csproj
├── Actifs/          ← Tests d'intégration pour les endpoints Actifs
├── Affectations/    ← Tests d'intégration pour les affectations
├── Agents/          ← Tests d'intégration pour les endpoints Agents
├── Authorization/   ← Tests d'intégration RBAC (rôles Administrateur/Gestionnaire/Agent)
├── Bureaux/         ← Tests d'intégration pour les endpoints Bureaux
├── Sites/           ← Tests d'intégration pour les endpoints Sites
└── Infrastructure/  ← Helpers partagés (WebApplicationFactory, base de données de test...)
```

---

## 🔶 Frontend — `GestionEspaces.Web` (React + Vite)

> Interface utilisateur moderne construite avec **React 18** et **Vite** comme bundler.

```
GestionEspaces.Web/
├── index.html                    ← Point d'entrée HTML de l'application SPA
├── vite.config.js                ← Configuration Vite (proxy API, plugins...)
├── package.json                  ← Dépendances npm et scripts (dev, build...)
├── .oxlintrc.json                ← Configuration du linter Oxlint
├── public/                       ← Fichiers statiques servis tels quels
└── src/
    ├── main.jsx                  ← Point d'entrée React (monte <App /> dans le DOM)
    ├── App.jsx                   ← Composant racine : définit les routes (React Router)
    ├── App.css                   ← Styles globaux de l'application
    ├── index.css                 ← Reset CSS et variables de design (couleurs, fonts...)
    ├── assets/                   ← Images, icônes et autres ressources statiques
    ├── components/
    │   ├── AppShell.jsx          ← Layout principal : sidebar, navbar, zone de contenu
    │   └── ProtectedRoute.jsx    ← HOC qui redirige vers /login si non authentifié
    ├── context/
    │   └── AuthContext.jsx       ← Context React pour l'état d'authentification (token, user)
    ├── hooks/
    │   └── useAuth.js            ← Hook personnalisé pour accéder au contexte Auth
    ├── services/
    │   └── api.js                ← Instance Axios configurée (baseURL, intercepteurs JWT)
    └── pages/
        ├── Login.jsx             ← Page de connexion (formulaire email/mot de passe)
        ├── Dashboard.jsx         ← Tableau de bord principal (statistiques, vue d'ensemble)
        ├── Agents.jsx            ← Page de gestion des Agents (liste, création, édition, affectation)
        ├── Assets.jsx            ← Page de gestion des Actifs / Équipements
        ├── Sites.jsx             ← Page de gestion des Sites et Bâtiments
        └── Spaces.jsx            ← Page de gestion des Bureaux / Espaces de travail
```

---

## 🏛️ Architecture Globale

```
┌─────────────────────────────────────────────────────────┐
│                   GestionEspaces.Web                     │
│              React + Vite (Port 5173)                    │
│    Login | Dashboard | Agents | Sites | Actifs           │
└──────────────────────┬──────────────────────────────────┘
                       │ HTTP / REST (JSON)
                       ▼
┌─────────────────────────────────────────────────────────┐
│                  GestionEspaces.Api                      │
│            ASP.NET Core 8 (Port 5001)                   │
│   Controllers | Middleware | JWT Auth | Swagger          │
└──────────────────────┬──────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────┐
│              GestionEspaces.Application                  │
│       UseCases | DTOs | Interfaces | Validators          │
└──────────────────────┬──────────────────────────────────┘
                       │
           ┌───────────┴───────────┐
           ▼                       ▼
┌──────────────────┐   ┌──────────────────────────────────┐
│ GestionEspaces   │   │     GestionEspaces.Infrastructure │
│   .Domain        │   │  Repositories | EF Core | DbContext│
│ Entities | Rules │   │  Migrations | Seeding              │
└──────────────────┘   └──────────────────────┬───────────┘
                                              │
                                              ▼
                                    ┌─────────────────┐
                                    │   SQL Server /  │
                                    │   SQLite (BDD)  │
                                    └─────────────────┘
```

---

## ⚙️ Fichiers de Configuration

| Fichier | Description |
|---|---|
| `global.json` | Fixe la version du SDK .NET utilisée par le projet |
| `GestionEspaces.slnx` | Fichier solution VS — référence tous les projets .NET |
| `appsettings.json` | Configuration de l'API (chaîne de connexion BDD, JWT secret, CORS...) |
| `appsettings.Development.json` | Surcharge pour le mode développement (logs verbeux...) |
| `vite.config.js` | Configuration Vite : proxy `/api` vers le backend, port du dev server |
| `package.json` | Dépendances npm du frontend (React, Axios, React Router...) |
| `.gitignore` | Exclut `node_modules`, `bin`, `obj`, secrets... |
| `.oxlintrc.json` | Règles du linter JavaScript/JSX (Oxlint) |

---

## 🧩 Flux de Données — Exemple : Créer un Agent

```
1. [Frontend] Agents.jsx          → POST /api/agents  (JSON: CreateAgentRequest)
2. [API]      AgentsController    → valide le token JWT
3. [API]      AgentsController    → appelle CreateAgentUseCase
4. [Application] CreateAgentUseCase
                 → valide via CreateAgentRequestValidator (FluentValidation)
                 → appelle IAgentRepository.AddAsync()
                 → appelle IUnitOfWork.CommitAsync()
5. [Infrastructure] AgentRepository → INSERT en base via EF Core
6. [Infrastructure] UnitOfWork       → SaveChangesAsync()
7. [API]      retourne 201 Created + AgentDto (JSON)
8. [Frontend] Agents.jsx          → met à jour l'état React, affiche l'agent créé
```

---

*Généré automatiquement — GestionEspaces © 2026*
