---
inclusion: always
---
# Repository structure

```
template/           dotnet new template source (Crypt), includes its own .kiro/
apps/haunted-inventory/   app #1 generated from the template
apps/seance-scheduler/    app #2 generated from the template
.kiro/              Kiro config for this repo (steering, specs, hooks, agents)
```
Each app: `src/<App>.Web/`, `tests/<App>.Tests/`, `<App>.slnx`, `Dockerfile`, `README.md`.
Build check for any change: `dotnet build` and `dotnet test` in the affected folder must pass.
