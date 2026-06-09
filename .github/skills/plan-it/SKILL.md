---
name: plan-it
description: Break work into small verifiable tasks with acceptance criteria and dependency ordering. Use when you have a spec and need implementable tasks. Triggers on "/plan" or "create a plan".
---

# Plan

Shortcut entry point for `planning-and-task-breakdown`. Invokes the full skill workflow with focused directives.

## Workflow

First, invoke the `planning-and-task-breakdown` skill for the full methodology. Then follow these directives:

1. Read the existing spec (`SPEC.md` or equivalent) and the relevant codebase sections
2. Enter plan mode — read only, no code changes
3. Identify the dependency graph between components
4. Slice work vertically (one complete path per task, not horizontal layers)
5. Write tasks with acceptance criteria and verification steps
6. Add checkpoints between phases
7. Present the plan for human review

Save the plan to `tasks/plan.md` and the task list to `tasks/todo.md`.
