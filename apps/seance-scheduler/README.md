# Seance Scheduler

Built from the [Crypt template](../../template) — a Kiro-ready .NET 10 skeleton for small business web apps.

## Description

Book seance sessions with mediums without double-booking: manage mediums, book 15-minute-aligned slots (overlaps are rejected, even for concurrent requests), browse the schedule by day and cancel seances. ASP.NET Core minimal API + Blazor SSR + EF Core (SQLite).

## Prerequisites

- .NET 10 SDK
- Docker (optional, for containerized deployment)

## Getting Started

### Restore and Build

```bash
# From the solution folder (created by `dotnet new crypt -n SeanceScheduler`)
# Restore dependencies
dotnet restore

# Build the project
dotnet build
```

### Run Locally

```bash
# Run the web application
dotnet run --project src/SeanceScheduler.Web

# Or use the solution
dotnet run --project src/SeanceScheduler.Web/SeanceScheduler.Web.csproj
```

The application will be available at `http://localhost:5000`.

### Run Tests

```bash
# Run all tests (integration + Playwright e2e; the first e2e run downloads Chromium)
dotnet test SeanceScheduler.slnx

# Only the browser tests
dotnet test tests/SeanceScheduler.E2E
```

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
dotnet ef migrations add <MigrationName> --project src/SeanceScheduler.Web

# Remove the last (unapplied) migration
dotnet ef migrations remove --project src/SeanceScheduler.Web
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

1. Add the provider: `dotnet add src/SeanceScheduler.Web package Npgsql.EntityFrameworkCore.PostgreSQL`.
2. In `Program.cs`, replace `UseSqlite(...)` with `UseNpgsql(...)` and point the connection string at the `postgres` service (uncomment it in `docker-compose.yml`).
3. Delete `src/SeanceScheduler.Web/Migrations/` and create a fresh initial migration - SQLite migrations are not portable to PostgreSQL.

## Project Structure

```
SeanceScheduler/
├── src/
│   └── SeanceScheduler.Web/
│       ├── Components/       # Blazor components
│       ├── Features/         # Feature-based code
│       ├── Data/             # EF Core context and models
│       ├── Migrations/       # Database migrations
│       ├── wwwroot/          # Static files
│       └── App_Data/         # SQLite database
├── tests/
│   ├── SeanceScheduler.Tests/       # Unit, HTTP and page-form integration tests
│   └── SeanceScheduler.E2E/         # Playwright e2e (Kestrel on a random port + headless Chromium)
├── .kiro/                    # Kiro configuration
├── .config/                  # Local tools (dotnet-ef)
├── Dockerfile
├── docker-compose.yml
├── SeanceScheduler.slnx
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
## Backups

The SQLite database runs in WAL mode, so `App_Data` holds `app.db` plus `app.db-wal` and `app.db-shm`.
Back up all three together while the app is stopped, or take a consistent online copy with
`sqlite3 App_Data/app.db "VACUUM INTO 'backup.db'"`.
