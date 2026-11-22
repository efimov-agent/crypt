# How Kiro was used to build Crypt

Crypt was built with **Kiro CLI** (3.5, Linux, headless) on the Kiro Free plan. The whole
project is spec-driven: one spec for the template itself and one spec per app. Kiro wrote
the specs and the first implementation of every phase; each Kiro change was then built,
tested and reviewed before it was committed (the git history keeps Kiro's commits and
the follow-up fixes separate, e.g. `2938102` → `06a280c`).

## 1. Steering first

Before any code, three steering docs were written in [`.kiro/steering/`](../.kiro/steering):
`product.md` (what the template is for, who uses it), `tech.md` (.NET 10, minimal API,
Blazor SSR, EF Core, SQLite → PostgreSQL, xUnit, Docker) and `structure.md` (folder layout,
naming, where tests go). They are the reason the generated code came out in one consistent
style across three solutions.

The template then **ships its own steering** for the generated app
(`template/.kiro/steering/`), written from the point of view of the app rather than the
template — after `dotnet new crypt -n Foo`, the namespace, paths and commands in the
steering already say `Foo`. Both apps (`apps/*/.kiro/steering/`) were generated that way.

## 2. Specs instead of vibe coding

| Spec | Requirements → design → tasks | Implemented by Kiro |
|---|---|---|
| [`crypt-template`](../.kiro/specs/crypt-template) | the skeleton: layers, health, Docker, tests, `.kiro`, NuGet packaging | phases 1–6 in one run, then 7–10 |
| [`stock-tracking`](../apps/haunted-inventory/.kiro/specs/stock-tracking) | SKU, quantities, stock movements, low-stock filter, Blazor pages | backend tasks 1–6, then pages |
| [`seance-booking`](../apps/seance-scheduler/.kiro/specs/seance-booking) | mediums, slots, no double-booking, schedule UI, e2e | tasks 1–12 in one run |

What paid off was **reviewing the spec before the code**. Each spec got one revision pass
(commits `701ec6e`, `c1f842d`) that fixed design problems while they were still cheap:

- *stock-tracking*: the first design read the quantity, checked it in C# and wrote it back —
  a lost update under concurrency. The revised design uses a single conditional
  `UPDATE … SET Quantity = Quantity + @delta WHERE Quantity + @delta >= 0`, and a migration
  step that backfills SKUs for existing rows **before** the unique index is created.
- *seance-booking*: the first design had a unique index on `(MediumId, StartsAt)`, which
  does not prevent *overlapping* slots and blocks re-booking a cancelled slot. The revised
  design uses a non-unique index plus an overlap check inside a `BEGIN IMMEDIATE`
  transaction, a UTC value converter (SQLite returns `DateTimeKind.Unspecified`), atomic
  cancel, and real concurrency tests (20 parallel bookings → exactly one `201`).

Tasks in `tasks.md` reference requirement ids and are ticked as they are completed, so the
spec doubles as the progress log.

## 3. Hooks — and what we learned about them

Every generated app has two hooks:

- **`test-on-stop`** — when the agent finishes a turn, run `dotnet test` once. The output
  is redirected to **stderr**: we found that is the channel the agent actually sees, so a
  red test comes back into the conversation instead of being silently logged.
- **`guard-migrations`** — blocks hand edits of EF-generated `*.Designer.cs` and the model
  snapshot and tells the agent to use `dotnet ef migrations add`. Editing `Up`/`Down` of a
  new migration (for data backfills) stays allowed.

Experiments with Kiro CLI hooks shaped the final design:

- Kiro CLI does not read standalone hook files; hooks have to be declared **inline in the
  agent config** (`.kiro/agents/*.json`, `"hooks"` array). Kiro IDE reads
  `.kiro/hooks/*.json`. The template ships **both formats** for the same two hooks.
- A `PostToolUse` hook runs after the write has already happened and its output is not
  shown to the model — useless as a guard. A **`PreToolUse` hook with exit code 2** blocks
  the tool call and its stderr is returned to the model. The guard was rewritten that way
  and limited to write tools (`fs_write|fs_append|str_replace`) so the agent can still
  *read* generated files.
- A per-save build hook was too slow and noisy for a .NET solution; one test run on
  `Stop` gives the same safety net.

## 4. Custom agents

Two workspace agents (`.kiro/agents/feature-scaffold.json`, `migrations.json`) with
prompts in `.kiro/prompts/`:

- **feature-scaffold** — adds a vertical slice (entity → service → minimal API endpoint →
  tests → page) following the steering; carries both hooks.
- **migrations** — creates, reviews and applies EF Core migrations safely (never edits
  generated files, checks the generated SQL, backfills before constraints).

Usage: `kiro-cli chat --agent-engine v3 --agent feature-scaffold`.

## 5. Kiro CLI, headless

All Kiro work ran non-interactively on a Linux server:

```bash
kiro-cli chat --no-interactive --trust-all-tools --model <model> "<task from tasks.md>"
```

On the Free plan's 50 credits we matched the model to the job: a fast lightweight model
for specs, **MiniMax M2.5** for multi-task implementation runs (phases 1–6 of the
template cost about 12 credits), **Qwen3 Coder Next** for small fixes.

## 6. What we would tell another Kiro user

- Review `design.md` like a pull request. Concurrency and data-migration bugs were cheapest
  to fix there.
- Check the asserts, not just the green tick: when a test failed, the quickest "fix" was
  sometimes a weaker assertion. The `test-on-stop` hook catches red tests, a human has to
  catch weak ones.
- Put hook output on stderr and use `PreToolUse` + exit 2 for anything that must be
  prevented rather than reported.
- Ship steering *inside* your templates — it is the cheapest way to make every future
  project start with an agent that already knows the house rules.

## Numbers

| | Tests | Notes |
|---|---|---|
| Template (fresh `dotnet new crypt`) | 15 | build + tests + Docker verified from the packed `.nupkg` |
| Haunted Inventory | 55 + 6 e2e | 20-way concurrent stock movements never go below zero |
| Seance Scheduler | 135 + 4 e2e | 20 parallel bookings of one slot → 1 × 201, 19 × 409 |
