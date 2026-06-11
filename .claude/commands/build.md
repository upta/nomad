---
description: Implement the next task incrementally — prove it in-engine, satisfy the DoD, commit
---

Invoke the agent-skills:incremental-implementation skill. In this project the proof artifact is an **in-engine validation scenario**, not a unit test — use the validate-gameplay skill for the scenario workflow.

Pick the next pending task from tasks/plan.md. For each task:

1. Read the task's acceptance criteria and the GDD sections it cites
2. Load relevant context: existing scenes/resources/code, server tables and reducers, untrailed patterns where the task references them
3. Author validation scenario(s) describing the intended behavior — confirm they FAIL before implementing
4. Implement incrementally — scene-first (`.tscn`/`.tres` are the authoring surface per client/CLAUDE.md), C# only, shared state owned by SpacetimeDB
5. Run the new scenarios until green, then **visually review the checkpoint screenshots** in the run artifacts — numeric assertions can pass while rendering is broken
6. Run both suites (`./scripts/validate_all.ps1`) to catch regressions; networked behavior needs coverage in `client/validation/scenarios_stdb/`
7. Verify the rest of the Definition of Done: `dotnet build` + `spacetime build` clean, `dotnet csharpier format .` in client/ and server/, and for startup-path changes the game boots clean (run-game skill)
8. Commit with a conventional message; mark the task complete in tasks/plan.md and tasks/todo.md

If any step fails, follow the agent-skills:debugging-and-error-recovery skill. Never delete or weaken a failing scenario to get the suite green.
