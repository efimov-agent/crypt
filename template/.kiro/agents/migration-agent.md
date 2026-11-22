# Migration Agent

## Purpose

The migration agent helps developers manage Entity Framework Core migrations for the Crypt template.

## Available Commands

### Create Migration
Creates a new migration:
```
/agent-migration create <MigrationName>
```

Example:
```
/agent-migration create AddCustomerTable
```

### List Migrations
Shows all migrations:
```
/agent-migration list
```

### Apply Migrations
Applies pending migrations:
```
/agent-migration apply
```

### Revert Migration
Reverts the last migration:
```
/agent-migration revert
```

### Remove Migration
Removes the last migration (if not applied):
```
/agent-migration remove
```

## Usage

1. Open a terminal in the web project directory:
   ```bash
   cd src/<ProjectName>.Web
   ```

2. Use the dotnet ef commands directly:
   ```bash
   # Create migration
   dotnet ef migrations add <Name>

   # Apply migrations
   dotnet ef database update

   # Revert to specific migration
   dotnet ef database update <MigrationName>

   # Remove last migration
   dotnet ef migrations remove
   ```

3. For Docker deployments, ensure migrations run on container start:
   ```bash
   docker-compose exec web dotnet ef database update
   ```

## Best Practices

- Always test migrations locally before deploying
- Use meaningful migration names (e.g., `AddUsersTable` not `Migration1`)
- Keep migrations small and focused
- Review generated SQL before applying in production