# CryptApp - Project Structure

## Solution Layout

```
CryptApp/
├── src/
│   └── CryptApp.Web/
│       ├── Components/
│       │   ├── Layout/
│       │   │   ├── MainLayout.razor
│       │   │   └── MainLayout.razor.css
│       │   ├── Pages/
│       │   │   ├── Index.razor
│       │   │   └── Error.razor
│       │   ├── App.razor
│       │   ├── Routes.razor
│       │   └── _Imports.razor
│       ├── Features/
│       │   └── Items/
│       │       ├── CreateItemRequest.cs   # request DTO (no Id), validated in the endpoint
│       │       ├── ItemsService.cs
│       │       └── ItemsEndpoints.cs
│       ├── Data/
│       │   ├── ApplicationDbContext.cs
│       │   └── Models/
│       │       └── Item.cs
│       ├── Migrations/
│       ├── wwwroot/
│       │   └── css/
│       │       └── app.css
│       ├── App_Data/
│       ├── Program.cs
│       ├── CryptApp.Web.csproj
│       └── Properties/
├── tests/
│   └── CryptApp.Tests/
│       ├── Fixtures/
│       │   └── TestDatabaseFixture.cs
│       ├── Features/
│       │   └── ItemsTests.cs
│       └── CryptApp.Tests.csproj
├── .kiro/
│   ├── steering/
│   ├── hooks/       # dotnet.json (Kiro IDE), guard-migrations.sh (used by agent hooks in Kiro CLI)
│   ├── agents/      # feature-scaffold.json, migrations.json
│   └── prompts/     # agent system prompts
├── Dockerfile
├── docker-compose.yml
├── .dockerignore
├── .config/dotnet-tools.json
├── CryptApp.slnx
└── README.md
```

## Feature-Based Organization

Features are organized in the `Features/<Name>/` folder rather than by technical layer. Each feature folder contains:
- Service classes (business logic)
- Endpoint classes (API routes)
- Related components (if any)

This keeps all code related to a feature together.

## Build Commands

```bash
# Build the solution
dotnet build

# Run tests
dotnet test

# Run specific project
dotnet run --project src/CryptApp.Web

```

## Key Files

- **Program.cs**: Application entry point with DI setup, middleware pipeline, and endpoint registration
- **ApplicationDbContext.cs**: EF Core database context
- **ItemsService.cs**: Example service with CRUD operations
- **ItemsEndpoints.cs**: Example minimal API endpoints
- **TestDatabaseFixture.cs**: Custom WebApplicationFactory for integration tests