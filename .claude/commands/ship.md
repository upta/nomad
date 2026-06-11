---
description: Definition-of-Done gate — parallel specialist fan-out plus direct DoD verification, then go/no-go
---

`/ship` here is a **Definition of Done gate** (CLAUDE.md), not a production-launch checklist — no OWASP/CVE/Web-Vitals theater. It keeps the fan-out pattern from agent-skills:shipping-and-launch with personas repointed at what actually bites this project.

## Phase A — Parallel fan-out

Spawn three subagents concurrently (all Agent tool calls in a single message — sequential calls defeat the purpose):

1. **code-reviewer** — five-axis review of the work batch per the project `/review` command: correctness, readability, architecture (scene-first, service split, AutoInject/GUIDE), server authority, performance.
2. **security-auditor** — server-authority audit instead of web OWASP: every reducer checks ownership/authorization (`ctx.Sender`), reducers are deterministic (no RNG/I/O/timers), client-authoritative movement is bounded server-side, no trust of client-supplied state beyond design intent, no secrets committed.
3. **test-engineer** — validation-scenario coverage analysis: does every behavior change have a scenario covering intended behavior (not just happy path)? Networked changes covered in `client/validation/scenarios_stdb/`? Any assertion that could pass while rendering is broken (screenshots reviewed)?

Personas do not call each other; the main agent merges.

## Phase B — Verify the DoD directly (main context)

1. `./scripts/validate_all.ps1` — both suites green, zero regressions
2. Game boots clean: real game headless ≥10s with zero `ERROR:` lines (run-game skill)
3. `dotnet build` + `spacetime build` + `dotnet csharpier format .` clean in client/ and server/
4. tasks/plan.md and tasks/todo.md updated; conventional commits in place

## Phase C — Decision

```markdown
## Ship Decision: GO | NO-GO

### Blockers (Critical findings or DoD failures)
### Recommended fixes (Important findings)
### Acknowledged risks (shipping anyway)
### Specialist reports (full)
```

Any Critical finding or DoD failure defaults to NO-GO unless the user explicitly accepts the risk. On GO: `git push origin`.
