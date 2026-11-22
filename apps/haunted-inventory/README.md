# Haunted Inventory

A Kiro-ready .NET 10 skeleton for small business web apps.

## Description

Stock tracking for a costume and prop rental shop built with ASP.NET Core, Blazor, and Entity Framework Core.

## Prerequisites

- .NET 10 SDK
- Docker (optional, for containerized deployment)

## Getting Started

### Restore and Build

```bash
# From the solution folder (created by `dotnet new crypt -n HauntedInventory`)
# Restore dependencies
dotnet restore

# Build the project
dotnet build
```

### Run Locally

```bash
# Run the web application
dotnet run --project src/HauntedInventory.Web

# Or use the solution
dotnet run --project src/HauntedInventory.Web/HauntedInventory.Web.csproj
```

The application will be available at `http://localhost:5000`.

### Run Tests

```bash
# Run all tests (integration + browser e2e)
dotnet test

# Only the browser e2e tests (Playwright, headless Chromium)
dotnet test tests/HauntedInventory.E2E
```

The e2e tests start the real app on Kestrel (random port, throw-away SQLite file) and drive `/items` and
`/items/{id}` in headless Chromium. The first run downloads Chromium for Playwright (~150 MB, once per machine).

## Local Development

### Database

The application uses SQLite by default. The database file is created automatically in `App_Data/app.db`.

### API Endpoints

- **Health Check**: `GET /api/health`
- **Items API**: 
  - `GET /api/items` - List all items
  - `GET /api/items/{id}` - Get item by ID
  - `POST /api/items` - Create new item
  - `DELETE /api/items/{id}` - Delete item

### Entity Framework Migrations

The EF Core CLI is pinned as a local tool (`.config/dotnet-tools.json`). Migrations are applied automatically on startup.

```bash
# Restore the pinned dotnet-ef once
dotnet tool restore

# Add a migration
dotnet ef migrations add <MigrationName> --project src/HauntedInventory.Web

# Remove the last (unapplied) migration
dotnet ef migrations remove --project src/HauntedInventory.Web
```

## Docker Deployment

### Build Image

```bash
docker build -t crypt-app .
```

### Run with Docker Compose

```bash
docker-compose up
```

The application will be available at `http://localhost:5000`.

### Enable PostgreSQL

The app is configured for SQLite only. Switching to PostgreSQL is a manual change:

1. Add the provider: `dotnet add src/HauntedInventory.Web package Npgsql.EntityFrameworkCore.PostgreSQL`.
2. In `Program.cs`, replace `UseSqlite(...)` with `UseNpgsql(...)` and point the connection string at the `postgres` service (uncomment it in `docker-compose.yml`).
3. Delete `src/HauntedInventory.Web/Migrations/` and create a fresh initial migration - SQLite migrations are not portable to PostgreSQL.

## Project Structure

```
HauntedInventory/
├── src/
│   └── HauntedInventory.Web/
│       ├── Components/       # Blazor components
│       ├── Features/         # Feature-based code
│       ├── Data/             # EF Core context and models
│       ├── Migrations/       # Database migrations
│       ├── wwwroot/          # Static files
│       └── App_Data/         # SQLite database
├── tests/
│   ├── HauntedInventory.Tests/       # Integration tests
│   └── HauntedInventory.E2E/         # Browser e2e tests (Playwright)
├── .kiro/                    # Kiro configuration
├── .config/                  # Local tools (dotnet-ef)
├── Dockerfile
├── docker-compose.yml
├── HauntedInventory.slnx
└── README.md
```

## Kiro Integration

This project includes Kiro configuration:

- **Steering** (`.kiro/steering/`): `tech.md`, `structure.md`, `product.md` - always-included guides for stack, layout and UI tone.
- **Hooks** - the same two hooks in two formats:
  - `test-on-stop` - when the agent finishes a turn, runs `dotnet test` once (builds and tests; output goes to stderr so failures reach the agent; can take ~30-60 s);
  - `guard-migrations` - stops hand edits of EF-generated `Migrations/*.Designer.cs` and the model snapshot and points the agent to `dotnet ef migrations add`; editing `Up`/`Down` of a new migration (e.g. a data backfill with `migrationBuilder.Sql`) stays allowed.
  - Kiro IDE reads `.kiro/hooks/dotnet.json` (standalone hooks, format `v1`; set `"enabled": false` to turn one off).
  - Kiro CLI runs hooks declared inside the agent config, so both custom agents carry them in their `"hooks"` array: `Stop` for tests and `PreToolUse` on `fs_write|fs_append|str_replace` calling `.kiro/hooks/guard-migrations.sh`, which blocks the write with exit code 2 (checked with kiro-cli 3.5).
- **Custom agents** (`.kiro/agents/*.json`, prompts in `.kiro/prompts/`):
  - `feature-scaffold` - adds a vertical-slice feature (entity, DTO, service, endpoints, tests) mirroring `Items`;
  - `migrations` - creates and reviews EF Core migrations, flags data loss.

Use them from Kiro CLI, for example `kiro-cli chat --agent-engine v3 --agent feature-scaffold` (workspace agents and `v1` hooks need the V3 engine), or open the folder in Kiro IDE.

## Contributing

1. Create a feature branch
2. Make changes following the coding conventions
3. Ensure build passes: `dotnet build`
4. Ensure tests pass: `dotnet test`
5. Submit a pull request

## License

MIT License