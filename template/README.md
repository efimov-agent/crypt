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

```bash
# Navigate to web project
cd src/CryptApp.Web

# Add a migration
dotnet ef migrations add <MigrationName>

# Apply migrations
dotnet ef database update

# Remove last migration
dotnet ef migrations remove
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
├── Dockerfile
├── docker-compose.yml
├── CryptApp.slnx
└── README.md
```

## Kiro Integration

This project includes Kiro configuration:

- **Steering**: `.kiro/steering/` - Technical, structure, and product guides
- **Hooks**: `.kiro/hooks/` - Build and test automation
- **Agents**: `.kiro/agents/` - Migration and feature scaffold agents

Open the project in Kiro IDE to take advantage of these features.

## Contributing

1. Create a feature branch
2. Make changes following the coding conventions
3. Ensure build passes: `dotnet build`
4. Ensure tests pass: `dotnet test`
5. Submit a pull request

## License

MIT License