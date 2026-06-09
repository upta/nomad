# Copilot Instructions

This is a Godot 4.x C# cooperative multiplayer game using [SpacetimeDB](https://spacetimedb.com/) for server-authoritative networking, the [agentic-godot-validation](https://github.com/upta/agentic-godot-validation) kit for automated gameplay validation, and [agent-skills](https://github.com/addyosmani/agent-skills) for development workflows.

Reference: [untrailed](https://github.com/upta/untrailed) for working SpacetimeDB + Godot integration patterns.

## Project Structure

- `client/` — the Godot 4.x C# project root (`project.godot`, `.sln`, `.csproj` live here)
- `client/game/` — gameplay scenes and C# scripts
- `client/bootstrap/` — app entry point with test-mode routing
- `client/validation/` — harnesses, scenarios, and harness controllers
- `client/Db/` — generated SpacetimeDB client bindings (do not edit)
- `client/addons/agentic_godot_validation/` — symlinked validation runtime (do not edit directly)
- `server/` — SpacetimeDB server module
- `server/src/Tables/` — SpacetimeDB table definitions (`partial struct` with `[Table]` attribute)
- `server/src/Reducers/` — reducer methods on `partial class Module` (one file per reducer)
- `server/src/Types/` — shared enums and types
- `submodules/agentic_godot_validation/` — git submodule source for validation kit
- `submodules/agent_skills/` — git submodule source for agent-skills
- `tools/` — symlinked validation runner scripts
- `docs/game-design-document.md` — the full Game Design Document (GDD) for Nomad

## Key Conventions

- The app root routes between the game and the validation test bootstrap via `--test-mode` CLI flag
- Validation scenarios are JSON contracts in `client/validation/scenarios/`
- Harness scenes live in `client/validation/harnesses/` with controllers in `client/validation/scripts/harness_controllers/`
- Run scenarios with `./tools/run_scenario.ps1 -Scenario client/validation/scenarios/<name>.json -GodotExe <path>`
- Do not modify files under `client/addons/agentic_godot_validation/` — changes belong in the submodule repo
- Do not modify files under `client/Db/` — generated SpacetimeDB bindings
- All game code is C# (.NET 8.0). Do not create `.gd` GDScript files.
- Build client with `dotnet build` from the `client/` directory
- Build server with `spacetime build` from the `server/` directory
- Format both with `dotnet csharpier format .` in each directory
- All filenames PascalCase: `.cs` and `.tscn` both match class name
- SpacetimeDB tables: `partial struct` with `[SpacetimeDB.Table(Accessor = "...", Public = true)]`
- SpacetimeDB reducers: static methods on `public static partial class Module`, NO `On` prefix on lifecycle hooks
- SpacetimeDB updates: always use `Update(row with { Field = value })` — never partial struct updates

## Agent Skills Available

This project includes Copilot CLI skills for structured development workflows, organized by development phase:

### Define
| Skill | When to Use |
|-------|------------|
| `interview-me` | Exploring requirements through structured questioning |
| `idea-refine` | Workshopping and sharpening game/feature ideas |
| `spec-driven-development` | Designing features before building |

### Plan
| Skill | When to Use |
|-------|------------|
| `planning-and-task-breakdown` | Breaking work into verifiable tasks |

### Build
| Skill | When to Use |
|-------|------------|
| `test-driven-development` | Implementing logic, fixing bugs, changing behavior |
| `incremental-implementation` | Building in small, testable increments |
| `context-engineering` | Optimizing AI agent context for complex tasks |
| `source-driven-development` | Using existing code as reference for new work |
| `doubt-driven-development` | Resolving ambiguity before coding |
| `frontend-ui-engineering` | Building UI components and layouts |
| `api-and-interface-design` | Designing APIs and module interfaces |

### Verify
| Skill | When to Use |
|-------|------------|
| `browser-testing-with-devtools` | Testing web UIs with browser DevTools |
| `debugging-and-error-recovery` | Triaging and fixing bugs systematically |
| `godot-validation-testing` | Writing Godot validation scenarios and harnesses |
| `author-validation-scenario` | Creating new scenario JSON contracts |
| `debug-validation-failure` | Debugging failing validation scenarios |

### Review
| Skill | When to Use |
|-------|------------|
| `code-review-and-quality` | Multi-axis review before merging |
| `code-simplification` | Refactoring for clarity and maintainability |
| `security-and-hardening` | Security review and hardening |
| `performance-optimization` | Profiling and optimizing performance |

### Ship
| Skill | When to Use |
|-------|------------|
| `git-workflow-and-versioning` | Branching, commits, and version management |
| `ci-cd-and-automation` | CI/CD pipeline setup and automation |
| `deprecation-and-migration` | Managing deprecations and migrations |
| `documentation-and-adrs` | Writing docs and Architecture Decision Records |
| `shipping-and-launch` | Pre-launch checklist and release process |

### Meta
| Skill | When to Use |
|-------|------------|
| `using-agent-skills` | Guidance on when and how to use skills |
| `install-agentic-godot-validation` | Installing validation kit into a Godot project |
| `spacetime-db-reference` | SpacetimeDB CLI and C# SDK reference for server development |

### Agent Personas

Invoke these in Copilot Chat:
- `@code-reviewer` — Review PRs and code changes
- `@test-engineer` — Analyze test coverage, design test strategy
- `@security-auditor` — Audit code for security vulnerabilities

### Slash Commands (Claude Code) / Skill Shortcuts (Copilot)

These lifecycle entry points are available as skills in Copilot CLI/Chat. Invoke by name (e.g., "use spec"). In Claude Code, use the equivalent slash command (e.g., `/spec`).

| Shortcut Skill | Underlying Skills | Purpose |
|---|---|---|
| `spec` | `spec-driven-development` | Write a structured spec before coding |
| `plan` | `planning-and-task-breakdown` | Break work into ordered, verifiable tasks |
| `build` | `incremental-implementation`, `test-driven-development` | Implement incrementally with tests |
| `test` | `test-driven-development`, `godot-validation-testing` | Run tests and verify behavior |
| `review` | `code-review-and-quality` | Multi-axis code review |
| `code-simplify` | `code-simplification` | Refactor for clarity |
| `ship` | `shipping-and-launch` | Pre-launch parallel review with all three personas |

For Claude Code users, the `.github/commands/` directory provides slash command aliases for these same workflows.

## Validation-First Policy

- **Every gameplay code change MUST include validation scenarios.** No exceptions. If a change affects player-visible behavior, it needs a scenario proving it works.
- Humans play-test for fun, feel, and game design feedback — never for QA or bug detection. Automated validation catches bugs.
- If a bug is found during play-testing that should have been caught by validation, add the missing scenario as part of the fix.
- If the validation framework doesn't support a needed assertion, improve the framework first.

## Definition of Done

A feature is not done until a human can play-test it for game feel — not for whether it works. "It works" is the agent's job to prove before any human touches the game. Specifically:

1. **Validation scenarios exist** for the change — covering the intended behavior, not just the happy path.
2. **New scenarios pass.** Writing a scenario is not enough. Run it and confirm green.
3. **All existing scenarios still pass.** Run the full suite (`run_all_scenarios.ps1`) and confirm no regressions. If something broke, fix it before calling the work done.
4. **`git push origin`** at the end of every work batch.

If any of these are missing, the feature is not done. A human should never encounter a bug that automated validation could have caught.

## Validation Asset Rules

- Expose semantic game state through harness controllers using `get_observed_state()`
- Prefer `nodes`, `metrics`, and `signals` under `harness_state`
- Prefer `assert_value` and `assert_pipeline` over custom scenario operations
- Keep harnesses deterministic and minimal

## C# Conventions

- Use `PascalCase` for public members, `_camelCase` for private fields
- Prefer `var` for local variables when the type is obvious
- Use file-scoped namespaces where possible
- Use `""` instead of `String.Empty`
- Always simplify null checks and use null propagation
- Prefer primary constructors
- Follow [Microsoft C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)

### Class Member Ordering

Within a C# class, struct, or interface, elements should be positioned in this order:
1. Fields
2. Constructors
3. Delegates
4. Events
5. Enums
6. Interfaces
7. Properties
8. Methods
9. Structs
10. Classes

Within each group, order by access: `public` → `internal` → `protected` → `private`. Members with the same access should be ordered alphabetically.

**Exception:** Godot lifecycle methods (prefixed with `_`, e.g. `_Ready`, `_PhysicsProcess`) come before other methods as their own group.

## Build & Format (Always Before Done)

```powershell
# Client
dotnet build                    # in client/
dotnet csharpier format .       # in client/

# Server
spacetime build                 # in server/
dotnet csharpier format .       # in server/
```

## Comments

Comments should be rare and ONLY explain "why" not "what".

## Boundaries

- Always: Run validation scenarios after gameplay changes, write tests before fixes, build + format both projects before considering work done
- Ask first: Changes to validation framework submodule, adding NuGet packages, any modification to `docs/game-design-document.md`
- Never: Commit GDScript files, skip validation, remove failing scenarios
