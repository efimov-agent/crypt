# Video script — Crypt (Kiroween 2026, Skeleton Crew)

Target length **2:45** (hard limit 3:00). Screen recording + voice-over, English.
No third-party music unless royalty-free with a licence; no logos other than our own UI.
Terminal: large font (≥ 18 pt), dark theme. Browser: 1280×800, zoom 125 %.

| Time | Screen | Voice-over |
|---|---|---|
| 0:00–0:15 | Repo root on GitHub: README table (template + two apps), `.kiro` folder visible | "Every small business app starts the same way — and every AI agent dropped into an empty folder re-invents it differently. Crypt is a .NET skeleton that gives the *agent* the house rules on day one." |
| 0:15–0:40 | Terminal: `dotnet new install ./template`, `dotnet new crypt -n GraveyardShift --title "Graveyard Shift"`, `tree -a -L 3 GraveyardShift/.kiro` | "One command creates an ASP.NET Core, Blazor and EF Core app — plus a ready dot-kiro folder: steering already rewritten for the new app's name, two custom agents, and hooks for Kiro IDE and Kiro CLI." |
| 0:40–1:05 | Open `.kiro/steering/structure.md` (namespace `GraveyardShift`), then `.kiro/agents/feature-scaffold.json` scrolled to `"hooks"` | "The feature-scaffold agent adds a vertical slice — entity, service, endpoint, tests, page. Two hooks keep it honest: tests run when the agent stops, and in Kiro CLI, edits of generated migration files are blocked before they happen." |
| 1:05–1:20 | Terminal: `kiro-cli chat --agent-engine v3 --agent feature-scaffold` asked to edit a `*.Designer.cs`; the guard message appears and the write is refused | "That guard is a PreToolUse hook returning exit code two — we learned the hard way that a PostToolUse hook only reports damage after it's done." |
| 1:20–1:50 | Browser: **Haunted Inventory** https://haunted.efimomax-sandbox.de — items list, low-stock filter, open an item, record "Sold −5" on an item with quantity 3 → "Not enough stock … Current: 3, Delta: -5" error, quantity unchanged; record "Sold −2" → history row added, quantity 1 | "App one, built from the skeleton with its own Kiro spec: stock for a costume rental shop. Stock can never go below zero — not in the form, not in the API, not with twenty concurrent requests." |
| 1:50–2:20 | Browser: **Seance Scheduler** https://seance.efimomax-sandbox.de — add a medium, book 20:00–21:00, try 20:30–21:30 with the same medium → 409 message, form keeps values; schedule grouped by day; cancel | "App two, completely different domain: bookings with mediums. Overlapping seances are rejected inside a serialized transaction — twenty parallel bookings of one slot give exactly one success." |
| 2:20–2:35 | Editor: `apps/seance-scheduler/.kiro/specs/seance-booking/design.md` scrolled to the index/transaction section (on screen caption: draft had a UNIQUE index incl. Status → fixed in review); then terminal `dotnet test` green (135 passed) | "Everything was spec-driven with Kiro CLI running headless. The biggest win: reviewing design.md like a pull request — the concurrency and data bugs were fixed in the spec, before a line of code existed." |
| 2:35–2:45 | README again, GitHub About box with the MIT license | "Crypt — a Kiro-ready skeleton. Two apps, three specs, one set of house rules. Link in the description." |

## Recording checklist

- Prepare data beforehand. Haunted Inventory has no create-item page: create the item via
  the API and add stock with a movement, e.g.
  `curl -X POST https://haunted.efimomax-sandbox.de/api/items -H 'Content-Type: application/json' -d '{"name":"Vampire cape","sku":"CAPE-01","reorderLevel":2}'`,
  then on the item page record "Received +3".
  Seance Scheduler: one active medium (e.g. "Madame Vex") via /mediums.
- Hook demo (1:05): needs Kiro credits (Free credits reset on 01.12, or Pro). Run it in a
  scratch copy, not in the repo; cut the wait in the edit. Fallback without credits: run
  `.kiro/hooks/guard-migrations.sh` with a sample PreToolUse JSON on stdin and show exit 2
  + the message.
- Upload to YouTube as **Public**, title "Crypt — a Kiro-ready .NET skeleton (Kiroween 2026)";
  put the repo URL in the description.
