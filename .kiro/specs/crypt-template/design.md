# Crypt Template — Design

## Overview
Crypt is a `dotnet new` template that generates a production-ready, Kiro-ready .NET 10 web application skeleton. The template embodies clean architecture principles, modern C# safety features, and integrated Kiro tooling to accelerate development of small business web apps.

## Architecture and Components

### Template Structure
The Crypt template creates the following structure when instantiated:

```
<ProjectName>/
├── src/
│   └── <ProjectName>.Web/
│       ├── <ProjectName>.Web.csproj          # Main web project
│       ├── Program.cs                         # DI setup, middleware, endpoints
│       ├── App.razor                          # Blazor app root
│       ├── Routes.razor                       # Blazor routing
│       ├── Components/                        # Blazor components (pages, layouts, shared)
│       │   ├── Layout/                        # App shell, theme
│       │   ├── Pages/                         # Blazor pages (@page routes)
│       │   └── ...
│       ├── Features/                          # Domain features
│       │   └── <Feature>/
│       │       ├── <Feature>Service.cs        # Business logic
│       │       ├── <Feature>Endpoints.cs      # Minimal API endpoints
│       │       └── ...
│       ├── Data/                              # EF Core context and migrations
│       │   ├── ApplicationDbContext.cs
│       │   └── Migrations/
│       ├── App_Data/                          # SQLite database file (dev/test)
│       └── wwwroot/                           # Static assets, CSS, theme
├── tests/
│   └── <ProjectName>.Tests/
│       ├── <ProjectName>.Tests.csproj         # Test project
│       ├── <Feature>Tests.cs                  # Feature tests
│       └── Fixtures/                          # Test fixtures, in-memory DB setup
├── <ProjectName>.slnx                         # Solution file
├── Dockerfile                                 # Multi-stage container build
├── docker-compose.yml                         # Local dev orchestration
├── README.md                                  # Project-specific setup guide
└── .kiro/                                     # Kiro integration
    ├── steering/
    │   ├── tech.md                            # Tech stack & conventions
    │   ├── structure.md                       # Project structure guide
    │   └── product.md                         # Product identity & naming
    ├── hooks/
    │   ├── build-on-save.json                 # Auto-build on file save
    │   ├── test-on-save.json                  # Auto-test on file save
    │   └── migration-check.json                # EF migration validation
    └── agents/
        ├── migration-agent.md                  # Custom agent for EF migrations
        └── feature-scaffold.md                 # Custom agent for feature generation
```

### Key Components

#### 1. Web Project (`<ProjectName>.Web.csproj`)
**Purpose:** Main ASP.NET Core application  
**Technology:**
- .NET 10, C# latest with nullable reference types enabled
- Blazor Web App (static SSR + interactive server for specific components)
- Minimal APIs under `/api` for data endpoints
- EF Core 10 for data access

**Features:**
- `Program.cs`: Configures DI, middleware, authentication (if needed), API endpoints
- `App.razor`: Blazor app shell with layout and theme
- `Routes.razor`: Client-side routing configuration
- `Components/`: Blazor components organized by function
- `Features/`: Domain-driven feature folders containing services and endpoints
- `Data/ApplicationDbContext.cs`: EF Core DbContext
- `wwwroot/`: Static assets with dark theme CSS (WCAG AA accessible)

#### 2. Test Project (`<ProjectName>.Tests.csproj`)
**Purpose:** Integration and unit tests  
**Technology:**
- xUnit test framework
- `Microsoft.AspNetCore.Mvc.Testing` for HTTP-level tests
- EF Core in-memory SQLite (not mocked, real database queries)

**Features:**
- `Fixtures/`: Custom `WebApplicationFactory<Program>` with in-memory DB
- Test classes mirror feature structure
- No mocking of EF — tests use real in-memory database

#### 3. Data Layer (`ApplicationDbContext.cs`)
**Purpose:** EF Core entity mapping and database configuration  
**Features:**
- DbSets for each domain entity
- Fluent API configuration for relationships and constraints
- Migrations folder with version control
- SQLite for local development; PostgreSQL support via docker-compose

#### 4. Minimal API Endpoints
**Purpose:** RESTful data access and operations  
**Example Endpoints:**
- `GET /api/health` — Health check
- Feature-specific endpoints (e.g., `GET /api/items`, `POST /api/rentals`)

**Conventions:**
- All endpoints are `async`
- All endpoints accept `CancellationToken` parameter
- Endpoints use constructor injection of services and `ILogger<T>`
- Logging at key points (request start, errors, completion)

#### 5. Dockerfile (Multi-stage)
**Stages:**
1. **Restore:** Download and cache NuGet dependencies
2. **Build:** Compile application and tests
3. **Runtime:** Minimal image with only runtime and app binaries

**Security:**
- Non-root user runs the application
- Multi-stage to minimize final image size
- .NET runtime image based on official Microsoft images

#### 6. docker-compose.yml
**Services:**
- **web:** ASP.NET Core application container
- **postgres:** PostgreSQL database (optional, for production-like local development)

**Configuration:**
- Network setup for service-to-service communication
- Volume mounts for SQLite (dev) and PostgreSQL data (optional)
- Environment variables for connection strings

---

## Data Flow

### Request Handling (Blazor SSR + Minimal API)
```
1. Browser request
   ↓
2. ASP.NET Core middleware (logging, auth, etc.)
   ↓
3. Route matching (Blazor SSR pages vs /api endpoints)
   ↓
4a. Blazor SSR: Render component → update DOM
4b. Minimal API: Call endpoint → service → EF query → response
   ↓
5. Response sent to browser
```

### Feature Service Pattern
```
Blazor Component/Endpoint
   ↓
Feature Service (business logic, logging, errors)
   ↓
ApplicationDbContext (EF Core query)
   ↓
SQLite database / PostgreSQL
```

---

## Template Parameters

The template accepts the following parameters when invoked via `dotnet new crypt`:

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `name` / `-n` | string | Yes | — | Project name (used for folders, namespaces, .csproj files) |
| `title` / `-t` | string | No | `{name}` | Friendly project title (used in README, UI) |
| `desc` / `-d` | string | No | `A Crypt-generated app` | Short description for README and docker-compose |
| `author` | string | No | "Your Name" | Author name for .csproj metadata |
| `usePostgres` / `-pg` | bool | No | `false` | Include PostgreSQL in docker-compose.yml |
| `skipGit` | bool | No | `false` | Skip .gitignore and git initialization |

### Parameter Substitution
- `$safeprojectname$` → project name (lowercase, alphanumeric)
- `$projectname$` → project name as provided
- `$title$` → friendly title
- `$description$` → description
- `$author$` → author name

---

## Kiro Integration

### .kiro/steering/

#### tech.md
Instructs developers on:
- .NET 10, C# latest, nullable reference types, warnings as errors
- ASP.NET Core + Blazor Web App architecture
- EF Core 10 with SQLite default, PostgreSQL option
- Clean architecture (features, one web + one test project)
- Async/await with `CancellationToken` throughout
- Logging via `ILogger<T>` and Options pattern configuration
- Testing with xUnit and in-memory EF
- Docker requirements (multi-stage, non-root)

#### structure.md
Defines:
- Generated project structure (src, tests, root files)
- Feature folder organization (`Features/<Name>/`)
- Build validation commands (`dotnet build`, `dotnet test`)

#### product.md
Communicates:
- Crypt purpose: template for small business web apps
- Halloween theme (tasteful, dark UI, accessible)
- Naming conventions (spooky copy in UI only, never in code identifiers)
- Generated app names (haunted-inventory, seance-scheduler as examples)

### .kiro/hooks/

#### build-on-save.json
- **Trigger:** `PostFileSave`
- **Matcher:** `\.(cs|razor|csproj)$`
- **Action:** `dotnet build`
- **Purpose:** Compile on every C#/Razor file save to catch errors early

#### test-on-save.json
- **Trigger:** `PostFileSave`
- **Matcher:** `.*\.Tests\.cs$`
- **Action:** `dotnet test --no-build` (after build hook completes)
- **Purpose:** Run tests automatically when test files are saved

#### migration-check.json
- **Trigger:** `PostFileSave`
- **Matcher:** `.*\.cs$` (EF-related files)
- **Action:** Validate pending migrations exist if model changes detected
- **Purpose:** Remind developers to create migrations when DbContext changes

### .kiro/agents/

#### migration-agent.md
**Purpose:** Custom agent for EF Core database migrations  
**Responsibilities:**
- Scaffold new migrations from DbContext changes
- Generate migration SQL for review
- Apply/revert migrations
- Provide migration history and status

**Example Invocations:**
```
/agent-migration create "Add rental overdue flag"
/agent-migration status
/agent-migration revert
```

#### feature-scaffold.md
**Purpose:** Custom agent for feature generation  
**Responsibilities:**
- Generate service, endpoints, tests, and DbSets for a new feature
- Use templates from `.kiro/templates/`
- Follow naming and structure conventions

**Example Invocations:**
```
/agent-scaffold Feature "Items" "String Name, int Quantity"
```

---

## Implementation Philosophy

### Clean Architecture (Without Over-Engineering)
- **One web project** + **one test project** per app
- Features grouped by domain (`Features/`), not by technical layer (no separate Repository, Service, Controller layers)
- Each feature is self-contained: service logic, endpoints, tests, DbSet configuration

### Async First
- Every async method uses `async`/`await`
- Every endpoint and EF call accepts `CancellationToken`
- Cancellation propagated through the entire call stack

### Logging and Observability
- `ILogger<T>` injected and used throughout
- Logs at key decision points and errors
- No console.WriteLine; all logs go through the logging framework

### Configuration via Options Pattern
- Configuration validated at application startup
- Early failure principle: bad config fails immediately, not at request time

### Testing Strategy
- No mocking of EF — in-memory SQLite for realistic queries
- HTTP-level integration tests via `WebApplicationFactory<Program>`
- Tests are fast and reliable

### Security and Performance
- Minimal attack surface: no over-engineered endpoints
- Multi-stage Docker builds for minimal image size
- Non-root containers for security
- Prepared statements and parameterized queries (via EF Core)

---

## Success Criteria for Template

1. **Buildability:** New project generated from template builds with no errors or warnings
2. **Testability:** New project's test suite runs with 100% pass rate
3. **Debuggability:** Developer can set breakpoints and debug in Visual Studio/VS Code
4. **Scaffolding:** Template parameters substitute correctly and project runs
5. **Docker:** `docker-compose up` builds and runs the app
6. **Kiro Ready:** `.kiro/` hooks and agents work without manual configuration
7. **Accessibility:** Generated UI components meet WCAG AA contrast standards
8. **Documentation:** README is clear and complete; developers can get to "app running" in <5 minutes
