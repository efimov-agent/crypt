# Crypt Template — Implementation Tasks

## Task Checklist

### Phase 1: Template Scaffolding and Project Structure

**[1.1]** Create template metadata file  
*Requirement Ref:* US-012  
*Description:* Create `.template.config/template.json` with template name, description, identity, symbols (parameters), and language settings.  
*Verification:* `dotnet new --search crypt` shows the template; metadata is correct.
*Status:* [x] Completed

**[1.2]** Create web project structure  
*Requirement Ref:* US-001, US-002  
*Description:* Generate directory structure under `src/<ProjectName>.Web/` with subdirectories: `Components/`, `Features/`, `Data/`, `wwwroot/`, `App_Data/`.  
*Verification:* Directory tree matches design; no extraneous files remain.
*Status:* [x] Completed

**[1.3]** Create web project file (.csproj)  
*Requirement Ref:* US-002, US-003  
*Description:* Create `src/<ProjectName>.Web/<ProjectName>.Web.csproj` with:
- `<TargetFramework>net10.0</TargetFramework>`
- `<Nullable>enable</Nullable>`
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
- NuGet dependencies: ASP.NET Core, Blazor, EF Core 10, logging.  
*Verification:* Project file is valid; `dotnet build` succeeds with no errors.
*Status:* [x] Completed

**[1.4]** Create test project structure  
*Requirement Ref:* US-001, US-006  
*Description:* Generate directory structure under `tests/<ProjectName>.Tests/` with subdirectories: `Fixtures/`.  
*Verification:* Directory tree matches design; no extraneous files remain.
*Status:* [x] Completed

**[1.5]** Create test project file (.csproj)  
*Requirement Ref:* US-006  
*Description:* Create `tests/<ProjectName>.Tests/<ProjectName>.Tests.csproj` with:
- NuGet dependencies: xUnit, `Microsoft.AspNetCore.Mvc.Testing`, EF Core testing tools.  
*Verification:* Project file is valid; `dotnet test --collect:"XPlat Code Coverage"` runs.
*Status:* [x] Completed

**[1.6]** Create solution file (.slnx)  
*Requirement Ref:* US-001  
*Description:* Generate `<ProjectName>.slnx` referencing web and test projects.  
*Verification:* Solution opens in Visual Studio; both projects load.
*Status:* [x] Completed

---

### Phase 2: ASP.NET Core and Blazor Setup

**[2.1]** Create Program.cs with DI setup  
*Requirement Ref:* US-002, US-003, US-004, US-007  
*Description:* Implement `Program.cs` with:
- Service registration (EF Core, logging, configuration)
- Middleware pipeline (logging, exception handling, etc.)
- Blazor app configuration
- API endpoint group setup under `/api`
- Configuration validation on startup  
*Verification:* `dotnet run` starts without errors; application is responsive.
*Status:* [x] Completed

**[2.2]** Create App.razor (Blazor root)  
*Requirement Ref:* US-001  
*Description:* Create `Components/App.razor` with:
- Blazor Web App component (SSR mode)
- HeadOutlet for styling and metadata
- Routes component for page routing
- Sensible defaults (interactive server where needed)  
*Verification:* App renders in browser; layout is clean.
*Status:* [x] Completed

**[2.3]** Create Routes.razor  
*Requirement Ref:* US-001  
*Description:* Create `Components/Routes.razor` with:
- @RouteTable directive
- Found and NotFound handlers
- Default layout reference  
*Verification:* Page routing works; 404 handled gracefully.
*Status:* [x] Completed (simplified approach for .NET 10)

**[2.4]** Create layout component  
*Requirement Ref:* US-001  
*Description:* Create `Components/Layout/MainLayout.razor` with:
- Dark theme CSS applied
- Navigation menu
- WCAG AA accessible colors and contrast
- Responsive grid layout  
*Verification:* Layout renders; no accessibility warnings from automated tools (WAVE, axe).
*Status:* [x] Completed

**[2.5]** Create index page  
*Requirement Ref:* US-001  
*Description:* Create `Components/Pages/Index.razor` with:
- Welcome message mentioning project name (from template parameter)
- Links to documentation
- Example Blazor component usage  
*Verification:* Page renders; template parameter substitution is correct.
*Status:* [x] Completed

**[2.6]** Create Health endpoint  
*Requirement Ref:* US-004, US-005  
*Description:* Add minimal API endpoint in `Program.cs`:
- Route: `GET /api/health`
- Response: JSON with status, timestamp, app version
- Async with `CancellationToken` parameter
- Logging on each call  
*Verification:* `curl http://localhost:5000/api/health` returns 200 with valid JSON.
*Status:* [x] Completed

---

### Phase 3: Entity Framework Core and Data Layer

**[3.1]** Create ApplicationDbContext  
*Requirement Ref:* US-003  
*Description:* Create `Data/ApplicationDbContext.cs` with:
- DbContext subclass inheriting from EF Core DbContext
- Example DbSet (e.g., `DbSet<Item>` for haunted-inventory)
- OnConfiguring: SQLite default, PostgreSQL support via options
- OnModelCreating: Fluent API configuration, indexes, constraints  
*Verification:* `dotnet ef dbcontext info` shows context info; no compile errors.
*Status:* [x] Completed

**[3.2]** Create initial EF Core migration  
*Requirement Ref:* US-003  
*Description:* Create initial migration `Migrations/00001_Initial.cs` via `dotnet ef migrations add Initial` or manually.  
*Verification:* Migration file is generated; applying it creates tables without errors.
*Status:* [x] Completed

**[3.3]** Create example entity models  
*Requirement Ref:* US-003  
*Description:* Create `Data/Models/` directory with example entities:
- Minimal example: `Item` class with id, name, description
- Configuration via OnModelCreating or Fluent API  
*Verification:* Entity maps to table correctly; migration generates valid SQL.
*Status:* [x] Completed

**[3.4]** Configure SQLite for local development  
*Requirement Ref:* US-003  
*Description:* In `Program.cs` and `ApplicationDbContext.cs`:
- SQLite connection string uses `App_Data/app.db`
- Database auto-creates on first run
- Logging level set to debug for queries (optional)  
*Verification:* First run creates `App_Data/app.db`; queries execute successfully.
*Status:* [x] Completed

**[3.5]** Create in-memory test database setup  
*Requirement Ref:* US-006  
*Description:* In `tests/<ProjectName>.Tests/Fixtures/TestDatabaseFixture.cs`:
- Custom `WebApplicationFactory<Program>` override
- Use in-memory SQLite for tests
- Database seeding for test data
- IAsyncLifetime for setup/teardown  
*Verification:* `dotnet test` uses in-memory DB; test isolation verified (each test has clean DB).
*Status:* [x] Completed

---

### Phase 4: Feature and Service Pattern

**[4.1]** Create example feature folder structure  
*Requirement Ref:* US-001, US-004  
*Description:* Create `Features/Items/` subdirectory as template example for features.  
*Verification:* Folder structure is clean; no unnecessary files.
*Status:* [x] Completed

**[4.2]** Create example service  
*Requirement Ref:* US-004, US-007  
*Description:* Create `Features/Items/ItemsService.cs` with:
- Constructor injection of `ApplicationDbContext`, `ILogger<ItemsService>`
- Example method: `GetItemsAsync(CancellationToken cancellationToken)`
- Async/await with `CancellationToken` throughout
- Error logging and exception handling  
*Verification:* Service compiles; can be injected into endpoints.
*Status:* [x] Completed

**[4.3]** Create example endpoint group  
*Requirement Ref:* US-004, US-007  
*Description:* Create `Features/Items/ItemsEndpoints.cs` with:
- Minimal API endpoint mappings for Items (GET, POST, DELETE)
- Constructor injection of service, logger, db context
- Async handlers with `CancellationToken`
- Logging at request and response  
*Verification:* Endpoints registered in `Program.cs`; curl requests work.
*Status:* [x] Completed

**[4.4]** Register feature service in DI  
*Requirement Ref:* US-007  
*Description:* In `Program.cs`, register `ItemsService` and map `ItemsEndpoints`.  
*Verification:* `dotnet run` starts; endpoints are accessible.
*Status:* [x] Completed

---

### Phase 5: Testing Setup

**[5.1]** Create test database fixture  
*Requirement Ref:* US-006  
*Description:* Create `tests/<ProjectName>.Tests/Fixtures/WebApplicationFactory.cs`:
- Extends `WebApplicationFactory<Program>`
- Overrides `CreateClient()` and `ConfigureWebHost()`
- Configures in-memory SQLite
- Provides seed data  
*Verification:* Fixture is usable; multiple tests can inherit from it.
*Status:* [x] Completed

**[5.2]** Create example integration test  
*Requirement Ref:* US-006  
*Description:* Create `tests/<ProjectName>.Tests/Features/ItemsTests.cs` with:
- Uses test fixture (factory pattern)
- Tests `GET /api/health` endpoint
- Tests example feature endpoint (e.g., `GET /api/items`)
- Uses real in-memory database, no mocking  
*Verification:* `dotnet test` runs and passes; assertions are meaningful.
*Status:* [x] Completed

**[5.3]** Create xUnit trait definitions (optional)  
*Requirement Ref:* US-006  
*Description:* Create `tests/<ProjectName>.Tests/Traits.cs` to categorize tests (integration, unit, slow).  
*Verification:* Tests can be filtered by trait: `dotnet test --filter "Category=Integration"`.
*Status:* [ ] Not implemented (optional)

---

### Phase 6: Docker Support

**[6.1]** Create multi-stage Dockerfile  
*Requirement Ref:* US-008  
*Description:* Create `Dockerfile` with:
- Stage 1 (restore): `FROM mcr.microsoft.com/dotnet/sdk:10.0` + `dotnet restore`
- Stage 2 (build): Build app and tests
- Stage 3 (runtime): `FROM mcr.microsoft.com/dotnet/aspnet:10.0` + app binaries
- Non-root user (create `app` user)
- Expose port 8080
- Entry point to run the web app  
*Verification:* `docker build -t myapp .` succeeds; image runs and app is accessible.
*Status:* [x] Completed

**[6.2]** Create docker-compose.yml  
*Requirement Ref:* US-008  
*Description:* Create `docker-compose.yml` with:
- **web** service: builds from Dockerfile, exposes port 5000→8080, volumes for SQLite
- **postgres** service (optional): PostgreSQL image, environment variables for user/password/db
- Network configuration
- Comments documenting how to enable PostgreSQL  
*Verification:* `docker-compose up` builds and starts; app is accessible at `http://localhost:5000`.
*Status:* [x] Completed

**[6.3]** Create .dockerignore  
*Requirement Ref:* US-008  
*Description:* Create `.dockerignore` to exclude build artifacts, .git, node_modules, etc.  
*Verification:* Docker build layer sizes are optimized; `docker history myapp` shows reasonable sizes.
*Status:* [x] Completed

---

### Phase 7: Kiro Integration

**[7.1]** Create steering/tech.md  
*Requirement Ref:* US-009  
*Description:* Create `.kiro/steering/tech.md` with sections on:
- .NET 10, C#, nullable types, warnings as errors
- ASP.NET Core + Blazor architecture
- EF Core + SQLite/PostgreSQL
- Clean architecture principles
- Async/await with `CancellationToken`
- Logging and Options pattern
- Testing strategy
- Docker and containerization  
*Verification:* File is readable; Kiro loads it on IDE start.
*Status:* [x] Completed

**[7.2]** Create steering/structure.md  
*Requirement Ref:* US-009  
*Description:* Create `.kiro/steering/structure.md` with:
- Generated project structure tree
- Feature folder explanation (`Features/<Name>/`)
- File organization within features
- Build and test commands  
*Verification:* File is readable; path examples match generated structure.
*Status:* [x] Completed

**[7.3]** Create steering/product.md  
*Requirement Ref:* US-009  
*Description:* Create `.kiro/steering/product.md` with:
- Project purpose (from template parameters)
- Halloween theme guidance (tasteful, dark UI, accessible)
- Naming conventions (spooky copy in UI only, never code identifiers)
- Example app names  
*Verification:* File is readable; project-specific values are substituted.
*Status:* [x] Completed

**[7.4]** Create build-on-save hook  
*Requirement Ref:* US-009  
*Description:* Create `.kiro/hooks/build-on-save.json`:
- Trigger: `PostFileSave`
- Matcher: `\.(cs|csproj|razor)$`
- Action: `dotnet build`  
*Verification:* Hook JSON is valid; Kiro recognizes the hook file.
*Status:* [x] Completed

**[7.5]** Create test-on-save hook  
*Requirement Ref:* US-009  
*Description:* Create `.kiro/hooks/test-on-save.json`:
- Trigger: `PostFileSave`
- Matcher: `.*\.Tests\.cs$`
- Action: `dotnet test --no-build`  
*Verification:* Hook JSON is valid; Kiro recognizes the hook file.
*Status:* [x] Completed

**[7.6]** Create migration-agent.md  
*Requirement Ref:* US-009  
*Description:* Create `.kiro/agents/migration-agent.md` with:
- Purpose and responsibilities (EF Core migrations)
- Example commands (`/agent-migration create`, `status`, `revert`)
- Instructions for developers  
*Verification:* Agent documentation is clear; developers understand how to use it.
*Status:* [x] Completed

**[7.7]** Create feature-scaffold-agent.md  
*Requirement Ref:* US-009  
*Description:* Create `.kiro/agents/feature-scaffold-agent.md` with:
- Purpose and responsibilities (feature generation)
- Example commands (`/agent-scaffold Feature <Name> <Properties>`)
- Templates or placeholders for feature files  
*Verification:* Agent documentation is clear and actionable.
*Status:* [x] Completed

---

> Phase 7 revision (K2): hooks consolidated into `.kiro/hooks/dotnet.json` (Kiro hooks format `v1`: test-on-stop, guard-migrations); agents rewritten as Kiro CLI V3 configs (format checked against the configuration reference; `kiro-cli agent list` loads them) `.kiro/agents/{migrations,feature-scaffold}.json` with prompts in `.kiro/prompts/`; `dotnet-ef` pinned in `.config/dotnet-tools.json`.

### Phase 8: Documentation and README

**[8.1]** Create project README.md  
*Requirement Ref:* US-011  
*Description:* Create `README.md` with sections:
- **Project Title** (uses template parameter `$title$`)
- **Description** (uses template parameter `$description$`)
- **Prerequisites** (.NET 10, Docker optional)
- **Getting Started** (clone/build steps)
- **Local Development** (run, debug, common tasks)
- **Testing** (run tests, test organization)
- **Docker Deployment** (build image, run container, docker-compose)
- **Project Structure** (brief explanation of src/tests/Features)
- **Kiro Integration** (steering, hooks, agents)
- **Contributing** (conventions, branch strategy)  
*Verification:* README is complete, clear, and accurate; following instructions results in running app.
*Status:* [x] Completed

**[8.2]** Create .gitignore  
*Requirement Ref:* US-001  
*Description:* Create `.gitignore` with entries for:
- Build artifacts (`bin/`, `obj/`)
- IDE files (`.vs/`, `.vscode/settings.json` optional)
- Database files (`App_Data/`, `*.db`)
- Dependencies (`packages/`)
- Logs and temp files  
*Verification:* File is valid; `git status` excludes correct files.
*Status:* [x] Completed

**[8.3]** Create launchSettings.json (optional)  
*Requirement Ref:* US-001  
*Description:* Create `src/<ProjectName>.Web/Properties/launchSettings.json` with:
- IIS Express profile (optional)
- dotnet CLI profile with correct port and environment  
*Verification:* `dotnet run` and debugging in VS Code work correctly.
*Status:* [x] Completed

---

### Phase 9: Verification and Quality Assurance

**[9.1]** Build verification  
*Requirement Ref:* US-002, US-003, US-006  
*Description:* Run `dotnet build` from template output directory. Verify:
- No compilation errors
- No warnings (due to `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`)
- Build succeeds in <30 seconds  
*Verification:* Build output shows zero errors.
*Status:* [x] Completed

**[9.2]** Test verification  
*Requirement Ref:* US-006  
*Description:* Run `dotnet test` from template output directory. Verify:
- All tests pass (health endpoint, example features)
- Coverage report generated (optional)
- No skipped or inconclusive tests  
*Verification:* Test output shows "X passed, 0 failed".
*Status:* [x] Completed

**[9.3]** Docker verification  
*Requirement Ref:* US-008  
*Description:* Run:
- `docker build -t crypt-test .`
- `docker run -p 5000:8080 crypt-test`
- Verify app is accessible at `http://localhost:5000`
- Verify `/api/health` responds with 200  
*Verification:* Container builds and app responds.
*Status:* [ ] Pending (requires Docker daemon)

**[9.4]** Template instantiation verification  
*Requirement Ref:* US-012  
*Description:* Test template parameter substitution:
- Run `dotnet new crypt -n TestApp --description "Test Description"`
- Verify project name is "TestApp" in namespaces and folders
- Verify description appears in README and docker-compose
- Verify app builds and runs  
*Verification:* Generated project is correct; build succeeds.
*Status:* [ ] Pending

**[9.5]** Code analysis and linting (optional)  
*Requirement Ref:* US-002, US-010  
*Description:* Run:
- `dotnet format` to check/apply code formatting
- Static analysis tools (optional: Roslyn analyzers, StyleCop)  
*Verification:* Code follows conventions; no style violations.
*Status:* [ ] Not implemented (optional)

**[9.6]** Accessibility verification  
*Requirement Ref:* US-001, US-008  
*Description:* Test generated UI for WCAG AA compliance:
- Use browser DevTools (Lighthouse) or WAVE to check contrast, colors, structure
- Verify dark theme is readable and accessible  
*Verification:* No contrast or accessibility warnings.
*Status:* [ ] Pending (requires browser testing)

---

### Phase 10: Finalization

**[10.1]** Create template.json with all parameters and symbols  
*Requirement Ref:* US-012  
*Description:* Update `.template.config/template.json` with:
- All template parameters (name, title, description)
- Symbol definitions with defaults and replacements
- Post-action scripts (optional: run dotnet restore, dotnet build)  
*Verification:* `dotnet new crypt --help` shows all parameters; `dotnet new crypt -n X` works correctly.
*Status:* [x] Completed

**[10.2]** Add template to local NuGet  
*Requirement Ref:* US-012  
*Description:* Package template and install locally:
- `dotnet pack Crypt.Templates.csproj -o ./artifacts` → `Crypt.Templates.<ver>.nupkg`
- `dotnet new install ./artifacts/Crypt.Templates.<ver>.nupkg` (or `dotnet new install ./template/` for development)
- Verify via `dotnet new list crypt`  
*Verification:* Template appears in list; can be instantiated.
*Status:* [x] Complete

**[10.3]** Final integration test  
*Requirement Ref:* US-001 through US-012  
*Description:* End-to-end test:
1. `dotnet new crypt -n MyAwesomeApp --description "My Test App"`
2. `cd MyAwesomeApp && dotnet build`
3. `dotnet test`
4. `dotnet run` (verify app starts)
5. `docker-compose up` (verify Docker works)
6. Open in Kiro (verify `.kiro/` hooks and agents work) — partial: agents load in kiro-cli (`agent list`), hooks not yet observed firing; to be confirmed while building K3 apps  
*Verification:* All steps succeed without manual intervention.
*Status:* [~] Steps 1–5 done (nupkg install, build, 12 tests, run, compose); step 6 partial

---

## Acceptance Criteria Summary

- [x] Template creates buildable, testable, runnable project from `dotnet new crypt -n <Name>`
- [x] Generated project follows all tech stack conventions (nullable types, warnings as errors, async, logging)
- [x] EF Core 10 configured with SQLite (PostgreSQL switch documented as manual steps)
- [x] Blazor SSR + minimal API endpoints working
- [x] xUnit tests with a temporary SQLite database passing
- [x] Multi-stage Docker build produces non-root image
- [x] `.kiro/` folder with steering, hooks, and agents ready for use
- [x] README is clear and complete; app runs locally within 5 minutes of following instructions
- [x] No build warnings; code follows conventions
- [ ] WCAG AA accessibility verified for generated UI