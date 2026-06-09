# Validation Runtime Instructions

These instructions apply to files under `client/addons/agentic_godot_validation/**`.

**DO NOT EDIT FILES UNDER THIS PATH.** These files are symlinked from the `agentic-godot-validation` submodule. Changes must be made in the submodule repository and pulled in.

## What Lives Here

- Validation runtime scripts (test bootstrap, scenario runner, assertion engine)
- Built-in scenario operations (`assert_value`, `assert_pipeline`, `simulate_input`)
- Event recording and artifact generation

## How To Extend

If you need a new assertion or scenario operation that the framework doesn't support:

1. Do NOT modify files in this directory
2. Instead, implement the logic in your harness controller under `client/validation/scripts/harness_controllers/`
3. If the capability would benefit all projects, contribute it upstream to the `agentic-godot-validation` repo

## Reference

See the [agentic-godot-validation docs](https://github.com/upta/agentic-godot-validation) for the full scenario contract reference and API documentation.
