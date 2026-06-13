# Nomad

A cooperative top-down sci-fi survival game prototype — Godot 4.x **C#** client with a [SpacetimeDB](https://spacetimedb.com/) server-authoritative backend, developed with Claude Code and automated in-engine validation.

The goal: **humans playtest for fun, not for bugs.** Automated validation catches regressions so iteration time goes into game feel instead of QA. See `docs/game-design-document.md` for the design.

## What's Here

- **`client/`** — Godot 4.x C# project (.NET 8.0), scene-first conventions, Chickensoft AutoInject, GUIDE input
- **`server/`** — SpacetimeDB module (tables, reducers, views)
- **[Agentic Godot Validation Kit](https://github.com/upta/agentic-godot-validation)** — automated gameplay validation via scenario contracts (git submodule)
- **[Agent Skills](https://github.com/addyosmani/agent-skills)** — development workflow skills, loaded as a Claude Code plugin (declared in `.claude/settings.json`; accept the install prompt on first session)
- **Claude Code setup** — `CLAUDE.md` hierarchy (root, `client/`, `server/`), project skills in `.claude/skills/`, project commands in `.claude/commands/` (`/build`, `/test`, `/review`, `/ship` adapted to validation-first), and a Stop hook that refuses to finish a session with unvalidated gameplay changes

## Quick Start

### 1. Clone with submodules

```bash
git clone --recursive https://github.com/upta/nomad.git
cd nomad
# or, if already cloned: git submodule update --init --recursive
```

### 2. Set up symlinks

```powershell
.\setup.ps1        # Windows
./setup.sh         # Linux/macOS
```

This links the validation kit into the project (`client/addons/`, `tools/`, two `.claude/skills/` entries) per `symlink-config.txt`. The agent-skills plugin installs itself from `.claude/settings.json` — accept the prompt the first time you open Claude Code here.

### 3. Server

```powershell
spacetime start                                                    # local SpacetimeDB
cd server
spacetime build --module-path ./src
spacetime publish nomad --yes --server local --module-path ./src
spacetime generate --lang csharp --out-dir ../client/Db --module-path ./src
```

### 4. Client

Open `client/project.godot` in Godot 4.x **Mono edition**, or build with `dotnet build` from `client/`.

### 5. Run validation

```powershell
.\tools\run_scenario.ps1 -Scenario client\validation\scenarios\player_moves_right.json
.\tools\run_all_scenarios.ps1          # pure suite, runs 4-wide in parallel
.\scripts\validate_all.ps1             # both suites (pure 4-wide, STDB 2-wide)
```

Suites run scenarios in parallel by default (tune with `-MaxParallel`; pass `1` for a serial
debug run). The pure suite scales to 4-wide; the SpacetimeDB suite defaults to 2-wide because all
clients share one local server. `godot.exe` must be on PATH (or set `GODOT_EXE`). Artifacts —
screenshots, event logs, scene trees, summaries — land in `client/artifacts/`.

## Project Structure

```
├── client/                            ← Godot project root
│   ├── bootstrap/                     ← app entry (test-mode routing)
│   ├── game/                          ← gameplay scenes and scripts
│   ├── validation/
│   │   ├── harnesses/                 ← test harness scenes
│   │   ├── scenarios/                 ← scenario JSON contracts
│   │   └── scripts/harness_controllers/
│   ├── addons/agentic_godot_validation/  ← symlink → validation submodule
│   ├── Db/                            ← generated SpacetimeDB bindings
│   └── CLAUDE.md                      ← Godot conventions (scene-first, AutoInject, GUIDE)
├── server/
│   ├── src/{Tables,Reducers,Types,Views}/
│   └── CLAUDE.md                      ← SpacetimeDB rules
├── submodules/
│   └── agentic_godot_validation/      ← git submodule (validation kit)
├── .claude/
│   ├── skills/                        ← project skills + symlinked validation-kit skills
│   ├── commands/                      ← project commands (/build /test /review /ship)
│   ├── hooks/check-validation.ps1     ← Stop hook: no unvalidated gameplay changes
│   └── settings.json                  ← hooks + agent-skills plugin declaration
├── tools/                             ← symlink → validation runner scripts
├── tasks/                             ← plan, todo, validation-stdb-plan
├── CLAUDE.md                          ← project policy, commands, Definition of Done
├── setup.ps1 / setup.sh               ← symlink setup
└── symlink-config.txt                 ← declarative symlink mapping
```

## How Validation Works

1. Game state is exposed through **harness controllers** (`get_observed_state()`)
2. **Scenario contracts** (JSON) load a harness, simulate input, and assert on checkpointed state
3. Scenarios run **headlessly** via CLI — no human interaction needed
4. Each run produces artifacts (checkpoint screenshots, event log, scene tree, summary) for review

The authoritative scenario format and workflow live in `.claude/skills/validate-gameplay/SKILL.md`; `client/validation/scenarios/` has working examples.

## Validation-First Policy

- Every gameplay code change includes validation scenarios.
- Humans play-test for **fun and game feel**, not for QA.
- A bug found in play-testing gets a failing reproduction scenario before the fix.
- A feature is done when new scenarios pass, the full suite passes, and the game boots clean — the complete Definition of Done is in `CLAUDE.md`.

## Updating Submodules

```bash
git -C submodules/agentic_godot_validation pull origin main
./setup.ps1   # re-link if targets changed
git add submodules/ && git commit -m "chore: update submodules"
```

The agent-skills plugin updates through Claude Code's plugin system (`/plugin`), independent of git.

## Requirements

- **Godot 4.x Mono** (on PATH, or set `GODOT_EXE`)
- **.NET SDK 8.0** with the `wasi-experimental` workload (for the server module)
- **SpacetimeDB CLI** (`spacetime`)
- **PowerShell** (Windows) or **bash** (Linux/macOS) for setup scripts
