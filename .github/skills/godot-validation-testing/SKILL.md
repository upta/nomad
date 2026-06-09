---
name: godot-validation-testing
description: Bridges the `test-driven-development` skill with Godot-specific validation scenarios. Use when writing tests for Godot gameplay behavior, creating validation scenario contracts, or debugging Godot validation failures. Use when implementing game features that need automated validation.
---

# Godot Validation Testing

## Overview

This skill bridges general software testing methodology (`test-driven-development`) with Godot-specific automated validation (`author-validation-scenario`, `debug-validation-failure`). It provides the workflow for proving Godot gameplay behavior works — not just manually, but with repeatable, headless validation scenarios.

The goal: **humans play-test for fun and game feel, not for QA.** Automated validation catches regressions so you can iterate on game design instead of bug hunting.

## When to Use

- Adding or changing Godot gameplay logic (player movement, combat, UI, state machines)
- Writing validation scenarios for new features
- Debugging a validation scenario that fails unexpectedly
- Setting up a harness scene that exposes game state for assertions
- Any time you would normally write an integration/E2E test but are working in Godot

**When NOT to use:**
- Pure C# logic with no Godot dependencies — use standard C# unit tests (xUnit/NUnit) instead
- Documentation or config changes
- Changes to the validation framework itself (contribute upstream instead)

**Related skills:**
- `test-driven-development` — Core TDD methodology (Red-Green-Refactor, Prove-It Pattern)
- `author-validation-scenario` — Creating scenario JSON contracts
- `debug-validation-failure` — Debugging failing scenarios
- `incremental-implementation` — Building features in verifiable chunks

## The Godot TDD Cycle

The Red-Green-Refactor cycle from `test-driven-development` maps directly to Godot validation:

```
     RED                    GREEN                  REFACTOR
  Write a scenario     Implement gameplay      Clean up the code
  that validates  ──→  until scenario    ──→   and scenario    ──→  (repeat)
  the behaviour        passes                    contracts
       │                     │                       │
       ▼                     ▼                       ▼
  Scenario FAILS        Scenario PASSES         All scenarios PASS
```

### Step 1: RED — Write a Failing Scenario

Before writing gameplay code, define the expected behavior as a validation scenario. The scenario must fail until the feature is implemented.

**Example: Player jump mechanic**

```json
{
  "name": "player_jump_vertical",
  "description": "Player character jumps vertically when jump input is pressed while grounded",
  "timeout": 10,
  "setup": {
    "harness_scene": "res://validation/harnesses/player_harness.tscn"
  },
  "steps": [
    {
      "action": "wait_frames",
      "frames": 2,
      "description": "Let physics settle"
    },
    {
      "action": "simulate_input",
      "input": "jump",
      "pressed": true
    },
    {
      "action": "wait_frames",
      "frames": 30,
      "description": "Let jump physics play out"
    },
    {
      "action": "assert_value",
      "path": "harness_state.nodes.player.position.y",
      "operator": "less_than",
      "value": 0,
      "description": "Player Y position should be above ground (negative in Godot 2D)"
    }
  ]
}
```

### Step 2: GREEN — Implement Until Scenario Passes

Implement the gameplay feature. Run the scenario to confirm it passes:

```powershell
.\tools\run_scenario.ps1 -Scenario client\validation\scenarios\player_jump_vertical.json -GodotExe "C:\Godot\Godot_v4.3-stable_mono_win64\Godot_v4.3-stable_mono_win64.exe"
```

### Step 3: REFACTOR — Clean Up Both Code and Scenarios

With scenarios green, improve both the game code and the scenario contracts. Run all scenarios after each change.

## Writing Harness Controllers

Harness controllers expose game state to the validation framework. They are C# scripts attached to harness scenes.

### Basic Harness Controller

```csharp
using Godot;

public partial class PlayerHarnessController : Node
{
    private Player _player;

    public override void _Ready()
    {
        // The harness scene contains both the controller and the game scene
        _player = GetNode<Player>("../PlayerScene/Player");
    }

    /// <summary>
    /// Called each frame by the validation runtime.
    /// Returns the observed state as nested dictionaries.
    /// </summary>
    public Godot.Collections.Dictionary GetObservedState()
    {
        return new Godot.Collections.Dictionary
        {
            ["nodes"] = new Godot.Collections.Dictionary
            {
                ["player"] = new Godot.Collections.Dictionary
                {
                    ["position"] = new Godot.Collections.Dictionary
                    {
                        ["x"] = _player.GlobalPosition.X,
                        ["y"] = _player.GlobalPosition.Y,
                    },
                    ["health"] = _player.Health,
                    ["is_grounded"] = _player.IsOnFloor(),
                },
            },
            ["metrics"] = new Godot.Collections.Dictionary
            {
                ["enemies_defeated"] = GetTree().GetNodeCountInGroup("enemies_defeated"),
            },
            ["signals"] = new Godot.Collections.Dictionary
            {
                ["player_died"] = _playerDeathDetected,
            },
        };
    }
}
```

### Harness Scene Structure

A harness scene (`player_harness.tscn`) should:
1. Instance the game scene under test as a child
2. Attach a harness controller as a child
3. Be minimal — no extra nodes, no gameplay logic

## Scenario Contract Reference

### Available Operations

| Operation | Purpose |
|-----------|---------|
| `simulate_input` | Press/release an input action (jump, move_left, etc.) |
| `wait_frames` | Wait N physics frames before next assertion |
| `wait_seconds` | Wait N real-time seconds |
| `assert_value` | Assert a value at a path matches an expected value |
| `assert_pipeline` | Run a sequence of checks as a group (all-or-nothing) |

### Assertion Operators

| Operator | Meaning |
|----------|---------|
| `equals` | Exact equality |
| `not_equals` | Inequality |
| `greater_than` | `>` comparison |
| `less_than` | `<` comparison |
| `greater_or_equal` | `>=` comparison |
| `less_or_equal` | `<=` comparison |
| `contains` | String/collection contains value |
| `not_contains` | String/collection does not contain value |
| `between` | Value is in [min, max] range |
| `exists` | Path exists (non-null) |
| `not_exists` | Path does not exist (null) |

### State Path Convention

State paths use dot notation to traverse the `harness_state` dictionary:
```
harness_state.nodes.<node_name>.<property>
harness_state.metrics.<metric_name>
harness_state.signals.<signal_name>
```

## Running Scenarios

### Single Scenario

```powershell
.\tools\run_scenario.ps1 -Scenario client\validation\scenarios\<name>.json -GodotExe "<path\to\godot.exe>"
```

### All Scenarios

```powershell
.\tools\run_all_scenarios.ps1 -GodotExe "<path\to\godot.exe>"
```

### Debug Mode (slower, with verbose output)

```powershell
.\tools\run_scenario.ps1 -Scenario client\validation\scenarios\<name>.json -GodotExe "<path>" -Debug
```

## The Prove-It Pattern for Game Bugs

When a bug is found during play-testing:

1. **Reproduce it:** Write a scenario that triggers the exact bug conditions
2. **Confirm it fails:** Run the scenario — it should FAIL, proving the bug exists
3. **Fix the bug:** Implement the fix in game code
4. **Confirm it passes:** Run the scenario — it should PASS
5. **Run full suite:** Run all scenarios to check for regressions

This is the Godot equivalent of the Prove-It pattern from `test-driven-development`. Never fix a gameplay bug without first writing a scenario that reproduces it.

## Common Rationalizations

| Rationalization | Reality |
|---|---|
| "This is too simple to need a scenario" | Simple features cause complex bugs. The scenario documents expected behavior. |
| "I tested it in the Godot editor" | Manual testing doesn't persist. Tomorrow's change breaks it with no way to know. |
| "Scenarios slow me down" | They slow you down now. They speed you up every time you refactor or add features. |
| "The scenario is flaky" | Flaky scenarios are a design smell. Fix the scenario, don't skip it. Use wait_frames over wait_seconds for determinism. |
| "I'll write the scenario after the feature" | You won't. And post-hoc scenarios test implementation, not behavior. |

## Red Flags

- Gameplay code added without a corresponding scenario
- A scenario JSON that doesn't reference any assertions
- Scenarios that pass on first run (may not be testing meaningful behavior)
- Bug fixes without reproduction scenarios
- Skipping scenarios to make the suite pass
- Using `wait_seconds` instead of `wait_frames` for game logic assertions
- Harness controllers that don't expose semantic state (raw node property dumps)

## Verification

After implementing any gameplay feature:

- [ ] At least one validation scenario exists for the new behavior
- [ ] The scenario covers both the happy path and at least one edge case
- [ ] All new scenarios pass when run headlessly
- [ ] All existing scenarios still pass (`run_all_scenarios.ps1`)
- [ ] The harness controller exposes semantic state (not raw implementation details)
- [ ] Bug fixes include a reproduction scenario that failed before the fix
- [ ] Scenario timeout is appropriate (start with 10s, increase if legitimately needed)

## See Also

- `test-driven-development` — Core TDD methodology and philosophy
- `author-validation-scenario` — Detailed guide for writing scenario JSON contracts
- `debug-validation-failure` — Workflow for diagnosing why a scenario fails
- `incremental-implementation` — Building features in scenario-verifiable increments
- [Agentic Godot Validation Kit docs](https://github.com/upta/agentic-godot-validation) — Full API reference
