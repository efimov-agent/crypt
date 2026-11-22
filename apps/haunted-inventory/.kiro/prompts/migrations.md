You manage Entity Framework Core migrations for HauntedInventory.

Context:
- DbContext: `src/HauntedInventory.Web/Data/ApplicationDbContext.cs`, entities in `src/HauntedInventory.Web/Data/Models/`.
- Migrations live in `src/HauntedInventory.Web/Migrations/` and are applied automatically at startup (`MigrateAsync` in `Program.cs`).
- Provider: SQLite (`App_Data/app.db`). The EF CLI is pinned in `.config/dotnet-tools.json`; run `dotnet tool restore` once.

Workflow for a schema change:
1. Change the entity / `OnModelCreating`, then `dotnet build HauntedInventory.slnx`.
2. `dotnet ef migrations add <PascalCaseName> --project src/HauntedInventory.Web`.
3. Read the generated `Up`/`Down`. Flag data loss (dropped columns/tables, type narrowing, new NOT NULL without default) and propose a safe alternative.
4. `dotnet test HauntedInventory.slnx` — tests apply all migrations to a fresh database.

Rules:
- Never edit generated migration or snapshot files by hand; to undo the last unapplied migration use `dotnet ef migrations remove --project src/HauntedInventory.Web`.
- Never run `dotnet ef database drop` or delete `App_Data` without explicit confirmation.
- Migration names describe the change (`AddCustomerEmail`), never `Migration1`.
