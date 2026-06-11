---
name: validate-gameplay
description: Author and run in-engine validation scenarios for Nomad gameplay. Use for ANY change to client gameplay code (client/game/**) or server gameplay logic that affects player-visible behavior, to reproduce a bug before fixing it, or to debug a failing scenario. Covers harness controllers, the scenario JSON contract format, runner scripts, and artifact/screenshot review.
---

# Validating Gameplay In-Engine

Validation scenarios run the real game code headlessly in Godot, simulate input, assert on observed state at checkpoints, and capture screenshots. They are the proof that a change works — humans play-test only for fun and feel, never to find bugs.

**This file is the authoritative format reference.** It matches the runtime in `client/addons/agentic_godot_validation/` (the kit's own docs and older skill files have drifted — trust this and the existing scenarios in `client/validation/scenarios/`).

## Workflow (red → green → suite → screenshots)

1. **Red:** before (or alongside) implementing, write a scenario asserting the new behavior. Run it — it must FAIL, proving it tests something real.
2. **Green:** implement until the scenario passes.
3. **Suite:** `./tools/run_all_scenarios.ps1` — every existing scenario must still pass.
4. **Look:** Read the checkpoint screenshots from the run artifacts and confirm the scene visually shows what the assertions claim. Numeric assertions pass just fine while rendering is invisible or broken — your eyes are the last assertion.

**Bug fixes (prove-it):** write a scenario reproducing the bug → run it, confirm FAIL → fix → confirm PASS → full suite. Never fix a gameplay bug without a reproduction scenario.

**When not to use:** pure C# logic with no Godot/engine dependency — plain unit tests are fine. Doc/config changes.

## The three pieces

A validation lives in three host-owned files (never touch `client/addons/agentic_godot_validation/` itself):

1. **Harness controller** — `client/validation/scripts/harness_controllers/<Name>HarnessController.cs`
2. **Harness scene** — `client/validation/harnesses/<Name>Harness.tscn`
3. **Scenario contract** — `client/validation/scenarios/<snake_case_name>.json`

### 1. Harness controller

A C# node exposing semantic game state through **`get_observed_state()`** (exact snake_case name — the GDScript runtime calls it by string):

```csharp
namespace Nomad.Validation.HarnessControllers;

using Godot;

public partial class MovementHarnessController : Node2D
{
    [Export]
    public NodePath ActorPath { get; set; } = new("Player");

    private Node2D _actor = null!;

    public override void _Ready()
    {
        _actor = GetNode<Node2D>(ActorPath);
    }

    public Godot.Collections.Dictionary get_observed_state()
    {
        return new Godot.Collections.Dictionary
        {
            ["actor_position"] = _actor.GlobalPosition,
            // nested dictionaries and arrays are addressable from scenarios
            // as harness_state.<key>.<subkey> / harness_state.<list>.<index>.<key>
        };
    }
}
```

Expose *semantic* state (positions, counts, ids, flags), not raw node dumps. Keep it deterministic — seed any RNG.

### 2. Harness scene

Minimal `.tscn` that wires the controller to the scene under test — no extra nodes, no gameplay logic. Give it **visible** content (sprites, ColorRects, labels) so checkpoint screenshots are meaningful:

```
[gd_scene format=3]

[ext_resource type="Script" path="res://validation/scripts/harness_controllers/MovementHarnessController.cs" id="1"]

[node name="MovementHarness" type="Node2D"]
script = ExtResource("1")
ActorPath = NodePath("PlayerActor")

[node name="PlayerActor" type="Node2D" parent="."]
position = Vector2(0, 0)
```

(Never invent `uid://` values — omit the attribute; Godot assigns one later.)

### 3. Scenario contract

```json
{
  "scenario_id": "player_moves_right",
  "version": 3,
  "description": "Player x increases past threshold while move_right is held.",
  "harness_scene": "res://validation/harnesses/MovementHarness.tscn",
  "done_contract": { "min_rightward_delta": 10.0 },
  "steps": [
    { "op": "load_harness", "scene": "res://validation/harnesses/MovementHarness.tscn" },
    { "op": "wait_frames", "frames": 5 },
    { "op": "checkpoint", "name": "before" },
    { "op": "press_action", "action": "move_right" },
    { "op": "wait_frames", "frames": 60 },
    { "op": "release_action", "action": "move_right" },
    { "op": "checkpoint", "name": "after" },
    {
      "op": "assert_pipeline",
      "sources": {
        "before_x": { "kind": "checkpoint", "checkpoint": "before", "path": "harness_state.actor_position.x" },
        "after_x": { "kind": "checkpoint", "checkpoint": "after", "path": "harness_state.actor_position.x" },
        "threshold": { "kind": "contract", "path": "done_contract.min_rightward_delta" }
      },
      "pipeline": [
        { "op": "subtract", "inputs": ["after_x", "before_x"], "as": "delta_x" }
      ],
      "assert": { "actual": "delta_x", "comparator": "gte", "expected_source": "threshold" }
    },
    { "op": "quit" }
  ]
}
```

#### Supported step ops (complete list)

| Op | Fields | Notes |
|---|---|---|
| `load_harness` | `scene` | First step; `res://` path to the harness scene |
| `wait_frames` | `frames` | Physics frames (60/s). Always frames, never wall-clock — there is no `wait_seconds` |
| `wait_until` | `path`, `comparator`, `expected`, `timeout_frames` (default 300), `poll_every_frames` (default 1) | Polls live `get_observed_state()` until the condition holds; fails with `assertion_failure` at timeout (captures a debug screenshot). **Use this, never a generous `wait_frames`, for anything with nondeterministic timing — network round-trips, SpacetimeDB subscription updates, deferred spawns.** Path-not-found while polling counts as "not yet" |
| `checkpoint` | `name` | Snapshots `harness_state` AND captures `screenshots/<name>.png` |
| `press_action` / `release_action` | `action` | Drives a Godot **InputMap** action (not GUIDE). The action must be registered in `EnsureInputActions()` in `client/bootstrap/AppRoot.cs` |
| `assert_value` | `checkpoint`, `path`, `comparator`, `expected` | Compare one checkpointed value against a literal |
| `assert_pipeline` | `sources`, `pipeline`, `assert` | Compute across checkpoints/contract values, then compare |
| `quit` | — | Last step |

- **Comparators:** `eq`, `neq`, `gt`, `gte`, `lt`, `lte`, `contains`, `starts_with`, `ends_with`
- **Pipeline ops:** `add`, `subtract`, `abs` (each: `inputs` array of source/derived names, `as` result name)
- **Source kinds:** `checkpoint` (`checkpoint` + `path` into `harness_state...`) and `contract` (`path` into `done_contract...`)
- State paths are dot-separated; list elements by index: `harness_state.room_types.0.room_id`

## SpacetimeDB scenarios (`client/validation/scenarios_stdb/`)

Networked behavior — reducers, subscriptions, remote entities — is validated against a **real local SpacetimeDB** with an ephemeral per-run database. Anything touching the server gets a scenario here, not in the pure suite.

- **Harness:** `ConnectedGameHarness.tscn` (single client) or `MultiplayerGameHarness.tscn` (adds an in-process `PuppetClient` — a second anonymous-identity connection that drives its own entity via reducers). Both use `ConnectedGameHarnessController`, which connects a real `DbManager`, instantiates the real `Main` + `Player`, and bridges InputMap actions to synthetic key events so GUIDE input works.
- **State paths:** `harness_state.connection.*` (`data_ready`, `local_entity_id`, `player_count`, `local_entity.{x,y,displacement_from_initial,distance_to_node}`), `harness_state.game.*` (`player_exists`, `player.displacement_from_start`, `remote_count`, `remote_node.*`, `remote_node_recreations`), `harness_state.puppet.*`.
- **Timing rule:** every network-dependent condition uses `wait_until`, never a guessed `wait_frames`. Assert displacements/convergence, not absolute coordinates — hull layout changes must not break netcode scenarios.
- **Config:** `DbManager` reads `NOMAD_STDB_URI` / `NOMAD_STDB_DB` env vars (the runner sets them) and `--client <id>` / `NOMAD_CLIENT_ID` for per-identity token files.

```powershell
./scripts/run_stdb_scenarios.ps1                                  # STDB suite (publishes ephemeral DB, deletes after)
./scripts/run_stdb_scenarios.ps1 -Scenario client/validation/scenarios_stdb/<name>.json
./scripts/run_stdb_scenarios.ps1 -KeepDatabase                    # keep the DB for inspection
```

## Running

```powershell
./tools/run_scenario.ps1 -Scenario client/validation/scenarios/<name>.json   # one pure scenario
./tools/run_all_scenarios.ps1                                               # pure suite
./scripts/validate_all.ps1                                                  # pure + STDB suites (Definition of Done)
./tools/run_all_scenarios.ps1 -RepeatCount 3                                # flakiness check
```

`godot.exe` is on PATH; `GODOT_EXE` overrides. Exit code 0 = pass. Output ends with `RESULT {json}` and `ARTIFACTS <path>`.

### Artifacts — always review after a run

Each run writes `client/artifacts/<scenario_id>/<timestamp>/`:

- `summary.json` — pass/fail per step with observed values (read this first on failure)
- `screenshots/<checkpoint>.png` — **Read these and visually verify the scene** (suite runs aggregate under `client/artifacts/suites/<timestamp>/suite.json`)
- `event_log.json`, `scene_tree.json`, `console.log` — step timeline, node tree at end, engine output

## Debugging a failing scenario

1. Read `summary.json` — which step, what was observed vs expected.
2. `console.log` — C# exceptions, missing scene/script load errors.
3. `scene_tree.json` — did the harness actually build the node structure you assumed?
4. Common causes: action not registered in `InputMap` (fix `EnsureInputActions()`); wrong `harness_state` path (log the dictionary or check `summary.json`'s observed snapshot); too few `wait_frames` for physics to settle (settle ~5 frames after load before the first checkpoint); harness `res://` path typo.
5. A flaky scenario is a design smell — make it deterministic (frame waits, seeded RNG), never delete or loosen it to go green.

## Quality bar

- Cover intended behavior and at least one edge case, not just the happy path
- A scenario that passes on its very first run probably isn't testing the new behavior — confirm it can fail
- Harness exposes semantic state and visible-on-screen indicators
- Scenario files are snake_case; harness files PascalCase
