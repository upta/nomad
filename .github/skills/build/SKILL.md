---
name: build
description: Implement the next task incrementally — build, test, verify, commit. Use when implementing tasks from a plan. Triggers on "/build" or "build the next task".
---

# Build

Shortcut entry point for `incremental-implementation` and `test-driven-development`. Invokes both skills with focused directives.

## Workflow

First, invoke the `incremental-implementation` and `test-driven-development` skills for the full methodology. Then follow these directives:

Pick the next pending task from the plan. For each task:

1. Read the task's acceptance criteria
2. Load relevant context (existing code, patterns, types)
3. Write a failing test for the expected behavior (RED)
4. Implement the minimum code to pass the test (GREEN)
5. Run the full test suite to check for regressions
6. Run the build to verify compilation
7. **Update `tasks/plan.md`** — mark the task `[x]` and reflect any scope changes
8. Commit with a descriptive message, **including `tasks/plan.md` and `tasks/todo.md`** in the commit
9. Mark the task complete and move to the next one

If any step fails, follow the `debugging-and-error-recovery` skill.

### Verify / Review / Ship Gates

After completing all tasks in a phase or checkpoint:

- **Verify** — run the full test suite and build one final time. Use `test` skill if scenarios exist.
- **Review** — invoke the `review` skill for a five-axis review before merging. At minimum, self-review the diff for correctness and completeness.
- **Ship** — for major phases (multi-task, user-facing changes), invoke the `ship` skill to fan out to `@code-reviewer`, `@security-auditor`, and `@test-engineer` personas. Skip the fan-out only if the change touches ≤2 files and is under 50 lines with no auth/security/data changes.

### Godot-Specific Notes

In this project, "tests" mean Godot validation scenarios. Use `godot-validation-testing` to bridge TDD methodology with Godot scenario contracts. Run scenarios with:

```powershell
.\tools\run_scenario.ps1 -Scenario client\validation\scenarios\<name>.json -GodotExe "<path>"
```
