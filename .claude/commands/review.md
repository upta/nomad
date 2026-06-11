---
description: Five-axis code review with Nomad-specific axes — scene-first, reducer authority/determinism, validation coverage
---

Invoke the agent-skills:code-review-and-quality skill.

Review the current changes (staged or recent commits) across five axes, each with this project's specifics:

1. **Correctness** — Matches the plan task and GDD intent? Edge cases handled? Validation scenarios exist, pass, and their screenshots were reviewed?
2. **Readability** — Clear names? C# style per CLAUDE.md (file-scoped namespaces, member ordering, comments explain *why* only)?
3. **Architecture** — Scene-first respected (static structure in `.tscn`, tunables as `[Export]`s in scene/resource files, no `GD.Load` string paths)? `_Service/`/`_Scene/` split? AutoInject + GUIDE conventions? Shared state owned by SpacetimeDB, client-side only prediction/rendering?
4. **Security (server authority)** — Every reducer validates ownership/authorization via `ctx.Sender`? Reducers deterministic (no RNG, I/O, timers)? Client-supplied values bounded and validated server-side?
5. **Performance** — No per-frame allocations or full-table scans in hot paths? Network update rates throttled sensibly? No unbounded subscriptions?

Also verify: no edits under `client/Db/` or `client/addons/`, no `.gd` files, CSharpier-clean in client/ and server/.

Categorize findings as Critical, Important, or Suggestion, with file:line references and fix recommendations.
