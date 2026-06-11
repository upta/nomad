# Nomad

Godot 4.x C# cooperative multiplayer game using [SpacetimeDB](https://spacetimedb.com/) for server-authoritative networking and the [agentic-godot-validation](https://github.com/upta/agentic-godot-validation) kit for automated in-engine gameplay validation.

Reference for working SpacetimeDB + Godot integration patterns: [untrailed](https://github.com/upta/untrailed).

## Layout

- `client/` — Godot 4.x C# project root (`project.godot`, `.sln`, `.csproj`). **See `client/CLAUDE.md` for Godot conventions — scene-first rules are mandatory.**
- `server/` — SpacetimeDB server module. **See `server/CLAUDE.md` for SpacetimeDB rules.**
- `client/validation/` — harnesses, scenario contracts, harness controllers (host-owned validation assets)
- `client/Db/` — generated SpacetimeDB client bindings — **never edit**
- `client/addons/` — external addons, including the symlinked validation runtime — **never edit** (validation kit changes belong in `submodules/agentic_godot_validation/` and require asking first)
- `tools/` — symlinked validation runner scripts (`run_scenario.ps1`, `run_all_scenarios.ps1`)
- `.claude/skills/` — project-owned skills (`validate-gameplay`, `run-game`, `spacetime-db-reference`) plus workflow skills symlinked from `submodules/agent_skills` and the validation kit. Run `./setup.ps1` after initializing/updating submodules to (re)create symlinks.
- `docs/game-design-document.md` — the GDD (ask before modifying)
- `SPEC.md`, `tasks/plan.md`, `tasks/todo.md` — spec, implementation plan, task tracking

## Commands

All from repo root unless noted. `godot.exe` is on PATH (godotenv); `GODOT_EXE` env var overrides.

| Action | Command |
|---|---|
| Build client | `dotnet build` (in `client/`) |
| Build server | `spacetime build --module-path ./src` (in `server/`) |
| Format | `dotnet csharpier format .` (in `client/` and `server/`) |
| Publish module | `spacetime publish nomad --yes --server local --module-path ./src` (in `server/`) |
| Regenerate client bindings | `spacetime generate --lang csharp --out-dir ../client/Db --module-path ./src` (in `server/`) |
| Run one validation scenario | `./tools/run_scenario.ps1 -Scenario client/validation/scenarios/<name>.json` |
| Run pure validation suite | `./tools/run_all_scenarios.ps1` |
| Run SpacetimeDB validation suite | `./scripts/run_stdb_scenarios.ps1` (ephemeral DB, auto-starts server) |
| Run both suites | `./scripts/validate_all.ps1` |
| Boot the real game | use the `run-game` skill |

**Multi-client testing from the editor:** Debug → Customize Run Instances is preconfigured (machine-local, lives in gitignored `client/.godot/editor/project_metadata.cfg`) to launch 2 instances with per-instance args `--position X,Y -- --client one` / `-- --client two`. `DbManager` reads `--client <id>` (after the `--` user-args separator) and keeps a token file per id (`.nomad-<id>`), so each instance authenticates as a distinct SpacetimeDB identity. If `.godot/` is wiped, re-enter those args in the dialog.

## Validation-first (play-testing, not QA)

Humans play-test for fun, feel, and game-design feedback — **never** to find bugs. Proving the code works in a running Godot engine is your job, before any human launches the game. Concretely:

- Every gameplay change ships with validation scenario(s) proving the new behavior in-engine. Use the **`validate-gameplay` skill** for the workflow, the real contract format, and how to review run artifacts.
- A bug found during play-testing means validation has a gap: first write a scenario that reproduces the bug (and fails), then fix until it passes.
- If the framework can't assert what you need, the framework gets improved (ask first — it's a submodule). Skipping validation is never the fallback.

## Definition of Done

A feature is not done until all of these hold:

1. **Validation scenarios exist** for the change — intended behavior, not just the happy path.
2. **New scenarios pass** — run them, confirm green, and **visually review the checkpoint screenshots** in the run artifacts (numeric assertions can pass while rendering is broken).
3. **Both suites pass** — `./scripts/validate_all.ps1` (pure scenarios + SpacetimeDB scenarios), no regressions. Anything touching reducers, subscriptions, or networked behavior needs coverage in `client/validation/scenarios_stdb/`.
4. **Game boots clean** — run the real game headless for ≥10s with zero `ERROR:` log lines (see the `run-game` skill). Test-mode scenarios can miss startup-path bugs.
5. **Builds and formatting are clean** — `dotnet build` + `dotnet csharpier format .` in `client/`; `spacetime build` + format in `server/`.
6. **`git push origin`** at the end of every work batch.

A Stop hook (`.claude/hooks/check-validation.ps1`) will block ending the session while gameplay code has changed without a newer validation run.

## Boundaries

- **Ask first:** changes inside `submodules/agentic_godot_validation/`, adding NuGet packages, modifying `docs/game-design-document.md`
- **Never:** create GDScript (`.gd`) files — all game code is C# (.NET 8.0); skip validation; delete or weaken a failing scenario to make the suite pass

## C# style

CSharpier owns formatting. Beyond that:

- `PascalCase` public members, `_camelCase` private fields; filenames PascalCase matching the class (`.cs` and `.tscn` both)
- File-scoped namespaces; `var` when the type is obvious; `""` not `String.Empty`; null propagation; primary constructors
- Class member order: fields, constructors, delegates, events, enums, interfaces, properties, methods, structs, classes — `public` → `internal` → `protected` → `private`, then alphabetical within each group. Exception: Godot lifecycle methods (`_Ready`, `_PhysicsProcess`, …) come first among methods as their own group.
- Comments are rare and explain *why*, never *what*
