---
name: ship
description: Run the pre-launch checklist via parallel fan-out to specialist personas, then synthesize a go/no-go decision. Use before deploying to production or merging a major feature. Triggers on "/ship" or "prepare to ship".
---

# Ship

Shortcut entry point for `shipping-and-launch`. Invokes the full skill workflow and runs parallel persona fan-out.

## Workflow

First, invoke the `shipping-and-launch` skill for the full methodology.

Ship is a **fan-out orchestrator**. It runs three specialist personas in parallel against the current change, then merges their reports into a single go/no-go decision with a rollback plan.

### Phase A — Parallel fan-out

Run three personas concurrently:

1. **`@code-reviewer`** — Five-axis review (correctness, readability, architecture, security, performance) on staged changes or recent commits
2. **`@security-auditor`** — Vulnerability and threat-model pass. Check OWASP Top 10, secrets handling, auth/authz, dependency CVEs
3. **`@test-engineer`** — Analyze test coverage for the change. Identify gaps in happy path, edge cases, error paths, and concurrency scenarios

In Copilot CLI, invoke these via the `task` tool with the relevant agent type. In Copilot Chat, invoke them as `@code-reviewer`, `@security-auditor`, `@test-engineer`.

### Phase B — Merge

Once all three reports are back, synthesize:

1. **Code Quality** — Aggregate Critical/Important findings. Resolve duplicates.
2. **Security** — Promote Critical/High findings to launch blockers.
3. **Performance** — Cross-check findings.
4. **Infrastructure** — Env vars, migrations, monitoring, feature flags.
5. **Documentation** — README, ADRs, changelog.

### Phase C — Decision and rollback

Produce a single output:

```markdown
## Ship Decision: GO | NO-GO

### Blockers (must fix before ship)
- [Source persona: Critical finding + file:line]

### Recommended fixes (should fix before ship)
- [Source persona: Important finding + file:line]

### Acknowledged risks (shipping anyway)
- [Risk + mitigation]

### Rollback plan
- Trigger conditions: [what signals would prompt rollback]
- Rollback procedure: [exact steps]
- Recovery time objective: [target]

### Specialist reports (full)
- [code-reviewer report]
- [security-auditor report]
- [test-engineer report]
```

### Rules

1. The three Phase A personas run in parallel — never sequentially
2. Personas do not call each other. The main agent merges in Phase B
3. The rollback plan is mandatory before any GO decision
4. If any persona returns a Critical finding, the default verdict is NO-GO unless the user explicitly accepts the risk
5. **Skip the fan-out only if all of the following are true:** the change touches 2 files or fewer, the diff is under 50 lines, and it does not touch auth, security, data access, or config/env. Otherwise, default to fan-out.
