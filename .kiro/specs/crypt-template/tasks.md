# Crypt Template — Implementation Tasks

## Task Checklist

### Phase 1: Template Scaffolding and Project Structure

**[1.1]** Create template metadata file  
*Requirement Ref:* US-012  
*Description:* Create `.template.config/template.json` with template name, description, identity, symbols (parameters), and language settings.  
*Verification:* `dotnet new --search crypt` shows the template; metadata is correct.

**[1.2]** Create web project structure  
*Requirement Ref:* US-001, US-002  
*Description:* Generate directory structure under `src/<ProjectName>.Web/` with subdirectories: `Components/`, `Features/`, `Data/`, `wwwroot/`, `App_Data/`.  
*Verification:* Directory tree matches design; no extraneous files remain.

**[1.3]** Create web project file (.csproj)  
*Requirement Ref:* US-002, US-003  
*Description:* Create `src/<ProjectName>.Web/<ProjectName>.Web.csproj` with:
- `<TargetFramework>net10.0</TargetFramework>`
- `<Nullable>enable</Nullable>`
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
- NuGet dependencies: ASP.NET Core, Blazor, EF Core 10, logging.  
*Verification:* Project file is valid; `dotnet build` succeeds with no errors.

**[1.4]** Create test project structure  
*Requirement Ref:* US-001, US-006  
*Description:* Generate directory structure under `tests/<ProjectName>.Tests/` with subdirectories: `Fixtures/`.  
*Verification:* Directory tree matches design; no extraneous files remain.

**[1.5]** Create test project file (.csproj)  
*Requirement Ref:* US-006  
*Description:* Create `tests/<ProjectName>.Tests/<ProjectName>.Tests.csproj` with:
- NuGet dependencies: xUnit, `Microsoft.AspNetCore.Mvc.Testing`, EF Core testing tools.  
*Verification:* Project file is valid; `dotnet test --collect:"XPlat Code Coverage"` runs.

**[1.6]** Create solution file (.slnx)  
*Requirement Ref:* US-001  
*Description:* Generate `<ProjectName>.slnx` referencing web and test projects.  
*Verification:* Solution opens in Visual Studio; both projects load.

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

**[2.2]** Create App.razor (Blazor root)  
*Requirement Ref:* US-001  
*Description:* Create `Components/App.razor` with:
- Blazor Web App component (SSR mode)
- HeadOutlet for styling and metadata
- Routes component for page routing
- Sensible defaults (interactive server where needed)  
*Verification:* App renders in browser; layout is clean.

**[2.3]** Create Routes.razor  
*Requirement Ref:* US-001  
*Description:* Create `Components/Routes.razor` with:
- @RouteTable directive
- Found and NotFound handlers
- Default layout reference  
*Verification:* Page routing works; 404 handled gracefully.

**[2.4]** Create layout component  
*Requirement Ref:* US-001  
*Description:* Create `Components/Layout/MainLayout.razor` with:
- Dark theme CSS applied
- Navigation menu
- WCAG AA accessible colors and contrast
- Responsive grid layout  
*Verification:* Layout renders; no accessibility warnings from automated tools (WAVE, axe).

**[2.5]** Create index page  
*Requirement Ref:* US-001  
*Description:* Create `Components/Pages/Index.razor` with:
- Welcome message mentioning project name (from template parameter)
- Links to documentation
- Example Blazor component usage  
*Verification:* Page renders; template parameter substitution is correct.

**[2.6]** Create Health endpoint  
*Requirement Ref:* US-004, US-005  
*Description:* Add minimal API endpoint in `Program.cs`:
- Route: `GET /api/health`
- Response: JSON with status, timestamp, app version
- Async with `CancellationToken` parameter
- Logging on each call  
*Verification:* `curl http://localhost:5000/api/health` returns 200 with valid JSON.

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

**[3.2]** Create initial EF Core migration  
*Requirement Ref:* US-003  
*Description:* Create initial migration `Migrations/00001_Initial.cs` via `dotnet ef migrations add Initial` or manually.  
*Verification:* Migration file is generated; applying it creates tables without errors.

**[3.3]** Create example entity models  
*Requirement Ref:* US-003  
*Description:* Create `Data/Models/` directory with example entities:
- Minimal example: `Item` class with id, name, description
- Configuration via OnModelCreating or Fluent API  
*Verification:* Entity maps to table correctly; migration generates valid SQL.

**[3.4]** Configure SQLite for local development  
*Requirement Ref:* US-003  
*Description:* In `Program.cs` and `ApplicationDbContext.cs`:
- SQLite connection string uses `App_Data/app.db`
- Database auto-creates on first run
- Logging level set to debug for queries (optional)  
*Verification:* First run creates `App_Data/app.db`; queries execute successfully.

**[3.5]** Create in-memory test database setup  
*Requirement Ref:* US-006  
*Description:* In `tests/<ProjectName>.Tests/Fixtures/TestDatabaseFixture.cs`:
- Custom `WebApplicationFactory<Program>` override
- Use in-memory SQLite for tests
- Database seeding for test data
- IAsyncLifetime for setup/teardown  
*Verification:* `dotnet test` uses in-memory DB; test isolation verified (each test has clean DB).

---

### Phase 4: Feature and Service Pattern

**[4.1]** Create example feature folder structure  
*Requirement Ref:* US-001, US-004  
*Description:* Create `Features/Items/` subdirectory as template example for features.  
*Verification:* Folder structure is clean; no unnecessary files.

**[4.2]** Create example service  
*Requirement Ref:* US-004, US-007  
*Description:* Create `Features/Items/ItemsService.cs` with:
- Constructor injection of `ApplicationDbContext`, `ILogger<ItemsService>`
- Example method: `GetItemsAsync(CancellationToken cancellationToken)`
- Async/await with `CancellationToken` throughout
- Error logging and exception handling  
*Verification:* Service compiles; can be injected into endpoints.

**[4.3]** Create example endpoint group  
*Requirement Ref:* US-004, US-007  
*Description:* Create `Features/Items/ItemsEndpoints.cs` with:
- Minimal API endpoint mappings for Items (GET, POST, DELETE)
- Constructor injection of service, logger, db context
- Async handlers with `CancellationToken`
- Logging at request and response  
*Verification:* Endpoints registered in `Program.cs`; curl requests work.

**[4.4]** Register feature service in DI  
*Requirement Ref:* US-007  
*Description:* In `Program.cs`, register `ItemsService` and map `ItemsEndpoints`.  
*Verification:* `dotnet run` starts; endpoints are accessible.

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

**[5.2]** Create example integration test  
*Requirement Ref:* US-006  
*Description:* Create `tests/<ProjectName>.Tests/Features/ItemsTests.cs` with:
- Uses test fixture (factory pattern)
- Tests `GET /api/health` endpoint
- Tests example feature endpoint (e.g., `GET /api/items`)
- Uses real in-memory database, no mocking  
*Verification:* `dotnet test` runs and passes; assertions are meaningful.

**[5.3]** Create xUnit trait definitions (optional)  
*Requirement Ref:* US-006  
*Description:* Create `tests/<ProjectName>.Tests/Traits.cs` to categorize tests (integration, unit, slow).  
*Verification:* Tests can be filtered by trait: `dotnet test --filter "Category=Integration"`.

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

**[6.2]** Create docker-compose.yml  
*Requirement Ref:* US-008  
*Description:* Create `docker-compose.yml` with:
- **web** service: builds from Dockerfile, exposes port 5000→8080, volumes for SQLite
- **postgres** service (optional): PostgreSQL image, environment variables for user/password/db
- Network configuration
- Comments documenting how to enable PostgreSQL  
*Verification:* `docker-compose up` builds and starts; app is accessible at `http://localhost:5000`.

**[6.3]** Create .dockerignore  
*Requirement Ref:* US-008  
*Description:* Create `.dockerignore` to exclude build artifacts, .git, node_modules, etc.  
*Verification:* Docker build layer sizes are optimized; `docker history myapp` shows reasonable sizes.

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

**[7.2]** Create steering/structure.md  
*Requirement Ref:* US-009  
*Description:* Create `.kiro/steering/structure.md` with:
- Generated project structure tree
- Feature folder explanation (`Features/<Name>/`)
- File organization within features
- Build and test commands  
*Verification:* File is readable; path examples match generated structure.

**[7.3]** Create steering/product.md  
*Requirement Ref:* US-009  
*Description:* Create `.kiro/steering/product.md` with:
- Project purpose (from template parameters)
- Halloween theme guidance (tasteful, dark UI, accessible)
- Naming conventions (spooky copy in UI only, never code identifiers)
- Example app names  
*Verification:* File is readable; project-specific values are substituted.

**[7.4]** Create build-on-save hook  
*Requirement Ref:* US-009  
*Description:* Create `.kiro/hooks/build-on-save.json`:
- Trigger: `PostFileSave`
- Matcher: `\.(cs|csproj|razor)$`
- Action: `dotnet build`  
*Verification:* Hook JSON is valid; Kiro recognizes the hook file.

**[7.5]** Create test-on-save hook  
*Requirement Ref:* US-009  
*Description:* Create `.kiro/hooks/test-on-save.json`:
- Trigger: `PostFileSave`
- Matcher: `.*\.Tests\.cs$`
- Action: `dotnet test --no-build`  
*Verification:* Hook JSON is valid; Kiro recognizes the hook file.

**[7.6]** Create migration-agent.md  
*Requirement Ref:* US-009  
*Description:* Create `.kiro/agents/migration-agent.md` with:
- Purpose and responsibilities (EF Core migrations)
- Example commands (`/agent-migration create`, `status`, `revert`)
- Instructions for developers  
*Verification:* Agent documentation is clear; developers understand how to use it.

**[7.7]** Create feature-scaffold-agent.md  
*Requirement Ref:* US-009  
*Description:* Create `.kiro/agents/feature-scaffold-agent.md` with:
- Purpose and responsibilities (feature generation)
- Example commands (`/agent-scaffold Feature <Name> <Properties>`)
- Templates or placeholders for feature files  
*Verification:* Agent documentation is clear and actionable.

---

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

**[8.2]** Create .gitignore  
*Requirement Ref:* US-001  
*Description:* Create `.gitignore` with entries for:
- Build artifacts (`bin/`, `obj/`)
- IDE files (`.vs/`, `.vscode/settings.json` optional)
- Database files (`App_Data/`, `*.db`)
- Dependencies (`packages/`)
- Logs and temp files  
*Verification:* File is valid; `git status` excludes correct files.

**[8.3]** Create launchSettings.json (optional)  
*Requirement Ref:* US-001  
*Description:* Create `src/<ProjectName>.Web/Properties/launchSettings.json` with:
- IIS Express profile (optional)
- dotnet CLI profile with correct port and environment  
*Verification:* `dotnet run` and debugging in VS Code work correctly.

---

### Phase 9: Verification and Quality Assurance

**[9.1]** Build verification  
*Requirement Ref:* US-002, US-003, US-006  
*Description:* Run `dotnet build` from template output directory. Verify:
- No compilation errors
- No warnings (due to `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`)
- Build succeeds in <30 seconds  
*Verification:* Build output shows zero errors.

**[9.2]** Test verification  
*Requirement Ref:* US-006  
*Description:* Run `dotnet test` from template output directory. Verify:
- All tests pass (health endpoint, example features)
- Coverage report generated (optional)
- No skipped or inconclusive tests  
*Verification:* Test output shows "X passed, 0 failed".

**[9.3]** Docker verification  
*Requirement Ref:* US-008  
*Description:* Run:
- `docker build -t crypt-test .`
- `docker run -p 5000:8080 crypt-test`
- Verify app is accessible at `http://localhost:5000`
- Verify `/api/health` responds with 200  
*Verification:* Container builds and app responds.

**[9.4]** Template instantiation verification  
*Requirement Ref:* US-012  
*Description:* Test template parameter substitution:
- Run `dotnet new crypt -n TestApp --desc "Test Description"`
- Verify project name is "TestApp" in namespaces and folders
- Verify description appears in README and docker-compose
- Verify app builds and runs  
*Verification:* Generated project is correct; build succeeds.

**[9.5]** Code analysis and linting (optional)  
*Requirement Ref:* US-002, US-010  
*Description:* Run:
- `dotnet format` to check/apply code formatting
- Static analysis tools (optional: Roslyn analyzers, StyleCop)  
*Verification:* Code follows conventions; no style violations.

**[9.6]** Accessibility verification  
*Requirement Ref:* US-001, US-008  
*Description:* Test generated UI for WCAG AA compliance:
- Use browser DevTools (Lighthouse) or WAVE to check contrast, colors, structure
- Verify dark theme is readable and accessible  
*Verification:* No contrast or accessibility warnings.

---

### Phase 10: Finalization

**[10.1]** Create template.json with all parameters and symbols  
*Requirement Ref:* US-012  
*Description:* Update `.template.config/template.json` with:
- All template parameters (name, title, desc, author, etc.)
- Symbol definitions with defaults and replacements
- Post-action scripts (optional: run dotnet restore, dotnet build)  
*Verification:* `dotnet new crypt --help` shows all parameters; `dotnet new crypt -n X` works correctly.

**[10.2]** Add template to local NuGet  
*Requirement Ref:* US-012  
*Description:* Package template and install locally:
- `dotnet new install ./template/`
- Verify via `dotnet new --search crypt`  
*Verification:* Template appears in list; can be instantiated.

**[10.3]** Final integration test  
*Requirement Ref:* US-001 through US-012  
*Description:* End-to-end test:
1. `dotnet new crypt -n MyAwesomeApp --desc "My Test App"`
2. `cd MyAwesomeApp && dotnet build`
3. `dotnet test`
4. `dotnet run` (verify app starts)
5. `docker-compose up` (verify Docker works)
6. Open in Kiro IDE (verify `.kiro/` hooks and agents work)  
*Verification:* All steps succeed without manual intervention.

---

## Acceptance Criteria Summary

- [ ] Template creates buildable, testable, runnable project from `dotnet new crypt -n <Name>`
- [ ] Generated project follows all tech stack conventions (nullable types, warnings as errors, async, logging)
- [ ] EF Core 10 configured with SQLite default and PostgreSQL optional support
- [ ] Blazor SSR + minimal API endpoints working
- [ ] xUnit tests with in-memory EF Core passing
- [ ] Multi-stage Docker build produces non-root image
- [ ] `.kiro/` folder with steering, hooks, and agents ready for use
- [ ] README is clear and complete; app runs locally within 5 minutes of following instructions
- [ ] No build warnings; code follows conventions
- [ ] WCAG AA accessibility verified for generated UI
