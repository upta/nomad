---
description: Validation-first workflow — author failing in-engine scenarios, implement, verify. For bugs, reproduce first.
---

Apply the discipline of agent-skills:test-driven-development, with this project's substitution: the test artifact is an **in-engine validation scenario** (validate-gameplay skill), not a unit test.

For new behavior:

1. Author scenario(s) describing the intended behavior (author-validation-scenario skill) — confirm they FAIL
2. Implement until they pass
3. Visually review the checkpoint screenshots in the run artifacts — numeric assertions can pass while rendering is broken

For bug fixes (reproduce-first):

1. Write a scenario that reproduces the bug — confirm it FAILS
2. Implement the fix
3. Confirm the scenario passes
4. Run both suites (`./scripts/validate_all.ps1`) for regressions

Rules:

- Anything touching reducers, subscriptions, or networked behavior needs coverage in `client/validation/scenarios_stdb/` (puppet-client multiplayer scenarios)
- A failing scenario with a non-obvious cause → debug-validation-failure skill (read summary.json, event logs, scene trees, screenshots)
- If the framework can't assert what you need, improve the framework (ask first — it's a submodule). Skipping validation is never the fallback.
