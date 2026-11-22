# Video script — Crypt (Kiroween 2026, Skeleton Crew)

Target length **2:45** (hard limit 3:00). Screen recording + voice-over, English.
No third-party music unless royalty-free with a licence; no logos other than our own UI.
Terminal: large font (≥ 18 pt), dark theme. Browser: 1280×800, zoom 125 %.

| Time | Screen | Voice-over |
|---|---|---|
| 0:00–0:15 | Repo root on GitHub: README table (template + two apps), `.kiro` folder visible | "Every small business app starts the same way — and every AI agent dropped into an empty folder re-invents it differently. Crypt is a .NET skeleton that gives the *agent* the house rules on day one." |
| 0:15–0:40 | Terminal: `dotnet new install ./template`, `dotnet new crypt -n GraveyardShift --title "Graveyard Shift"`, `tree -a -L 3 GraveyardShift/.kiro` | "One command creates an ASP.NET Core, Blazor and EF Core app — plus a ready dot-kiro folder: steering already rewritten for the new app's name, two custom agents, and hooks for Kiro IDE and Kiro CLI." |
| 0:40–1:05 | Open `.kiro/steering/structure.md` (namespace `GraveyardShift`), then `.kiro/agents/feature-scaffold.json` scrolled to `"hooks"` | "The feature-scaffold agent adds a vertical slice — entity, service, endpoint, tests, page. Two hooks keep it honest: tests run when the agent stops, and hand edits of generated migration files are blocked before they happen." |
| 1:05–1:20 | Terminal: `kiro-cli chat --agent-engine v3 --agent feature-scaffold` asked to edit a `*.Designer.cs`; the guard message appears and the write is refused | "That guard is a PreToolUse hook returning exit code two — we learned the hard way that a PostToolUse hook only reports damage after it's done." |
| 1:20–1:50 | Browser: **Haunted Inventory** `<HAUNTED_URL>` — items list, low-stock filter, open an item, record "Sold −5" on an item with quantity 3 → "insufficient stock" error, quantity unchanged; record "Sold −2" → history row added, quantity 1 | "App one, built from the skeleton with its own Kiro spec: stock for a costume rental shop. Stock can never go below zero — not in the form, not in the API, not with twenty concurrent requests." |
| 1:50–2:20 | Browser: **Seance Scheduler** `<SEANCE_URL>` — add a medium, book 20:00–21:00, try 20:30–21:30 with the same medium → 409 message, form keeps values; schedule grouped by day; cancel | "App two, completely different domain: bookings with mediums. Overlapping seances are rejected inside a serialized transaction — twenty parallel bookings of one slot give exactly one success." |
| 2:20–2:35 | Editor: `apps/seance-scheduler/.kiro/specs/seance-booking/design.md` side by side with `git log --oneline` showing the spec revision commit; then terminal `dotnet test` green (135 passed) | "Everything was spec-driven with Kiro CLI running headless. The biggest win: reviewing design.md like a pull request — the concurrency bugs were fixed in the spec, before a line of code existed." |
| 2:35–2:45 | README again, MIT badge/About | "Crypt — a Kiro-ready skeleton. Two apps, three specs, one set of house rules. Link in the description." |

## Recording checklist

- Prepare data beforehand: in Haunted Inventory an item with quantity 3 (e.g. "Vampire cape");
  in Seance Scheduler one active medium (e.g. "Madame Vex").
- Hook demo (1:05): run in a scratch copy, not in the repo; if the run takes long, cut the
  wait in the edit.
- Upload to YouTube as **Public**, title "Crypt — a Kiro-ready .NET skeleton (Kiroween 2026)";
  put the repo URL in the description.
