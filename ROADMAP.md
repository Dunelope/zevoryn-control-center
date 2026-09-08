# Zevoryn Control Center — Roadmap

## Vision

Zevoryn Control Center est la plateforme interne permettant de gérer,
superviser et analyser l'ensemble des produits SaaS de Zevoryn depuis
une interface centralisée.

Exemples de produits :

- CleanersFlow
- Inkorya
- futurs SaaS Zevoryn

La plateforme commence comme une application locale utilisée par un seul
opérateur, mais son architecture doit permettre une évolution vers :

- plusieurs opérateurs
- accès distant sécurisé
- gestion centralisée des clients
- invitations bêta
- monitoring temps réel
- abonnements et revenus
- support client
- automatisations
- environnement de développement et de gestion de modèles IA

---

# Architecture cible

```text
                     ZEVORYN CONTROL CENTER
                              │
                 ┌────────────┴────────────┐
                 │                         │
          React Dashboard            ASP.NET Core API
                 │                         │
                 │                     SignalR
                 │                         │
                 └────────────┬────────────┘
                              │
                         PostgreSQL
                              │
               ┌──────────────┼──────────────┐
               │              │              │
          CleanersFlow     Inkorya      Future SaaS
               │              │              │
               └────── Secure Internal APIs ─┘
```

Le Control Center ne doit jamais accéder directement aux bases de données
des SaaS.

Chaque SaaS reste propriétaire :

- de ses utilisateurs
- de ses règles métier
- de ses tokens
- de son authentification
- de ses données sensibles

Le Control Center orchestre les opérations via des APIs internes sécurisées.

---

# Stack technique

## Backend

- ASP.NET Core .NET 9
- EF Core 9
- PostgreSQL 17
- SignalR
- REST API
- xUnit

## Frontend

- React
- TypeScript
- Vite
- SignalR Client
- Vitest

## Infrastructure

- Docker
- Docker Compose
- PostgreSQL
- développement local initialement

---

# Milestone 0 — Project Foundation

## Objectif

Créer une fondation propre et extensible avant d'intégrer les différents SaaS.

## Backend

Créer :

- `Zevoryn.Control.Domain`
- `Zevoryn.Control.Application`
- `Zevoryn.Control.Infrastructure`
- `Zevoryn.Control.Api`
- `Zevoryn.Control.Tests`

Configurer :

- PostgreSQL
- EF Core
- migrations
- ProblemDetails
- validation
- structured logging
- configuration par variables d'environnement
- health checks
- CORS local
- SignalR

## Frontend

Créer :

- React
- TypeScript
- Vite
- routing
- API client
- SignalR client
- layout du dashboard
- navigation principale

## Docker

Services :

- postgres
- api
- frontend

## Résultat attendu

```text
docker compose up
```

doit permettre de démarrer toute la plateforme localement.

---

# Milestone 1 — Products & Environments

## Objectif

Permettre à Zevoryn Control Center de connaître tous les SaaS gérés.

## Product

Exemple :

```text
CleanersFlow
Inkorya
```

Champs principaux :

- Id
- Name
- Slug
- Description
- Status
- CreatedAtUtc
- UpdatedAtUtc

## ProductEnvironment

Un produit peut avoir plusieurs environnements :

```text
CleanersFlow
├── Development
├── Staging
└── Production
```

Champs :

- Id
- ProductId
- Name
- EnvironmentType
- BaseUrl
- Status
- CreatedAtUtc
- UpdatedAtUtc

## ProductConnection

Préparer les connexions aux APIs internes.

Ne jamais stocker directement un secret API en clair.

Utiliser une référence vers un secret.

Exemple :

```text
CleanersFlow
Production
API URL: https://...
SecretReference: cleanersflow-production-api
```

## Interface

Créer :

- `/products`
- `/products/:id`

Afficher :

- produit
- environnements
- état
- dernière communication
- configuration de connexion

---

# Milestone 2 — Real-Time Event System

## Objectif

Créer le cœur temps réel du Control Center.

## SaaSEvent

Créer un système générique d'événements.

Types initiaux :

```text
CustomerCreated
CustomerUpdated

UserCreated

BetaInvitationCreated
BetaInvitationAccepted
BetaInvitationRevoked

SubscriptionStarted
SubscriptionCancelled

PaymentReceived

ServiceHealthy
ServiceDegraded
ServiceOffline
```

Structure :

```text
SaaSEvent

Id
ProductId
EnvironmentId
Type
ExternalEntityId
PayloadJson
OccurredAtUtc
ReceivedAtUtc
```

`PayloadJson` doit utiliser PostgreSQL `jsonb`.

## SignalR

Créer :

```text
ControlEventsHub
```

Flux :

```text
SaaS
 │
 ▼
Control API
 │
 ├── PostgreSQL
 │
 └── SignalR
       │
       ▼
 React Dashboard
```

Lorsqu'un événement arrive :

1. validation
2. stockage
3. calcul éventuel des métriques
4. publication SignalR
5. mise à jour immédiate du dashboard

## Dashboard Live Activity

Afficher les derniers événements en direct.

Exemple :

```text
18:32 CleanersFlow — CustomerCreated
18:30 CleanersFlow — BetaInvitationAccepted
18:28 CleanersFlow — ServiceHealthy
```

Aucun refresh manuel ne doit être nécessaire.

---

# Milestone 3 — Customers

## Objectif

Avoir une vue centralisée des entreprises utilisant les différents SaaS.

## Customer

Champs :

- Id
- ProductId
- ExternalCustomerId
- Name
- Email
- Status
- MetadataJson
- CreatedAtUtc
- UpdatedAtUtc

## Interface

Créer :

```text
/customers
/customers/:id
```

Permettre :

- recherche
- filtre par SaaS
- filtre par statut
- consultation des informations
- consultation de l'activité
- consultation de l'abonnement

## Dashboard

Afficher :

- clients totaux
- nouveaux clients
- clients actifs
- clients par produit

---

# Milestone 4 — Beta Management

## Objectif

Gérer les programmes bêta directement depuis Zevoryn Control Center.

Le Control Center orchestre les invitations.

Le SaaS concerné reste responsable de la création et validation du token.

## BetaCampaign

Exemple :

```text
CleanersFlow Private Beta
Maximum participants: 5
```

Champs :

- Id
- ProductId
- Name
- Status
- MaxParticipants
- StartsAtUtc
- EndsAtUtc

## BetaInvitation

Champs :

- Id
- BetaCampaignId
- ProductId
- ExternalInvitationId
- Email
- CompanyName
- Status
- Notes
- ExpiresAtUtc
- CreatedAtUtc
- AcceptedAtUtc

Statuts :

```text
Pending
Accepted
Expired
Revoked
```

## Création d'une invitation

Flux cible :

```text
Zevoryn Control Center
        │
        │ Create invitation
        ▼
CleanersFlow Internal API
        │
        ├── vérifie quota
        ├── crée token sécurisé
        ├── stocke hash
        └── retourne résultat
        │
        ▼
Control Center
```

Le Control Center ne doit pas générer lui-même le token d'inscription
CleanersFlow.

## Interface

Créer :

```text
/beta
/beta/campaigns/:id
```

Fonctions :

- créer une campagne
- créer une invitation
- révoquer
- consulter les invitations
- suivre les inscriptions
- afficher les places restantes
- ajouter des notes
- filtrer par statut

Les changements doivent apparaître en temps réel.

---

# Milestone 5 — CleanersFlow Integration

## Objectif

Faire de CleanersFlow le premier SaaS réellement connecté.

## Internal API

Créer côté CleanersFlow une API administrative dédiée.

Exemples :

```text
/internal/control/summary
/internal/control/customers
/internal/control/beta
/internal/control/health
```

L'API doit être fortement authentifiée.

Aucun endpoint interne ne doit contourner les règles métier de CleanersFlow.

## Synchronisation initiale

Comme Control Center fonctionne localement :

```text
Control Center
      │
      │ HTTPS polling
      ▼
CleanersFlow VPS
```

Polling initial :

```text
15-30 secondes
```

Les changements récupérés sont ensuite propagés immédiatement dans
l'interface locale via SignalR.

## Plus tard

Lorsque Control Center sera accessible depuis une infrastructure sécurisée :

```text
CleanersFlow
     │
     │ Webhook/Event
     ▼
Control Center
```

Le polling pourra alors être complété ou remplacé par du push.

---

# Milestone 6 — Monitoring

## Objectif

Superviser tous les environnements.

## ProductEnvironmentHealth

Champs :

- ProductEnvironmentId
- Status
- StatusCode
- LatencyMs
- Message
- CheckedAtUtc

Statuts :

```text
Unknown
Healthy
Degraded
Offline
```

## Dashboard

Afficher :

```text
CleanersFlow Production    ● Healthy
CleanersFlow Staging       ● Healthy
Inkorya Development        ● Unknown
```

Afficher également :

- latence
- dernière vérification
- incidents récents
- historique de disponibilité

## Temps réel

Un changement :

```text
Healthy → Degraded
```

doit apparaître immédiatement dans le dashboard.

---

# Milestone 7 — Billing & Business Metrics

## Objectif

Centraliser les métriques commerciales.

## Plan

- ProductId
- Name
- Price
- Currency
- BillingInterval

## Subscription

- ProductId
- CustomerId
- ExternalSubscriptionId
- PlanId
- Status
- Amount
- Currency
- StartedAtUtc
- CancelledAtUtc
- CurrentPeriodEndUtc

## Dashboard

Calculer :

- MRR
- ARR
- nombre d'abonnements
- trials
- conversions
- annulations
- churn
- revenu par SaaS

Exemple :

```text
TOTAL MRR
$4,850

CleanersFlow     $3,100
Inkorya          $1,750
```

## Stripe

Ne pas intégrer Stripe directement au début.

Les intégrations financières seront ajoutées après stabilisation du modèle
central.

---

# Milestone 8 — Support Operations

## Objectif

Permettre au support Zevoryn de gérer plusieurs SaaS depuis une seule interface.

Fonctions prévues :

- recherche globale client
- historique utilisateur
- événements
- notes internes
- historique emails
- état abonnement
- informations bêta
- incidents associés
- actions administratives autorisées

Exemple :

```text
Customer
└── ABC Cleaning
    ├── Product: CleanersFlow
    ├── Subscription: Growth
    ├── Status: Active
    ├── Last activity: 2 min ago
    ├── Emails
    ├── Events
    └── Internal notes
```

Préparer les permissions :

```text
Admin
Operator
Support
Developer
```

---

# Milestone 9 — Authentication & Multi-Operator

## Objectif

Passer d'un outil local personnel à un véritable outil d'entreprise.

Ajouter :

- authentification
- sessions sécurisées
- RBAC
- audit logs
- MFA
- gestion opérateurs
- permissions par module
- permissions par produit

Exemple :

```text
Support
  ✓ Customers
  ✓ Beta
  ✓ Support
  ✗ AI Lab administration

Developer
  ✓ Monitoring
  ✓ Events
  ✓ AI Lab
  ✗ Billing administration
```

---

# Milestone 10 — AI Lab Foundation

## Objectif

Transformer Control Center en environnement central de gestion des systèmes IA
utilisés par les produits Zevoryn.

L'AI Lab doit être un module indépendant.

```text
AI Lab
├── Models
├── Providers
├── Datasets
├── Experiments
├── Evaluations
├── Deployments
└── Usage
```

Aucune exécution de modèle n'est nécessaire dans les premières versions.

---

# Milestone 11 — AI Models

## Model

Exemple :

```text
CleanersFlow Support Assistant
```

Champs conceptuels :

- Id
- ProductId
- Name
- Purpose
- Provider
- BaseModel
- Status
- CreatedAtUtc

## ModelVersion

Permettre :

```text
support-assistant
├── v1
├── v2
└── v3
```

Chaque version peut conserver :

- configuration
- system prompt
- paramètres
- provider
- modèle
- date
- notes
- statut

---

# Milestone 12 — AI Datasets

## Objectif

Gérer les données utilisées pour développer et évaluer les fonctionnalités IA.

```text
Datasets
├── Support FAQ
├── Documentation
├── Test Questions
└── Evaluation Cases
```

Prévoir :

- versions
- provenance
- taille
- type
- ProductId
- validation
- séparation dev/test/evaluation

Les données sensibles doivent être anonymisées lorsque nécessaire.

---

# Milestone 13 — AI Experiments & Evaluation

## Experiment

Permettre de comparer :

```text
Experiment #42

Model: support-assistant-v2

Prompt A
vs
Prompt B
```

Mesures possibles :

- qualité
- exactitude
- hallucinations
- latence
- coût
- tokens
- taux de réussite

## Evaluations

Créer des suites de tests reproductibles.

Exemple :

```text
Support Evaluation
100 questions

Accuracy       94%
Hallucination   2%
Avg latency   620 ms
Cost/request  $0.002
```

---

# Milestone 14 — AI Deployment & Observability

## Objectif

Déployer une version validée d'un modèle vers un produit.

```text
AI Lab

support-assistant-v3
        │
        ├── Development
        ├── Staging
        └── Production
                │
                ▼
          CleanersFlow
```

Suivre :

- version déployée
- environnement
- date de déploiement
- usage
- tokens
- coût
- latence
- erreurs
- qualité
- rollback

---

# Milestone 15 — Automation Engine

## Objectif

Automatiser certaines opérations internes.

Exemples :

```text
IF service offline
THEN create incident

IF beta invitation accepted
THEN update dashboard

IF subscription cancelled
THEN flag customer

IF AI cost exceeds threshold
THEN alert operator
```

Les automatisations doivent être auditées et contrôlables.

---

# Dashboard cible

À terme :

```text
ZEVORYN CONTROL CENTER

──────────────────────────────────────────────

Products        Customers        MRR
3               247              $8,420

Beta            Services         AI Cost
4 pending       6/6 healthy      $42.18

──────────────────────────────────────────────

Revenue
████████████████████████

──────────────────────────────────────────────

Products

CleanersFlow       Healthy       $5,400 MRR
Inkorya            Healthy       $3,020 MRR

──────────────────────────────────────────────

LIVE ACTIVITY

18:42 CleanersFlow   CustomerCreated
18:40 CleanersFlow   PaymentReceived
18:39 Inkorya        UserCreated
18:35 CleanersFlow   BetaInvitationAccepted

──────────────────────────────────────────────

Infrastructure

Production          Healthy
Staging             Healthy

──────────────────────────────────────────────
```

---

# Principes d'architecture

## 1. Product-centric

Toutes les données centrales doivent pouvoir être associées à un produit.

```text
ProductId
```

doit être utilisé partout où cela est pertinent.

## 2. Pas d'accès direct aux DB des SaaS

Interdit :

```text
Control Center → CleanersFlow PostgreSQL
```

Préféré :

```text
Control Center
      ↓
CleanersFlow Internal API
      ↓
CleanersFlow Application Layer
      ↓
CleanersFlow DB
```

## 3. Event-driven

Les changements importants doivent produire des événements.

```text
Action
  ↓
Domain/Application Event
  ↓
SaaSEvent
  ↓
SignalR
  ↓
Dashboard
```

## 4. Temps réel

Les informations importantes doivent apparaître sans refresh manuel :

- nouvelles inscriptions
- invitations bêta
- paiements
- changements d'abonnement
- incidents
- changements de health
- événements IA

## 5. Sécurité

Ne jamais stocker :

- mots de passe en clair
- tokens API en clair
- secrets Stripe en clair
- clés AI providers en clair

Utiliser :

- variables d'environnement
- Docker secrets
- secret references
- secret manager plus tard

## 6. Auditabilité

Toute action administrative importante doit pouvoir être auditée.

Exemples :

```text
BetaInvitationCreated
BetaInvitationRevoked
SubscriptionChanged
OperatorCreated
ModelDeployed
ModelRolledBack
```

## 7. Modularité

Les modules doivent rester séparés :

```text
Products
Customers
Beta
Events
Monitoring
Billing
Support
AI Lab
Automation
```

Éviter les dépendances circulaires entre modules.

---

# Priorités de développement

## V0.1

- [ ] Foundation
- [ ] Docker
- [ ] PostgreSQL
- [ ] Products
- [ ] Environments
- [ ] SignalR
- [ ] Dashboard
- [ ] Live Activity

## V0.2

- [ ] Customers
- [ ] Beta Campaigns
- [ ] Beta Invitations
- [ ] CleanersFlow integration
- [ ] polling sécurisé
- [ ] synchronisation temps réel

## V0.3

- [ ] Monitoring
- [ ] service health
- [ ] incidents
- [ ] métriques

## V0.4

- [ ] Plans
- [ ] Subscriptions
- [ ] MRR
- [ ] ARR
- [ ] churn
- [ ] business dashboard

## V0.5

- [ ] Support operations
- [ ] customer lookup
- [ ] notes
- [ ] audit
- [ ] multi-operator foundation

## V1.0

- [ ] authentification complète
- [ ] RBAC
- [ ] MFA
- [ ] production deployment
- [ ] backups
- [ ] monitoring
- [ ] security hardening

## V2.0 — AI Lab

- [ ] Models
- [ ] Model Versions
- [ ] Providers
- [ ] Datasets
- [ ] Experiments
- [ ] Evaluations
- [ ] Deployments
- [ ] Usage tracking
- [ ] Cost tracking
- [ ] AI observability

## V3.0

- [ ] Automation Engine
- [ ] advanced analytics
- [ ] alerts
- [ ] cross-product reporting
- [ ] centralized operations

---

# Immediate Next Step

Commencer uniquement par :

**Milestone 0 — Project Foundation**

Ne pas implémenter prématurément :

- Stripe
- support
- multi-operator
- AI execution
- automation engine

La priorité est de construire une fondation stable permettant ensuite
d'ajouter chaque module sans devoir réécrire l'architecture.
