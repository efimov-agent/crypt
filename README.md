# Crypt — a Kiro-ready .NET skeleton (Kiroween 2026, Skeleton Crew)

**Crypt** is a `dotnet new` template for small business web apps on .NET 10
(ASP.NET Core minimal API + Blazor SSR + EF Core + Docker) that ships with a complete
`.kiro` folder: steering docs and two custom agents with hooks.
Generate a project, open it in Kiro, and the agent already knows the stack, the folder
layout and the rules — and the hooks keep it honest (tests run when the agent stops,
edits of generated EF migration files are blocked in Kiro CLI and flagged in Kiro IDE).
Each app built from it adds its own Kiro spec.

Two distinct apps were built from the same skeleton to show its versatility:

| Folder | App | What it shows |
|---|---|---|
| [`template/`](template) | **Crypt** template | The skeleton itself: feature-sliced app, 15 tests, Dockerfile + compose, `.kiro` |
| [`apps/haunted-inventory/`](apps/haunted-inventory) | **Haunted Inventory** — stock for a costume & prop rental shop | SKU, quantities, reorder levels, atomic stock movements (never below zero, also under 20 concurrent requests), movement history, low-stock filter |
| [`apps/seance-scheduler/`](apps/seance-scheduler) | **Seance Scheduler** — bookings with mediums | Mediums, seances on a 15-minute grid, no double-booking under concurrency (`BEGIN IMMEDIATE`), schedule by day, cancellation, problem+json API errors |

Live demos (public sandbox, no login; data may be reset):
[Haunted Inventory](https://haunted.efimomax-sandbox.de) ·
[Seance Scheduler](https://seance.efimomax-sandbox.de).

How Kiro was used (specs, steering, hooks, custom agents, Kiro CLI headless):
see [docs/kiro-writeup.md](docs/kiro-writeup.md).

## Quick start

Requirements: .NET 10 SDK; Docker is optional.

```bash
# 1. Install the template from this repo
dotnet new install ./template

# 2. Create an app (the -n name becomes the C# namespace)
dotnet new crypt -n GraveyardShift --title "Graveyard Shift" \
  --description "Shift planning for night watchmen"
cd GraveyardShift

# 3. Build, test, run
dotnet test GraveyardShift.slnx
dotnet run --project src/GraveyardShift.Web   # http://localhost:5000

# or in a container
docker compose up --build
```

The template can also be packed as a NuGet package: `dotnet pack Crypt.Templates.csproj -o ./artifacts`.

### Run the two apps

```bash
# from the repo root, one app at a time
(cd apps/haunted-inventory && dotnet test HauntedInventory.slnx && dotnet run --project src/HauntedInventory.Web)
(cd apps/seance-scheduler  && dotnet test SeanceScheduler.slnx  && dotnet run --project src/SeanceScheduler.Web)
```

The test suites include browser e2e tests (Playwright, headless Chromium); the first run
downloads Chromium. Both apps add forms on top of the skeleton (antiforgery,
Post/Redirect/Get); Seance Scheduler also keeps values after a validation error and marks
invalid fields with `aria-invalid` + `aria-describedby`. Details per app: each folder has its own README.

## What every generated app gets

- **Boring-on-purpose structure**: `Features/<Feature>/` vertical slices (service with the
  business rules + minimal API endpoints + DTOs), `Data/` (EF Core context and models),
  `Migrations/`, `Components/` (Blazor SSR pages).
- **Dark theme** with AA-contrast text and button colours, per-page titles, with a light Halloween touch.
- **Defaults that are easy to get wrong**: UTC timestamps independent of the host time zone,
  real 404 pages for unknown routes, persisted Data Protection keys, migrations applied on
  startup, non-root container user.
- **Tests from day one**: xUnit + `WebApplicationFactory` integration tests on a real SQLite file.
- **`.kiro/`**: steering (`product.md`, `tech.md`, `structure.md`), custom agents
  `feature-scaffold` and `migrations` with prompts, hooks for Kiro IDE (`hooks/dotnet.json`)
  and Kiro CLI (inline in the agent configs).

## Repository layout

```
.kiro/                      # Kiro config used to build the template itself (steering + spec)
template/                   # the dotnet new template (has its own .kiro for generated apps)
apps/haunted-inventory/     # app #1, generated from the template, own spec in .kiro/specs
apps/seance-scheduler/      # app #2, generated from the template, own spec in .kiro/specs
Crypt.Templates.csproj      # packs template/ as a NuGet template package
docs/                       # Kiro writeup
```

## License

[MIT](LICENSE)
