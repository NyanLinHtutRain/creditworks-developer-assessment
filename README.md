# Vehicle Manager

A web application for managing vehicles and weight categories, built for the CreditWorks Software Engineer assignment.

- Add vehicles and view them in a list that can be sorted by owner, manufacturer, year or weight.
- Create, edit and delete weight categories, each with a weight range and an icon.
- A vehicle's category is calculated from its weight, so changing a range updates every vehicle straight away.
- Boundary rule: a category includes its **From** weight and excludes its **To** weight, so 500.00 kg is Medium, not Light.

| Layer | Technology |
|---|---|
| API | .NET 8, ASP.NET Core, Entity Framework Core 8 |
| Database | Microsoft SQL Server 2022 (Docker) |
| Web | React, TypeScript, Vite, IBM Carbon Design System |
| Tests | xUnit, FluentAssertions |

**.NET version.** The API targets .NET 8 (LTS), which Microsoft supports until 10 November 2026. Moving to .NET 10 (LTS, supported until November 2028) was left out on purpose for now. The upgrade means changing the target framework in the five .NET projects, updating the EF Core, test host and `dotnet-ef` package versions to match, and removing the `.WithOpenApi()` call on the health endpoint.

## Prerequisites

- .NET 8 SDK
- Node.js 20.19 or newer (or 22.12 or newer) with npm
- Docker Desktop, for the database. On Apple Silicon the SQL Server image runs under emulation.

## Run it

**1. Start the database.** Copy `.env.example` to `.env` and choose a password for `MSSQL_SA_PASSWORD` (8+ characters with upper case, lower case, digits and symbols). Then start SQL Server in Docker and wait until it shows `healthy`.

```bash
docker compose up -d
docker compose ps
```

**2. Give the API the connection string.** It contains the password, which must not be committed, so it is kept in user-secrets (stored on your machine, outside the repository). Use the password from step 1, and the port from `.env` if you changed it.

```bash
dotnet user-secrets set 'ConnectionStrings:Default' 'Server=127.0.0.1,1433;Database=VehicleManager;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True' --project api/VehicleManager.Api
```

Use the single quotes. Without them, bash and zsh treat characters such as `!` in a password as special and the command fails.

Already have your own SQL Server? Skip steps 1 and 2 and set `ConnectionStrings:Default` to point at it instead.

**3. Start the API.** In Development it creates the database, applies the migration and loads the seed data (five manufacturers and three categories) on startup.

```bash
dotnet run --project api/VehicleManager.Api --launch-profile http
```

The API listens on http://localhost:5055 and Swagger is at http://localhost:5055/swagger.

**4. Start the web app** in a second terminal, then open http://localhost:5173. The dev server forwards `/api` calls to the API, so the API must be running.

```bash
cd web
npm install
npm run dev
```

**Other environments.** Migrations only run automatically in Development. Elsewhere, apply them with:

```bash
dotnet tool restore
dotnet ef database update --project api/VehicleManager.Infrastructure --startup-project api/VehicleManager.Api
```

## Test it

```bash
dotnet test api/VehicleManager.sln          # unit and integration tests
dotnet test api/VehicleManager.UnitTests    # unit tests only, no database needed
```

The integration tests run the real API against SQL Server, so steps 1 and 2 above must be done first. Each test class creates its own throwaway database and drops it afterwards, and your development data is not touched.

There are no frontend tests. In `web`, `npm run lint` and `npm run build` check the code and the types.

## Project layout

```
api/
  VehicleManager.Domain             entities and business rules, no dependencies
  VehicleManager.Infrastructure     EF Core context, migrations, seed data
  VehicleManager.Api                endpoints, services, error handling
  VehicleManager.UnitTests          rule tests, no database
  VehicleManager.IntegrationTests   API tests against SQL Server
web/                                React app
docker-compose.yml                  SQL Server for local development
DESIGN.md                           design decisions, assumptions, limitations
```

Architecture, database design, how gaps and overlaps are prevented, assumptions and known limitations are in [DESIGN.md](DESIGN.md).

---

Built by **Nyan Lin Htut Rain**
