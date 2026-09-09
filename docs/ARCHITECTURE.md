# Zevoryn Control Center Architecture

## System context

The Control Center is a private operator-facing platform for managing multiple Zevoryn SaaS products. It owns only control-plane data and communicates with products through authenticated internal APIs. It never connects directly to a product database.

```mermaid
flowchart LR
  UI[React + TypeScript] --> API[ASP.NET Core API]
  API --> DB[(PostgreSQL 17)]
  API --> HUB[SignalR Hub]
  HUB --> UI
  API -. authenticated internal APIs .-> SaaS[CleanersFlow / Inkorya / future SaaS]
```

## Backend layers

`Domain` contains entities and simple invariants. `Application` contains DTOs, service use cases, repository abstractions, and the event publisher abstraction. `Infrastructure` contains EF Core/PostgreSQL persistence. `Api` contains HTTP, ProblemDetails, health checks, DI, and the SignalR adapter. Dependencies point inward: API → Infrastructure → Application → Domain.

## Frontend architecture

Vite serves a React application with React Router, a small typed API client, and SignalR connections for live event activity. Pages do not access PostgreSQL; all data crosses the API boundary. Placeholder routes intentionally contain no future business logic.

## PostgreSQL

The schema contains Products, ProductEnvironments, ProductConnections, and SaaSEvents. Timestamps are UTC, event payloads are `jsonb`, slugs are unique, and environment names are unique per product. Product connections have one connection per type/environment and contain only a logical secret reference. The milestone migration is `AddProductConnectionsAndLifecycle`.

Product deletion is implemented as deactivation to protect future dependent data. Environments can be hard-deleted only when no ProductConnection depends on them; event environment references are nullable and therefore do not block cleanup.

## SignalR

`POST /api/events` validates the product/environment relationship, persists the event, then invokes `IControlEventPublisher`. The API adapter publishes `saasEventReceived` through `/hubs/control-events`; the frontend updates its live activity state without a refresh.

## Product connections and secrets

`ProductConnection` represents an integration capability such as `InternalApi` or `HealthEndpoint`. `SecretReference` is a logical identifier such as `cleanersflow-production-control-api`; no API key, password, or bearer token is persisted. The local `ISecretProvider` maps it to `ZEVORYN_SECRET_CLEANERSFLOW_PRODUCTION_CONTROL_API`. Secret values are never returned by the API or logged. A cloud secret manager can replace this provider later.

## Health-check foundation

`IEnvironmentHealthChecker` uses `IHttpClientFactory`, a five-second timeout, and `{BaseUrl}/health`. Manual checks update the environment status and return status code, latency, message, and UTC checked time. Only absolute HTTP/HTTPS URLs are accepted. This leaves an explicit SSRF hardening task for secure remote deployment: use URL/IP allowlists, private-network egress controls, DNS rebinding defenses, and operator authorization.

## Product-centric design

`ProductId` is the central association key for environments and events. Modules remain separate so Customers, Beta, Monitoring, Billing, Support, AI Lab, and Automation can evolve without making the initial product catalog depend on them.

## Future SaaS integrations

Each SaaS remains the owner of its users, business rules, authentication, tokens, and sensitive data. Future adapters will call authenticated internal APIs or receive authenticated webhooks/polling responses. There is an explicit rule: **the Control Center must not access external SaaS databases directly**.

## Beta Management

BetaCampaign and BetaInvitation are central, product-agnostic orchestration records. Campaigns transition Draft → Active → Paused/Closed, with Paused → Active allowed and Closed terminal. Active invitation capacity counts Pending, Sent, Accepted, and Failed invitations; Revoked and Expired invitations free capacity. A partial unique index prevents duplicate active emails within a campaign.

Zevoryn creates the central record first, then delegates token generation, hashing, email delivery, acceptance, and provider-specific rules to the selected integration. `ExternalReference` stores only the remote invitation ID; provider failures remain retryable central `Failed` records with bounded, sanitized error details.

The target SaaS remains responsible for token generation, token validation, signup security, and invitation-specific business rules.

## Future AI Lab

AI Lab is currently only a navigation placeholder. It will later manage model development metadata and operations, but this milestone executes no models and stores no model credentials.

## Security evolution

Authentication and operator/RBAC are deliberately deferred. The extension points are the API pipeline, per-integration authenticated clients, secret references rather than plaintext credentials, structured audit events, and a future authorization policy layer. Local CORS and configuration are already environment-driven.

## CleanersFlow beta integration

Zevoryn orchestrates invitations through `CleanersFlowBetaInvitationProvider`, selected by product slug and an enabled `ProductConnection` of type `InternalApi`. The provider resolves a logical secret reference through `ISecretProvider` and sends it only as `X-Zevoryn-Control-Key` to CleanersFlow's dedicated internal control API. Its client has a timeout and redirects disabled; production should require HTTPS and restrict allowed hosts/egress to reduce SSRF risk.

CleanersFlow remains authoritative for token generation, hashing, email, acceptance, quotas, and product-specific status. Zevoryn sends its invitation ID as `sourceReference`; CleanersFlow's unique source-reference index makes create/retry idempotent. Zevoryn persists only the remote invitation ID as `ExternalReference`, never a token or hash. Status mapping is explicit: Sent, Accepted, Revoked, Expired, Pending, and unknown/error to Failed.

## Integration testing

API integration tests use Testcontainers with an isolated PostgreSQL 17 container and the real API host, EF Core migrations, repositories, ProblemDetails, and endpoints. Tests do not depend on a developer database. Health-check transport tests remain a future addition using a controlled local HTTP server.

## Local-first deployment strategy

Docker Compose runs PostgreSQL, API, and frontend locally. The API is stateless apart from PostgreSQL and can later be deployed behind TLS with managed PostgreSQL, secret storage, authentication, and restricted network access.
