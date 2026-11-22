---
inclusion: always
---
# Tech stack and conventions

- .NET 10, C# latest, `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.
- ASP.NET Core with Blazor Web App (static SSR + interactive server where needed) and minimal APIs under `/api`.
- EF Core 10 with SQLite by default (file in `App_Data/`), PostgreSQL optional via `docker-compose.yml`.
- Clean architecture without over-engineering: one web project + one test project per app. Features grouped by folder
  (`Features/<Name>/`), not by technical layer.
- async all the way, `CancellationToken` on every endpoint and EF call.
- Logging via `ILogger<T>`, configuration via the Options pattern with validation on start.
- Tests: xUnit, `Microsoft.AspNetCore.Mvc.Testing` for HTTP-level tests; no mocking of EF — use SQLite in-memory.
- `/health` endpoint, Dockerfile (multi-stage, non-root), `docker-compose.yml`.
- Code, comments, commit messages: English.
