# CryptApp - Project Structure

## Generated Project Structure

When you run `dotnet new crypt -n MyApp`, the following structure is created:

```
MyApp/
├── src/
│   └── MyApp.Web/
│       ├── Components/
│       │   ├── Layout/
│       │   │   ├── MainLayout.razor
│       │   │   └── MainLayout.razor.css
│       │   ├── Pages/
│       │   │   └── Index.razor
│       │   ├── App.razor
│       │   └── _Imports.razor
│       ├── Features/
│       │   └── Items/
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
│       ├── MyApp.Web.csproj
│       └── Properties/
├── tests/
│   └── MyApp.Tests/
│       ├── Fixtures/
│       │   └── TestDatabaseFixture.cs
│       ├── Features/
│       │   └── ItemsTests.cs
│       └── MyApp.Tests.csproj
├── .kiro/
│   ├── steering/
│   ├── hooks/dotnet.json
│   ├── agents/      # feature-scaffold.json, migrations.json
│   └── prompts/     # agent system prompts
├── Dockerfile
├── docker-compose.yml
├── .dockerignore
├── .config/dotnet-tools.json
├── MyApp.slnx
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
dotnet run --project src/MyApp.Web

```

## Key Files

- **Program.cs**: Application entry point with DI setup, middleware pipeline, and endpoint registration
- **ApplicationDbContext.cs**: EF Core database context
- **ItemsService.cs**: Example service with CRUD operations
- **ItemsEndpoints.cs**: Example minimal API endpoints
- **TestDatabaseFixture.cs**: Custom WebApplicationFactory for integration tests