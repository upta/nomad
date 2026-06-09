# Godot C# Prototype Template

A GitHub template for rapid Godot game prototyping in **C#** with automated validation and Copilot CLI agent skills.

The goal: **humans playtest for fun, not for bugs.** Automated validation catches regressions so you can iterate on game feel instead of QA.

## What's Included

- **Godot 4.x C# project scaffold** (`client/`) — ready-to-run with test-mode routing and .NET 8.0
- **[Agentic Godot Validation Kit](https://github.com/upta/agentic-godot-validation)** — automated gameplay validation via scenario contracts (git submodule)
- **[Agent Skills](https://github.com/addyosmani/agent-skills)** — production-grade development workflows for Copilot CLI (git submodule)
- **Setup scripts** — cross-platform symlink management for both submodules
- **`godot-validation-testing` skill** — bridges general TDD methodology with Godot-specific validation

## Quick Start

### 1. Create from template

Click **"Use this template"** on GitHub, then clone your new repo:

```bash
git clone --recursive https://github.com/YOUR_USER/YOUR_REPO.git
cd YOUR_REPO
```

If you already cloned without `--recursive`:

```bash
git submodule update --init --recursive
```

### 2. Set up symlinks

**Windows (PowerShell):**
```powershell
.\setup.ps1
```

**Linux/macOS:**
```bash
chmod +x setup.sh && ./setup.sh
```

This creates symlinks from both submodules into the project (`client/addons/`, `tools/`, `.github/skills/`, `references/`).

### 3. Open in Godot

Open `client/project.godot` in Godot 4.x **Mono edition** (required for C# support). Build the project (the hammer icon or `dotnet build` from `client/`).

### 4. Run validation

```powershell
.\tools\run_scenario.ps1 -Scenario client\validation\scenarios\your_scenario.json -GodotExe "path\to\godot"
```

## Project Structure

```
├── submodules/
│   ├── agentic_godot_validation/     ← git submodule (validation kit)
│   └── agent_skills/                 ← git submodule (Copilot skills)
├── client/                            ← Godot project root
│   ├── project.godot
│   ├── Nomad.csproj                    ← .NET project file
│   ├── Nomad.sln                       ← .NET solution
│   ├── bootstrap/                      ← app entry (test-mode routing)
│   │   ├── AppRoot.cs                  ← C# entry point
│   │   └── AppRoot.tscn
│   ├── game/                           ← your game scenes and scripts
│   │   ├── Main.cs                     ← placeholder game scene
│   │   └── Main.tscn
│   ├── addons/
│   │   └── agentic_godot_validation/   ← symlink → validation submodule
│   └── validation/
│       ├── harnesses/                  ← test harness scenes
│       ├── scenarios/                  ← scenario JSON contracts
│       └── scripts/
│           └── harness_controllers/    ← C# state exposure scripts
├── .github/
│   ├── agents/                       ← Copilot agent personas
│   ├── skills/                       ← Copilot CLI skills (symlinked + custom)
│   ├── instructions/                 ← path-specific Copilot guidance
│   └── copilot-instructions.md       ← project-wide Copilot config
├── tools/                            ← symlink → validation runner scripts
├── references/                       ← symlinked testing patterns
├── setup.ps1 / setup.sh             ← symlink setup
└── symlink-config.txt               ← declarative symlink mapping
```

## Language: C#

This template uses **C# (.NET 8.0)** for all game code. The original GDScript version is available at [godot-prototype-template](https://github.com/upta/godot-prototype-template).

Key differences from GDScript:
- All game scripts are `.cs` files (not `.gd`)
- Project uses `Nomad.csproj` and `Nomad.sln`
- Build with `dotnet build` or Godot's built-in MSBuild
- Requires **Godot Mono** edition (standard Godot does not include .NET support)
- Godot API is accessed through the `Godot` namespace (e.g., `GD.Load<PackedScene>()`, `GetNode<T>()`)

## Copilot CLI Agent Skills

This template integrates 6 skills from [agent-skills](https://github.com/addyosmani/agent-skills) for structured development workflows:

| Skill | Phase | When to Use |
|-------|-------|------------|
| `spec-driven-development` | Define | Designing features and creating specs |
| `planning-and-task-breakdown` | Plan | Breaking work into verifiable tasks |
| `incremental-implementation` | Build | Building features in small increments |
| `test-driven-development` | Build | TDD: Red-Green-Refactor for logic |
| `godot-validation-testing` | Verify | Godot-specific validation scenarios |
| `code-review-and-quality` | Review | Reviewing code across 5 axes |
| `debugging-and-error-recovery` | Debug | Triaging and fixing bugs |

### Godot-Specific Testing Skill

The custom `godot-validation-testing` skill bridges general TDD with Godot validation:

- **References** `test-driven-development` for core TDD methodology
- **References** `author-validation-scenario` for scenario creation
- **References** `debug-validation-failure` for debugging
- **Provides** Godot-specific workflows: harness controllers, scenario contracts, the Prove-It pattern for game bugs

### Agent Personas

Invoke in Copilot Chat:

```
@code-reviewer Review this PR
@test-engineer Analyze test coverage for player controller
```

Both agents are aware of the Godot validation framework and will reference appropriate skills.

## How Validation Works

1. **You** create game scenes with observable state (exposed via harness controllers)
2. **You** write scenario contracts (JSON) that simulate input and assert outcomes
3. Scenarios run **headlessly** via CLI — no human interaction needed
4. Artifacts (screenshots, event logs, scene trees) are produced for debugging

### Example Scenario

```json
{
  "name": "player_moves_right",
  "timeout": 10,
  "setup": { "harness_scene": "res://validation/harnesses/player_harness.tscn" },
  "steps": [
    { "action": "simulate_input", "input": "move_right", "pressed": true },
    { "action": "wait_frames", "frames": 60 },
    { "action": "assert_value", "path": "harness_state.nodes.player.position.x",
      "operator": "greater_than", "value": 0 }
  ]
}
```

See the [validation kit docs](https://github.com/upta/agentic-godot-validation) for the full scenario contract reference.

## Validation-First Policy

- **Every gameplay code change MUST include validation scenarios.**
- Humans play-test for **fun and game feel**, not for QA.
- If a bug is found in play-testing, add a reproduction scenario as part of the fix.
- A feature is not done until scenarios exist, pass, and all existing scenarios still pass.

## Updating Submodules

```bash
# Update validation kit
cd submodules/agentic_godot_validation && git pull origin main && cd ../..

# Update agent skills
cd submodules/agent_skills && git pull origin main && cd ../..

# Re-run setup if symlink targets changed
.\setup.ps1

# Commit the submodule pointer update
git add submodules/ && git commit -m "chore: update submodules"
```

## Requirements

- **Godot 4.x Mono** (C# support requires the Mono/.NET edition)
- **.NET SDK 8.0** (for `dotnet build` and C# tooling)
- **PowerShell** (Windows) or **bash** (Linux/macOS) for setup scripts
- **Git** with submodule support
