# Crypt Template — Requirements

## Overview
Crypt is a `dotnet new` template for small business web apps, built for the Kiroween "Skeleton Crew" category. The template ships with its own `.kiro` folder (steering, hooks, custom agents) so a new project starts Kiro-ready on day one.

## User Stories with Acceptance Criteria (EARS)

### US-001: Template Project Structure
**As a** developer generating a new app from the Crypt template  
**I want** the template to create a clean, organized project structure  
**So that** I can start building immediately with industry-standard conventions

**Acceptance Criteria (EARS):**
- **Given** I run `dotnet new crypt -n MyApp` from the command line
- **When** the template generates
- **Then** I see a folder structure with:
  - `src/MyApp.Web/` containing the ASP.NET Core Blazor Web App
  - `tests/MyApp.Tests/` containing the xUnit test project
  - `MyApp.slnx` solution file at the root
  - `Dockerfile` with multi-stage build
  - `docker-compose.yml` for local PostgreSQL support (optional)
  - `README.md` with project-specific setup instructions
- **And** no extraneous or hidden template metadata files are visible to the developer

### US-002: Tech Stack Configuration
**As a** developer using the Crypt template  
**I want** the template to enforce .NET 10, C# latest with nullable reference types enabled and warnings as errors  
**So that** I have modern language safety guarantees from the start

**Acceptance Criteria (EARS):**
- **Given** a new project is generated from the template
- **When** I inspect the project file(s)
- **Then** I see:
  - `<TargetFramework>net10.0</TargetFramework>`
  - `<Nullable>enable</Nullable>`
  - `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
- **And** when I run `dotnet build`, there are no warnings or errors from the base template

### US-003: ASP.NET Core and EF Core Setup
**As a** developer using the Crypt template  
**I want** the template to include ASP.NET Core with Blazor Web App (static SSR + interactive server) and EF Core 10 with SQLite  
**So that** I can persist data with a lightweight, production-ready setup

**Acceptance Criteria (EARS):**
- **Given** a new project is generated from the template
- **When** I inspect the web project
- **Then** I see:
  - Blazor Web App configured with static SSR mode
  - Interactive server components where appropriate
  - EF Core 10 DbContext class
  - SQLite database file configured to use `App_Data/` directory
- **And** the application starts and can query the database without errors

### US-004: Minimal API Endpoints
**As a** a developer using the Crypt template  
**I want** the template to include at least one example minimal API endpoint under `/api`  
**So that** I understand the pattern for building REST endpoints

**Acceptance Criteria (EARS):**
- **Given** a new project is generated from the template
- **When** I inspect the Program.cs file
- **Then** I see:
  - At least one example minimal API endpoint registered (e.g., `GET /api/health`)
  - Endpoints use `async` and accept `CancellationToken`
  - Endpoints follow clean architecture with logging
- **And** the endpoint returns a successful response when the application runs

### US-005: Health Check Endpoint
**As a** a developer deploying the app to production  
**I want** the template to include a `/health` endpoint  
**So that** load balancers and monitoring tools can verify the app is running

**Acceptance Criteria (EARS):**
- **Given** a new project is generated from the template
- **When** I run the application
- **Then** a `GET /health` endpoint responds with a 200 status code
- **And** the response indicates the health status of the application

### US-006: Testing Infrastructure
**As a** a developer writing tests for the app  
**I want** the template to include xUnit and `Microsoft.AspNetCore.Mvc.Testing` configured  
**So that** I can write HTTP-level integration tests without mocking EF

**Acceptance Criteria (EARS):**
- **Given** a new test project is generated from the template
- **When** I inspect the test project file and directory structure
- **Then** I see:
  - xUnit referenced and configured
  - `Microsoft.AspNetCore.Mvc.Testing` referenced
  - A sample test class demonstrating HTTP-level testing
  - In-memory SQLite database configuration for tests
- **And** running `dotnet test` executes tests successfully

### US-007: Configuration and Logging
**As a** a developer configuring the app  
**I want** the template to use the Options pattern with `ILogger<T>` for logging  
**So that** I have a standard, extensible configuration and logging setup

**Acceptance Criteria (EARS):**
- **Given** a new project is generated from the template
- **When** I inspect the Program.cs and feature code
- **Then** I see:
  - Configuration loaded via the Options pattern (e.g., `services.AddOptions<MyOptions>()`)
  - Logging configured via `ILogger<T>` injected into services
  - At least one example of configuration validation on application start
- **And** configuration errors are caught and reported clearly at startup

### US-008: Docker Support
**As a** a developer deploying the app  
**I want** the template to include a multi-stage Dockerfile and docker-compose.yml  
**So that** I can containerize and orchestrate the app locally and in production

**Acceptance Criteria (EARS):**
- **Given** a new project is generated from the template
- **When** I inspect the Dockerfile
- **Then** I see:
  - Multi-stage build (restore → build → runtime)
  - Non-root user running the application
  - Build artifacts optimized for size and security
- **And** when I inspect docker-compose.yml
- **Then** I see:
  - The web app service configured
  - PostgreSQL service (optional, commented or conditional)
  - Network configuration for local development
- **And** running `docker-compose up` starts the app successfully

### US-009: Kiro Integration
**As a** a developer using Crypt with Kiro IDE  
**I want** the template to ship with its own `.kiro` folder with steering, hooks, and a custom agent for EF migrations  
**So that** a generated project starts Kiro-ready without additional setup

**Acceptance Criteria (EARS):**
- **Given** a new project is generated from the template
- **When** I inspect the `.kiro/` directory
- **Then** I see:
  - `steering/` folder with `.md` files for tech, structure, and product conventions
  - `hooks/` folder with automation hooks (e.g., build on save, test on save)
  - `agents/` folder with custom agents, including one for EF Core migrations
  - `.kiro/` files reference the generated app, not generic/placeholder values
- **And** opening the project in Kiro IDE loads steering automatically

### US-010: Code and Naming Conventions
**As a** a developer using the Crypt template  
**I want** the template to follow clean architecture principles without over-engineering  
**So that** the code is maintainable and scalable from day one

**Acceptance Criteria (EARS):**
- **Given** a new project is generated from the template
- **When** I inspect the project structure and code
- **Then** I see:
  - Features grouped by folder (`Features/<Name>/`) not by technical layer
  - One web project + one test project (no over-engineered layers)
  - All async code paths with `CancellationToken` on endpoints and EF calls
  - Code comments and identifiers in English
- **And** spooky naming (e.g., "Haunted Inventory") is used only in UI copy, never in code identifiers

### US-011: README with Setup Instructions
**As a** a developer generating a new app from the template  
**I want** the README to include project-specific setup instructions  
**So that** I know how to build, test, run, and deploy the app

**Acceptance Criteria (EARS):**
- **Given** a new project is generated from the template
- **When** I open the generated README.md
- **Then** I see:
  - Project description with app name and purpose
  - Prerequisites (e.g., .NET 10, Docker)
  - Build and test instructions
  - Local development setup
  - Docker deployment instructions
  - Any app-specific features or configurations
- **And** following the instructions results in a working local development environment

### US-012: Template Metadata and Parameters
**As a** the template creator  
**I want** to define template parameters so generated projects can customize project name, description, and other metadata  
**So that** the template is reusable and flexible

**Acceptance Criteria (EARS):**
- **Given** I run `dotnet new crypt -n MyApp --desc "My Description"`
- **When** the template generates
- **Then** I see:
  - Project name substituted throughout (.csproj, namespace, folder names)
  - Description used in README and docker-compose metadata
  - Default values provided for all parameters
- **And** the generated project builds and runs without manual path or reference fixes
