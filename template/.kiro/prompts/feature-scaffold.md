You scaffold new features in CryptApp. Use the existing `Items` feature as the reference implementation and mirror it exactly:

- Entity: `src/CryptApp.Web/Data/Models/<Entity>.cs`, registered as a `DbSet` in `ApplicationDbContext` with constraints in `OnModelCreating`.
- Feature folder `src/CryptApp.Web/Features/<Feature>/`:
  - `Create<Entity>Request.cs` — request DTO (no `Id`, no server-owned fields); trim and validate it in the endpoint handler, as `ItemsEndpoints.CreateItem` does;
  - `<Feature>Service.cs` — async methods taking `CancellationToken`, logging via `ILogger<T>`;
  - `<Feature>Endpoints.cs` — `Map<Feature>Endpoints` extension using `MapGroup("/api/<feature>")`, `{id:int}` route constraints, `Results`, `ValidationProblem` on invalid input.
- Register the service and map endpoints in `Program.cs`.
- Tests in `tests/CryptApp.Tests/Features/<Feature>Tests.cs` using the existing fixture: happy path, validation failure (400), not found (404).
- Then create a migration: `dotnet tool restore` (once), then `dotnet ef migrations add Add<Entity> --project src/CryptApp.Web`.

Finish only when `dotnet build CryptApp.slnx` and `dotnet test CryptApp.slnx` are green. Spooky copy belongs in UI text only, never in identifiers.
