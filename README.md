# Zevoryn Control Center

Private internal control plane for Zevoryn SaaS products. This repository implements the foundation plus the Products & Environments milestone: product/environment lifecycle management, secret references, connection metadata, and manual health checks.

## Prerequisites

- .NET SDK 9
- Node.js 22+
- Docker Desktop / Docker Engine with Compose

## Quick start with Docker

```bash
cp .env.example .env
docker compose up --build
```

Open http://localhost:5173. API: http://localhost:5080. Live hub: http://localhost:5080/hubs/control-events. PostgreSQL is exposed on port 5442.

## Local backend

```bash
dotnet restore Zevoryn.Control.sln
dotnet build Zevoryn.Control.sln
dotnet run --project backend/Zevoryn.Control.Api
```

Docker Compose applies pending migrations when `Database__ApplyMigrations=true`. For local SDK-driven migration management:

```bash
dotnet ef database update --project backend/Zevoryn.Control.Infrastructure --startup-project backend/Zevoryn.Control.Api
```

## Local frontend

```bash
cd frontend
npm install
npm run dev
```

Set `VITE_API_BASE_URL` in `frontend/.env` if the API is not at `http://localhost:5080`.

## Tests and validation

```bash
dotnet test Zevoryn.Control.sln
cd frontend && npm test && npm run build
cd .. && docker compose config
```

## Structure

```text
backend/                 Layered .NET solution and tests
frontend/                React/Vite/TypeScript operator UI
docs/ARCHITECTURE.md     Architecture decisions and boundaries
docs/ROADMAP.md          Source-of-truth roadmap
docker-compose.yml       postgres, api, frontend
```

## Current routes

Dashboard `/`, Products `/products`, Product detail `/products/:id`, Events `/events`, Beta `/beta`, and Beta campaign detail `/beta/campaigns/:id`, plus clearly marked placeholders for Monitoring, Customers, Billing, and AI Lab.

Product deletion is intentionally a deactivation (`Inactive`) so future dependent data remains consistent. Environment deletion is hard deletion only when no ProductConnection exists; SaaSEvent environment references remain nullable. ProductConnection stores only a logical `SecretReference`, never credentials.

Health checks use a five-second HttpClient timeout and request `{BaseUrl}/health`. Only HTTP/HTTPS URLs are accepted. This is a foundation, not a complete SSRF defense; deployed environments should add allowlists, private-network egress controls, and stronger URL/IP validation.

No authentication, RBAC, customer data, billing, model execution, or direct external SaaS database access is implemented in this milestone.

Beta Management is product-agnostic. Creating an invitation creates only a central orchestration record; it does not generate a token, send email, call CleanersFlow, or call Postmark. Active invitation capacity counts every invitation except `Revoked` and `Expired`; revoking an invitation frees capacity.
