# 🏢 GestionEspaces — Système de Gestion des Espaces de Travail

> Portail back-office pour la gestion des sites, bâtiments, bureaux, agents et actifs de l'**ONEE** (Office National de l'Électricité et de l'Eau Potable), avec affectation des agents aux bureaux et du matériel aux agents.
>
> **Backend** : ASP.NET Core 10 (Clean Architecture) · **Frontend** : React 19 + Vite + Tailwind CSS v4 · **Base de données** : SQL Server

> **Branche `school-ai`** : cette branche contient tout ce qui est sur `main` (version remise en stage) **plus** une fonctionnalité de recherche de bureau assistée par IA (section [Intelligence artificielle](#intelligence-artificielle)), ajoutée pour le projet de fin d'études. `main` reste la version stage, sans dépendance à un service IA externe.

---

## 1. Présentation du projet

GestionEspaces répond à un cahier des charges centré sur **7 entités** : `Site`, `Batiment`, `Bureau`, `Agent`, `Actif`, `AffectationPoste` (agent ↔ bureau) et `AffectationActif` (agent ↔ matériel). L'application couvre le cycle complet : consultation et administration du référentiel, recherche de bureaux disponibles, création et clôture d'affectations, historique, et consultation en libre-service par l'agent de ses propres données.

Trois rôles se partagent l'application :

| Rôle | Ce qu'il peut faire |
|---|---|
| **Administrateur** | Gère l'intégralité du référentiel (Sites, Bâtiments, Bureaux, Agents, Actifs) et consulte le tableau de bord global. |
| **Gestionnaire** | Recherche dans le référentiel (lecture seule) pour créer et clôturer des affectations de poste et d'actif au quotidien. Aucun droit d'écriture sur le référentiel. |
| **Agent** | Consulte uniquement ses propres données : son bureau actuel et les actifs qui lui sont confiés. |

---

## 2. Stack technique

**Backend**
- ASP.NET Core 10, architecture en couches (Clean Architecture / ports & adapters)
- Entity Framework Core 10 + SQL Server (concurrence optimiste via `rowversion`)
- FluentValidation, authentification JWT Bearer, RBAC par policies
- xUnit (tests unitaires en mémoire) + Testcontainers (tests d'intégration sur un vrai SQL Server dans Docker)

**Frontend**
- React 19, React Router 7, Vite 8
- Tailwind CSS v4 (design back-office : tableaux denses, tri, pagination, tiroirs latéraux pour les formulaires)
- Axios pour les appels API

---

## 3. Architecture

### Couches backend

```
GestionEspaces.Domain          → entités, value objects, règles métier pures
        ↑
GestionEspaces.Application     → cas d'usage, DTOs, interfaces de repository, validation
        ↑
GestionEspaces.Infrastructure  → EF Core, repositories, migrations
GestionEspaces.Api             → contrôleurs REST, JWT, policies, Swagger
```

Détail complet des couches, du schéma de données et des invariants métier : voir [`GestionEspaces/README.md`](GestionEspaces/README.md) (référence technique).

### Modèle de rôles (RBAC)

| Policy | Rôles autorisés | Portée |
|---|---|---|
| `ReferentielAdmin` | Administrateur | Création/modification/suppression sur Sites, Bâtiments, Bureaux, Agents, Actifs |
| `ReferentielLecture` | Administrateur, Gestionnaire | Lecture seule sur le référentiel (nécessaire au Gestionnaire pour sélectionner un agent/bureau/actif lors d'une affectation) |
| `GestionAffectations` | Administrateur, Gestionnaire | Création et clôture des `AffectationPoste` / `AffectationActif` |
| `LectureAgent` | Agent | `GET /api/agents/me/office` et `GET /api/agents/me/assets` — l'agent est identifié via son claim JWT, jamais via un id dans l'URL |

### Durcissement sécurité

Au-delà du RBAC, plusieurs mécanismes protègent l'application :

| Mécanisme | Détail |
|---|---|
| **Jetons JWT courts + rafraîchissement** | Le jeton d'accès expire en 30 minutes (`Jwt:AccessTokenMinutes`). Un jeton de rafraîchissement (7 jours, `Jwt:RefreshTokenDays`) permet d'en obtenir un nouveau via `POST /api/auth/refresh` sans redemander le mot de passe. Il n'est **jamais stocké en clair** côté serveur — seul son hash SHA-256 l'est — et il **tourne à chaque utilisation** : l'ancien est révoqué au moment même où le nouveau est émis, donc un jeton volé ne peut être rejoué qu'une seule fois. `POST /api/auth/logout` le révoque explicitement. |
| **Limitation de débit sur la connexion** | `POST /api/auth/login` et `/api/auth/refresh` sont plafonnés à 10 tentatives par minute et par adresse IP (`429 Too Many Requests` au-delà), pour ralentir une attaque par force brute sur les mots de passe. |
| **Journal d'audit persisté** | Les actions métier significatives (affectation/clôture de poste ou d'actif, mise en maintenance/remise en service d'un bureau) déclenchent un *domain event* côté `Domain`, capturé par `GestionEspacesDbContext.SaveChangesAsync` et écrit dans la table `AuditLog` **dans la même transaction** que le changement métier — avec qui (email + rôle extraits du JWT), quoi et quand. Consultable via la page *Journal d'audit* (Administrateur uniquement). |
| **Hachage et dépendances à jour** | Mots de passe en PBKDF2-SHA256 (10 000 itérations, méthode statique `Rfc2898DeriveBytes.Pbkdf2`, non obsolète) ; dépendance `SSH.NET` épinglée à une version corrigeant une vulnérabilité connue (`GHSA-q939-rpr3-3284`) remontée par une dépendance transitive de Testcontainers. |

### Intelligence artificielle

*Spécifique à la branche `school-ai`.* La page **Recherche IA** (Administrateur, Gestionnaire) permet de décrire en français libre le bureau recherché — *"un bureau individuel disponible avec au moins 3 places au Siège ONEE"* — au lieu de manipuler des filtres. La requête est envoyée à un LLM via [OpenRouter](https://openrouter.ai/) (`POST /api/bureaux/ai-search`), qui la traduit en critères structurés (bâtiment, statut, type, capacité/étage minimum) à partir de la liste réelle des bâtiments et sites ; ces critères sont ensuite exécutés contre le référentiel `Bureau` existant.

**Dégradation gracieuse** — pensée dès la conception, pas ajoutée après coup : si la clé API n'est pas configurée, si OpenRouter est injoignable, ou si le modèle répond quelque chose d'inexploitable, la recherche **n'échoue pas** — elle bascule automatiquement sur une recherche par mot-clé classique sur le numéro de bureau, et l'interface l'indique clairement (badge *"IA activée"* vs *"Repli mot-clé"*). C'est ce chemin de repli qui est exercé par les tests automatisés (voir ci-dessous), puisqu'aucune clé réelle n'est configurée dans l'environnement de test.

**Configuration** — pour activer réellement l'appel au LLM (sinon la fonctionnalité tourne en mode repli, sans erreur) :

```bash
cd GestionEspaces
dotnet user-secrets set "OpenRouter:ApiKey" "sk-or-..." --project GestionEspaces.Api
```

Le modèle utilisé (`openai/gpt-4o-mini` par défaut) se change dans `appsettings.json` → `OpenRouter:Model`, avec n'importe quel identifiant de modèle disponible sur OpenRouter.

**Architecture** — `IOfficeSearchAssistant` (interface, `Application`) / `OpenRouterOfficeSearchAssistant` (implémentation, `Infrastructure`, injectée via `IHttpClientFactory`) / `OfficeSearchAiUseCase` (orchestration + repli, `Application`). Aucune donnée n'est persistée par cette fonctionnalité — pas de nouvelle table, pas de migration.

---

## 4. Démarche de développement

Le projet a évolué en plusieurs étapes à partir d'une base Clean Architecture existante :

1. **Alignement sur le cahier des charges validé** — une fonctionnalité de réservation temporaire de bureau (`Reservation`) avait été ajoutée hors périmètre ; elle a été entièrement retirée (entité, migration inverse, endpoints, tests, page frontend) pour ne garder que les 7 entités confirmées.
2. **Mise en place du RBAC à 3 rôles** — remplacement du modèle initial à 2 rôles (Lecteur/Gestionnaire) par le modèle Administrateur/Gestionnaire/Agent défini au cahier des charges, avec ajout des endpoints self-service `me/office` et `me/assets` pour l'Agent, et des comptes de test seedés pour chaque rôle.
3. **Ouverture d'un accès lecture au Gestionnaire** (`ReferentielLecture`) — en construisant les pages Gestionnaire (recherche de bureau, création d'affectation), il est apparu que le Gestionnaire avait besoin de lire le référentiel pour sélectionner un agent/bureau/actif, sans quoi il ne pouvait pas faire son travail quotidien malgré des droits d'affectation valides.
4. **Refonte du frontend en outil back-office** — passage d'un style vitrine à un style outil de gestion interne : tableaux denses avec tri/pagination, formulaires en tiroir latéral, fil d'ariane, badges de statut sobres, navigation groupée par rôle. Création des pages manquantes par rôle (`Batiments`, `Bureaux` séparés, `RechercheBureaux`, `AffectationsPoste`, `AffectationsActif`, `HistoriqueAffectations`, `MonBureau`, `MesActifs`).
5. **Portabilité "clone & run"** — remplacement de la dépendance à une instance SQL Server locale nommée par un `docker-compose.yml` autonome, ajout d'un retry au démarrage pour tolérer une base encore en cours d'initialisation, et documentation complète (ce fichier).
6. **Durcissement sécurité** — après audit du modèle d'authentification existant (jeton JWT unique de 8h, sans rafraîchissement ni révocation, aucune limitation de débit sur la connexion), ajout des jetons de rafraîchissement avec rotation, de la limitation de débit, et d'un journal d'audit basé sur des *domain events* pour tracer qui a fait quoi. Correction au passage des deux avertissements remontés par `dotnet build` (API de hachage obsolète, dépendance transitive vulnérable). Détail dans la section [Durcissement sécurité](#durcissement-sécurité) ci-dessus.
7. **Recherche IA** *(branche `school-ai` uniquement)* — ajout d'une recherche de bureau en langage naturel via un LLM (OpenRouter), avec repli automatique sur une recherche par mot-clé si le service IA est indisponible, pour que la fonctionnalité ne devienne jamais un point de défaillance. Détail dans la section [Intelligence artificielle](#intelligence-artificielle) ci-dessus. Le reste du projet reste identique à la branche `main` (version stage).

---

## 5. Prérequis

- [.NET SDK 10](https://dotnet.microsoft.com/download) (version exacte pinée dans `global.json`)
- [Node.js](https://nodejs.org/) 20+ et npm
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (pour la base de données locale via `docker-compose.yml`, et pour les tests d'intégration)

---

## 6. Installation et démarrage rapide

```bash
# 1. Cloner le dépôt
git clone https://github.com/othman-gr3/gestion_des_espaces.git
cd gestion_des_espaces

# 2. Démarrer la base de données SQL Server (Docker)
docker compose up -d

# 3. Appliquer les migrations EF Core
cd GestionEspaces
dotnet ef database update --project GestionEspaces.Infrastructure/GestionEspaces.Infrastructure.csproj --startup-project GestionEspaces.Api/GestionEspaces.Api.csproj

# 4. Lancer le backend (écoute sur http://localhost:5153, Swagger sur /swagger)
dotnet run --project GestionEspaces.Api/GestionEspaces.Api.csproj
```

Dans un second terminal :

```bash
# 5. Lancer le frontend (écoute sur http://localhost:5173)
cd GestionEspaces.Web
npm install
npm run dev
```

Ouvrez `http://localhost:5173` : les migrations et le jeu de données de démonstration (2 sites, 3 bâtiments, 7 bureaux, 7 agents, 7 actifs) sont appliqués automatiquement au premier démarrage du backend — aucune étape manuelle de seed n'est nécessaire.

> **Connexion à un SQL Server existant plutôt qu'au conteneur Docker ?** Créez un fichier `GestionEspaces/GestionEspaces.Api/appsettings.Development.json` (ignoré par git) avec votre propre `ConnectionStrings:GestionEspacesDatabase` — il prend le pas sur la valeur par défaut d'`appsettings.json`.

> **Clé de signature JWT** : `appsettings.json` contient une clé de développement placeholder qui fonctionne telle quelle en local (avertissement affiché au démarrage). Pour définir la vôtre : `dotnet user-secrets set "Jwt:SigningKey" "<valeur-aléatoire-32-caractères-minimum>"` depuis `GestionEspaces.Api/`.

---

## 7. Comptes de test

Seedés dans `appsettings.json` → `Users`, un compte par rôle :

| Email | Mot de passe | Rôle |
|---|---|---|
| `admin@onee.ma` | `Admin123!` | Administrateur |
| `gestionnaire@onee.ma` | `Gestion123!` | Gestionnaire |
| `y.elamrani@onee.ma` | `Agent123!` | Agent (lié à l'agent "Youssef El Amrani", qui a un bureau et des actifs déjà affectés — utile pour tester le self-service) |

---

## 8. Lancer les tests

```bash
cd GestionEspaces

# Tests unitaires — en mémoire, ne nécessitent PAS Docker
dotnet test GestionEspaces.Tests/GestionEspaces.Tests.csproj

# Tests d'intégration — nécessitent Docker Desktop actif (SQL Server via Testcontainers)
dotnet test GestionEspaces.IntegrationTests/GestionEspaces.IntegrationTests.csproj

# Les deux à la fois
dotnet test GestionEspaces.slnx
```

---

## 9. Dépannage

### Docker ne répond pas / `Docker is either not running or misconfigured`

Erreur typique rencontrée en développement :
```
System.ArgumentException : Docker is either not running or misconfigured...
```

1. Vérifiez que le démon répond : `docker ps`. Si la commande échoue ou reste bloquée, Docker Desktop n'est pas complètement démarré.
2. Redémarrez Docker Desktop entièrement (fermer puis relancer l'application, pas juste la fenêtre).
3. Si le problème persiste sous Windows, vérifiez WSL2 : `wsl --status`, puis `wsl --shutdown` suivi d'un redémarrage de Docker Desktop.
4. **Fonctionnalité "Docker AI" / "Ask Gordon"** : si les logs de Docker Desktop montrent une erreur du type `initializing Inference manager: ... The filename, directory name, or volume label syntax is incorrect`, désactivez cette fonctionnalité bêta (Docker Desktop → Paramètres → Beta features → désactiver Docker AI) — elle peut empêcher le démon de démarrer correctement.
5. Dans Docker Desktop → Paramètres → General, activez *"Start Docker Desktop when you log in"* pour éviter que le démon soit éteint entre deux sessions de travail.
6. Rappel : seuls les **tests d'intégration** et l'usage du `docker-compose.yml` nécessitent Docker. Le backend, le frontend et les tests unitaires n'en dépendent pas.

### Le port 1433 est déjà utilisé

Si vous avez déjà une instance SQL Server locale sur le port 1433, changez le mapping de port dans `docker-compose.yml` (ex. `"1434:1433"`) et mettez à jour la chaîne de connexion en conséquence, ou utilisez votre instance existante via `appsettings.Development.json` (voir section 6).

### `dotnet ef database update` échoue juste après `docker compose up -d`

Le conteneur SQL Server met quelques secondes à être prêt après son démarrage. Réessayez la commande après quelques secondes, ou vérifiez `docker inspect --format='{{.State.Health.Status}}' gestionespaces-db` (doit afficher `healthy`). Le backend lui-même retente automatiquement (5 tentatives avec délai croissant) au premier lancement pour absorber ce délai.

---

## 10. Intégration continue

Un workflow GitHub Actions ([`.github/workflows/ci.yml`](.github/workflows/ci.yml)) s'exécute à chaque push et pull request sur `main` ou `school-ai` :

- **Backend** — restauration, build en configuration `Release`, puis exécution des tests unitaires (`GestionEspaces.Tests`). Les tests d'intégration ne tournent pas en CI : ils nécessitent un vrai SQL Server via Testcontainers, ce qui alourdirait et ralentirait la vérification rapide à chaque commit.
- **Frontend** — installation des dépendances (`npm ci`) et build de production (`npm run build`), pour détecter toute erreur de compilation avant qu'elle n'atteigne `main`.

---

## 11. Structure du projet

```
gestion_des_espaces/
├── .github/workflows/ci.yml             ← Intégration continue (voir section 10)
├── docker-compose.yml                   ← Base SQL Server locale (voir section 6)
├── GestionEspaces/                      ← Solution .NET (backend)
│   ├── GestionEspaces.slnx
│   ├── GestionEspaces.Domain/           ← Entités, value objects, règles métier
│   ├── GestionEspaces.Application/      ← Use cases, DTOs, interfaces, validation
│   ├── GestionEspaces.Infrastructure/   ← EF Core, repositories, migrations
│   ├── GestionEspaces.Api/              ← Contrôleurs, JWT, policies, Swagger
│   ├── GestionEspaces.Tests/            ← Tests unitaires
│   ├── GestionEspaces.IntegrationTests/ ← Tests d'intégration (Testcontainers)
│   └── README.md                        ← Référence technique détaillée (anglais)
├── GestionEspaces.Web/                  ← Frontend React
│   └── src/
│       ├── pages/                       ← Une page par écran, organisées par rôle
│       ├── components/                  ← Composants partagés (Drawer, Breadcrumb, StatusBadge...)
│       └── services/                    ← Client API (Axios)
├── global.json                          ← Version du SDK .NET ciblée
└── .gitignore
```

Détail complet des couches, du schéma de données, des invariants métier et des mécanismes de concurrence optimiste : voir [`GestionEspaces/README.md`](GestionEspaces/README.md).
