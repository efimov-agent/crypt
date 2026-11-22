# Crypt Application

A Kiro-ready .NET 10 skeleton for small business web apps.

## Description

A Kiro-ready .NET 10 web app built with ASP.NET Core, Blazor, and Entity Framework Core.

## Prerequisites

- .NET 10 SDK
- Docker (optional, for containerized deployment)

## Getting Started

### Clone and Build

```bash
# Clone the repository
git clone <repository-url>
cd CryptApp

# Restore dependencies
dotnet restore

# Build the project
dotnet build
```

### Run Locally

```bash
# Run the web application
dotnet run --project src/CryptApp.Web

# Or use the solution
dotnet run --project src/CryptApp.Web/CryptApp.Web.csproj
```

The application will be available at `http://localhost:5000`.

### Run Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test
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
dotnet ef migrations add <MigrationName> --project src/CryptApp.Web

# Remove the last (unapplied) migration
dotnet ef migrations remove --project src/CryptApp.Web
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

To use PostgreSQL instead of SQLite, uncomment the `postgres` service in `docker-compose.yml` and update the connection string in `Program.cs`.

## Project Structure

```
CryptApp/
├── src/
│   └── CryptApp.Web/
│       ├── Components/       # Blazor components
│       ├── Features/         # Feature-based code
│       ├── Data/             # EF Core context and models
│       ├── Migrations/       # Database migrations
│       ├── wwwroot/          # Static files
│       └── App_Data/         # SQLite database
├── tests/
│   └── CryptApp.Tests/       # Integration tests
├── .kiro/                    # Kiro configuration
├── .config/                  # Local tools (dotnet-ef)
├── Dockerfile
├── docker-compose.yml
├── CryptApp.slnx
└── README.md
```

## Kiro Integration

This project includes Kiro configuration:

- **Steering** (`.kiro/steering/`): `tech.md`, `structure.md`, `product.md` - always-included guides for stack, layout and UI tone.
- **Hooks** (`.kiro/hooks/dotnet.json`, hooks format `v1`):
  - `test-on-stop` - when the agent finishes a turn, runs `dotnet test` once (builds and tests; can take ~30-60 s). Set `"enabled": false` on the hook to turn it off.
  - `guard-migrations` - when the agent edits a file under `Migrations/`, asks it to revert and create a new migration instead. Agent-type hook actions are an IDE feature; Kiro CLI (3.5) may skip them.
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