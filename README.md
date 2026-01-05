# Converge Configuration Service

Lightweight configuration management microservice supporting tenant/global scopes, versioning and rollback. This repository contains an ASP.NET Core Web API, an in-memory config service (default), a Redis cache decorator, and optional Postgres persistence wiring.

Goals
- Tenant-scoped and global configuration
- Immutable versioned updates and rollback
- Read-through caching via Redis for low-latency reads
- Optional Postgres persistence for production durability
- Clean domain error responses (mapped by middleware)

Contents
- `src/Converge.Configuration.API` � Web API and middleware
- `src/Converge.Configuration.Application` � application services, DTOs, handlers
- `src/Converge.Configuration.Domain` � domain entities and interfaces
- `src/Converge.Configuration.Persistence` � EF Core DbContext + repository (Postgres)
- `postman/Converge.Configuration.postman_collection.json` � Postman collection to exercise API

Prerequisites
- .NET 10 SDK
- (Optional) Docker for local Postgres + Redis
- (Optional) dotnet-ef tool for EF migrations: `dotnet tool install --global dotnet-ef`

Quickstart � run with default in-memory service
1. From repo root run:
   `dotnet build`
   `dotnet run --project src/Converge.Configuration.API`
2. API will listen on the configured ASP.NET Core URLs (see console). Default wiring uses the in-memory `IConfigService`.
3. Import `postman/Converge.Configuration.postman_collection.json` into Postman and run requests.

Enable Redis caching (optional)
- Set configuration (appsettings.json or environment):
  ```json
  {
    "Caching": { "Enabled": true },
    "ConnectionStrings": { "Redis": "localhost:6379" }
  }
  ```
- Restart the API. When enabled, `CachedConfigService` decorates `IConfigService` to cache `GetEffective` results and invalidate on writes.

Enable Postgres persistence (optional)
- Provide a Postgres connection string in `appsettings.json` or env:
  ```json
  {
    "ConnectionStrings": {
      "Postgres": "Host=localhost;Port=5432;Database=configuration;Username=postgres;Password=postgres"
    },
    "Persistence": { "UsePostgres": true }
  }
  ```
- Ensure `src/Converge.Configuration.Persistence` project is referenced by the API project (already present in the solution).
- Create and apply EF migrations (from repo root):
  ```bash
  dotnet ef migrations add InitialConfiguration \
    --project src/Converge.Configuration.Persistence \
    --startup-project src/Converge.Configuration.API -o src/Converge.Configuration.Persistence/Migrations

  dotnet ef database update \
    --project src/Converge.Configuration.Persistence \
    --startup-project src/Converge.Configuration.API
  ```
- Start the API; when `Persistence:UsePostgres=true` the API uses a DB-backed `IConfigService`.

Local Docker Compose (recommended for dev)
Create `docker-compose.yml` with this content to run Postgres + Redis for local testing:

```yaml
version: "3.8"
services:
  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: configuration
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      retries: 5

  redis:
    image: redis:7
    ports:
      - "6379:6379"
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      retries: 5
```

Then:
- `docker compose up -d`
- Update your `appsettings.json` or environment variables to point to the containers
- Run migrations and start the API

Error handling & responses
- The API uses domain exceptions and a global middleware to return clean JSON errors. Example error responses:
  - 409 Conflict { "error": "ConfigurationAlreadyExists", "message": "...", "correlationId": "..." }
  - 409 Conflict { "error": "VersionConflict", "message": "...", "correlationId": "..." }
  - 400 Bad Request { "error": "InvalidRequest", "message": "...", "correlationId": "..." }
  - 500 Internal Server Error { "error": "InternalServerError", "message": "An unexpected error occurred.", "correlationId": "..." }

Postman collection
- Import `postman/Converge.Configuration.postman_collection.json` and set environment variables `baseUrl`, `tenantId`, etc.
- A pre-request script in the collection sets `correlationId` automatically.

Testing
- Unit tests: `dotnet test src/Converge.UnitTests` (adjust path if needed)
- Integration tests: recommend using Testcontainers to spin up Postgres and Redis.

Development notes / TODOs
- `DbConfigService` in `src/Converge.Configuration.Application/Services` is a minimal DB-backed implementation. Replace `Guid.Empty` creator resolution with authenticated user context when integrating auth/audit.
- Consider publishing domain events (ConfigCreated/Updated/RolledBack) after writes for cache invalidation across instances.
- Add RBAC integration (PolicyEngine) for GLOBAL vs TENANT access control.

Contributing
- Fork, create feature branch `feat/<name>` and open a PR against `main`.
- Include unit/integration tests and, if changing schema, include an EF migration.

License & contacts
Proprietary - ConvergeERP
