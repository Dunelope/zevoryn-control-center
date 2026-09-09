# Zevoryn Control Center

Private internal control plane for Zevoryn SaaS products. This repository implements the initial production-oriented foundation from Milestone 0, with Products/Environments and the generic real-time event foundation.

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

Apply the migration with:

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

Dashboard `/`, Products `/products`, Product detail `/products/:id`, Events `/events`, plus clearly marked placeholders for Monitoring, Beta, Customers, Billing, and AI Lab.

No authentication, RBAC, customer data, billing, model execution, or direct external SaaS database access is implemented in this milestone.
