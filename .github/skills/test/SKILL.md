---
name: test
description: Run TDD workflow — write failing tests, implement, verify. For bugs, use the Prove-It pattern. Use when proving behavior works or fixing bugs. Triggers on "/test" or "run tests".
---

# Test

Shortcut entry point for `test-driven-development` and `godot-validation-testing`. Invokes both skills with focused directives.

## Workflow

First, invoke the `test-driven-development` and `godot-validation-testing` skills for the full methodology. Then follow these directives:

### For new features:
1. Write tests that describe the expected behavior (they should FAIL)
2. Implement the code to make them pass
3. Refactor while keeping tests green

### For bug fixes (Prove-It pattern):
1. Write a test that reproduces the bug (must FAIL)
2. Confirm the test fails
3. Implement the fix
4. Confirm the test passes
5. Run the full test suite for regressions

### Godot-Specific Notes

In this project, tests are Godot validation scenarios. Run scenarios with:

```powershell
.\tools\run_scenario.ps1 -Scenario src\validation\scenarios\<name>.json -GodotExe "<path>"
.\tools\run_all_scenarios.ps1 -GodotExe "<path>"
```

For browser-related issues on web exports, also invoke `browser-testing-with-devtools`.
