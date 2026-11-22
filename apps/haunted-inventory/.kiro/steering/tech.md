# HauntedInventory - Technical Guide

## Overview

HauntedInventory is a small business web app built on .NET 10 with ASP.NET Core and Blazor.

## Tech Stack

- **.NET 10** with C# latest
- **ASP.NET Core** with Blazor Web App (static SSR)
- **Entity Framework Core 10** with SQLite (PostgreSQL needs the Npgsql provider and its own migrations, see README)
- **xUnit** for integration tests against a temporary SQLite database file (`WebApplicationFactory`)

## Key Conventions

### Nullable Types
All code uses `<Nullable>enable</Nullable>`. Always use nullable reference types (`string?`, `object?`) when a value can be null.

### Warnings as Errors
`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` is enabled. All warnings must be addressed.

### Async/Await
All database operations and HTTP handlers are async. Always use `CancellationToken`:
```csharp
public async Task<IReadOnlyList<Item>> GetItemsAsync(CancellationToken cancellationToken)
```

### Logging
Use `ILogger<T>` for all logging:
```csharp
private readonly ILogger<ItemsService> _logger;
_logger.LogInformation("Fetching items at {Time}", DateTime.UtcNow);
```

### Configuration
Use the Options pattern with validation:
```csharp
builder.Services.Configure<MyOptions>(configuration.GetSection("MyOptions"));
```

## Project Structure

```
src/<ProjectName>.Web/
  Components/       # Blazor components
  Features/         # Feature-based organization
  Data/             # EF Core context and models
  wwwroot/          # Static files
  App_Data/         # SQLite database

tests/<ProjectName>.Tests/
  Fixtures/         # Test fixtures
  Features/         # Feature tests
```

## Build and Test

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run application
dotnet run --project src/<ProjectName>.Web
```

## Docker

```bash
# Build image
docker build -t crypt-app .

# Run with docker-compose
docker-compose up
```