# Devpost submission — Kiroween 2026

Copy-paste material for the Devpost form. Placeholders: `<REPO_URL>`, `<HAUNTED_URL>`,
`<SEANCE_URL>`, `<VIDEO_URL>` — fill in after deployment and video upload.

- **Project name:** Crypt — a Kiro-ready .NET skeleton
- **Category:** Skeleton Crew
- **Tagline (≤200 chars):** A `dotnet new` template that ships with Kiro steering, custom agents and guard-rail hooks — proven by two haunted business apps built from it with Kiro specs.
- **Repository:** `<REPO_URL>` (MIT, `/.kiro` at the root)
- **Live apps:** Haunted Inventory `<HAUNTED_URL>`, Seance Scheduler `<SEANCE_URL>` (no login needed)
- **Video:** `<VIDEO_URL>`
- **Built with:** kiro, kiro-cli, csharp, dotnet, asp.net-core, blazor, entity-framework-core, sqlite, xunit, playwright, docker

---

## Inspiration

Most small business software is the same shape: a few tables, forms, a list with filters,
one rule that must never break (stock never goes negative, a slot is never booked twice).
Every new project starts by re-deciding the same things, and an AI agent dropped into an
empty folder re-decides them differently every time. I wanted a skeleton where the *agent*
gets the house rules on day one, not just the code.

## What it does

**Crypt** is a `dotnet new crypt` template for .NET 10 (ASP.NET Core minimal API, Blazor
SSR, EF Core, SQLite, Docker). Every generated app contains a ready `.kiro` folder:

- **Steering** (`product.md`, `tech.md`, `structure.md`) already rewritten for the new
  app's name, namespace and paths.
- **Two custom agents**: `feature-scaffold` (adds a vertical slice: entity → service →
  endpoint → tests → page) and `migrations` (EF Core migrations, done safely).
- **Hooks** in both Kiro IDE and Kiro CLI format: run the tests when the agent stops, and
  stop edits of EF-generated migration files (blocked before the write in Kiro CLI,
  flagged after save in Kiro IDE).

To show it is a real skeleton and not a one-off, two very different apps were built from it,
each with its own Kiro spec:

- **Haunted Inventory** — stock for a costume and prop rental shop: SKUs, quantities,
  reorder levels, stock movements with history and a low-stock filter. Stock can never go
  below zero, even with 20 concurrent movements on one item.
- **Seance Scheduler** — bookings with mediums: 30-minute to 4-hour seances on a
  15-minute grid, a schedule grouped by day,
  cancellation. A slot can never be double-booked: 20 parallel bookings of the same slot
  give exactly one success and nineteen `409 Conflict`.

Both have a dark UI with AA-contrast colours, labelled forms with antiforgery and
Post/Redirect/Get, and a JSON API. Seance Scheduler goes further: errors linked to their
fields with `aria-invalid` + `aria-describedby`, values kept after a failed submit, and
`application/problem+json` for every API error.

## How I built it

Spec-driven, with Kiro CLI running headless on a Linux server:

1. Steering docs first, then a `crypt-template` spec (requirements → design → tasks).
   Kiro implemented the skeleton phase by phase.
2. Generated each app with `dotnet new crypt`, wrote an app spec with Kiro
   (`stock-tracking`, `seance-booking`), **reviewed `design.md` like a pull request**, and
   let Kiro implement the backend tasks.
3. Kiro's runs were mostly committed as-is, then built, tested and reviewed, with fixes in
   separate commits (the first app's pages were corrected before commit). When the Free plan's credits ran out, the remaining tasks of the second app
   (pages, tests, Playwright e2e) were finished without Kiro, still following the spec.

Kiro features used: specs, steering, custom agents with file prompts and resources, hooks
(`Stop`, `PreToolUse`), headless `kiro-cli chat --no-interactive`, per-task model choice to
stay within the Free plan's credits. Full writeup: `docs/kiro-writeup.md`.

## Challenges I ran into

- **Hooks that look right but do nothing.** Kiro CLI ignores standalone hook files — hooks
  must live inside the agent config. A `PostToolUse` guard runs after the damage is done and
  its output never reaches the model; the working guard is `PreToolUse` with exit code 2.
  Test output goes to stderr: per the Kiro hook docs that is what gets reported for a failing
  command, and `dotnet test` writes its errors to stdout.
- **Concurrency on SQLite.** The first stock design was read-check-write (a lost update
  under load); the first booking design had a unique index that made a re-booked slot
  impossible to cancel (two identical `Cancelled` rows). Fixed in the specs: a conditional
  `UPDATE … WHERE Quantity + delta >= 0`, and a non-unique index next to the existing
  overlap check in a `BEGIN IMMEDIATE` transaction — both proven by parallel tests.
- **Time zones.** SQLite returns `DateTimeKind.Unspecified`; without explicit UTC
  converters the rules behaved differently on a server in Berlin and one in UTC.
- **Blazor SSR quirks**, e.g. a static `<title>` silently overriding every `PageTitle`, and
  status pages for unknown routes coming back empty.

## Accomplishments that I'm proud of

- One template, two unrelated apps, three Kiro specs — and the generated apps really are
  Kiro-ready: open the folder and the agent already knows the stack and the rules.
- Hard business invariants backed by real concurrency tests, not just by happy-path demos.
- 15 tests in a fresh template; 55 + 6 e2e in Haunted Inventory; 135 + 4 e2e in Seance Scheduler.

## What I learned

- Spec review is the cheapest place to fix concurrency and data-migration bugs.
- Check the asserts, not only the green tick — the quickest "fix" for a red test is
  sometimes a weaker assertion.
- Steering shipped inside a template is the most leveraged Kiro file you can write.

## What's next for Crypt

- Publish the template to NuGet and add a PostgreSQL switch as a template option.
- Authentication as an optional template feature.
- More custom agents: `ui-page` (accessible form from a DTO) and `seed-data`.
