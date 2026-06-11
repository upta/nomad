# Plan: SpacetimeDB-Aware Validation

**Status:** proposed (not started)
**Goal:** validation scenarios that exercise the real client ↔ server path — reducers, subscriptions, remote-entity rendering — so multiplayer behavior (the heart of the game) is provable in-engine, not just client-local logic.

## Current state (verified 2026-06-11)

- `--test-mode` routes around `DbManager.Connect()` entirely (`client/bootstrap/AppRoot.cs`), so no scenario today touches SpacetimeDB. Reducer wiring, subscription handling, and remote-entity rendering are unvalidatable.
- `DbManager.Connect()` hardcodes `http://localhost:3000` and database name `nomad`.
- The scenario runtime has no polling primitive — only fixed `wait_frames`. Network round-trips are nondeterministic in latency, so STDB scenarios need wait-until semantics or they will be flaky.
- `tools/` is a **symlink into the validation-kit submodule** — host-owned orchestration scripts cannot live there.
- Each anonymous `DbConnection` gets its own identity, and the server's `ClientConnected` reducer creates a player entity per identity — so two in-process connections naturally produce two players.

## Design principles

1. **Real SpacetimeDB, no fakes.** Faking `DbConnection` would drift from the SDK and miss the bugs this exists to catch. `spacetime` CLI + local server is fast enough.
2. **Poll, don't sleep.** All network-dependent assertions go through a `wait_until` op with a frame-budget timeout. `wait_frames` for physics only.
3. **Ephemeral databases.** Each STDB suite run publishes to a throwaway database name (`nomad_test_<timestamp>`), wiped on publish and deleted after. Dev data in `nomad` is never touched.
4. **Configuration via environment variables**, not CLI args — the scenario runner belongs to the submodule, and env vars (`OS.GetEnvironment` client-side) let the host orchestrate without forking the runner.
5. **Pure scenarios stay pure.** Existing client-local scenarios keep running with no server dependency; STDB scenarios live in a separate directory.

## Phase 1 — `wait_until` op (validation-kit submodule)

The one framework change everything else depends on; generically useful even without STDB.

- New step op in `runtime/drivers/scenario_driver.gd` + `runtime/verifiers/scenario_verifier.gd`:

  ```json
  { "op": "wait_until", "path": "harness_state.connection.player_count",
    "comparator": "gte", "expected": 2, "timeout_frames": 300, "poll_every_frames": 5 }
  ```

  Polls `get_observed_state()` until the comparison holds (pass) or the frame budget expires (fail, reporting the last observed value). Reuses the existing comparator set.
- Record final observed value + frames-waited in `summary.json`; capture a screenshot on timeout (like a checkpoint) for debugging.
- Update the kit's `author-validation-scenario` skill doc.
- Upstream to `upta/agentic-godot-validation`, bump the submodule here.

**Effort: small** (one op, mirrors existing assert_value plumbing).

## Phase 2 — Connected harnesses + orchestration (host project)

1. **Configurable `DbManager`:** read `NOMAD_STDB_URI` / `NOMAD_STDB_DB` env vars, falling back to current hardcoded values. Two-line change, zero behavior change for normal play.
2. **`ConnectedHarnessController` base** (`client/validation/scripts/harness_controllers/`): owns a `DbManager`, calls `Connect()` in `_Ready()`, and exposes connection state under `harness_state.connection.*` (`is_connected`, `data_ready`, `local_entity_id`, table counts, per-entity positions read from `Connection.Db`). Concrete harnesses subclass it and add feature state.
3. **Host-owned orchestration script** `scripts/run_stdb_scenarios.ps1` (new top-level `scripts/` dir, NOT the symlinked `tools/`):
   - ensure `spacetime start` is running (start as background process if not)
   - `spacetime publish nomad_test_<timestamp> --delete-data=always` + `spacetime generate` sanity check
   - set `NOMAD_STDB_URI`/`NOMAD_STDB_DB` env vars, invoke the kit's `run_scenario.ps1`/`run_all_scenarios.ps1` against `client/validation/scenarios_stdb/`
   - on exit: `spacetime delete nomad_test_<timestamp>` (always, in `finally`)
4. **First two scenarios** (`client/validation/scenarios_stdb/`):
   - `client_connects_and_spawns` — connect → `wait_until` `data_ready` → `wait_until` local player entity exists in the Db → assert a player node is rendered.
   - `movement_round_trip` — press `move_right` → `wait_until` the *server table* position (via harness Db read) advances past a contract threshold. This proves input → reducer call → table update → subscription delivery, the full loop.

**Effort: medium.** Main risk: test-mode boot path currently skips GUIDE context push and `Main` instantiation (`OnDataReady` flow) — the connected harness must instantiate enough of the real game path to be meaningful; expect some refactoring of `Main`/`AppRoot` seams (good forcing function — those seams are currently tangled, see the scene-first refactor task).

## Phase 3 — Multiplayer scenarios (puppet client, host project)

Prove remote-entity behavior with **two identities in one process**: the harness opens a second, independent `DbConnection` ("puppet"). Anonymous connections get distinct identities, so the server treats it as a second player.

- `PuppetClient` helper (validation-only, `client/validation/scripts/`): second `DbConnection` + simple scripted behaviors (e.g., "call `MoveEntity` rightward every N frames"), state exposed via the harness (`harness_state.puppet.*`).
- Scenario: `remote_player_renders_and_moves` — local client connects → puppet connects → `wait_until` remote node rendered → puppet moves → `wait_until` the rendered remote node's x advances (proves `EntityMover`/`SnapshotInterpolator` against real server traffic).
- Scripted-bot behaviors keep the framework untouched. If scenarios later need fine-grained puppet control from JSON steps, propose a generic `invoke_harness` op (call a named controller method) upstream — deferred until actually needed.

**Effort: medium.**

## Phase 4 — Suite integration + DoD

- `run_all_scenarios.ps1` already takes `-ScenarioDirectory`: pure suite runs as today; `scripts/run_stdb_scenarios.ps1` runs the STDB directory. A top-level `scripts/validate_all.ps1` runs both.
- Update `CLAUDE.md` DoD step 3 to "both suites pass" and the `validate-gameplay` skill with the STDB scenario how-to (`wait_until`, connected harness, puppet).
- The Stop hook needs no change (STDB runs emit the same `summary.json` artifacts).
- CI note: requires `spacetime` CLI + Godot on the runner; defer CI wiring until a pipeline exists.

**Effort: small.**

## Open questions

1. **Suite default:** should STDB scenarios run in the everyday DoD loop (slower: publish + connect per run) or only before push? Recommendation: in the loop — connection bugs are the expensive ones — and revisit if wall-clock exceeds ~2 min.
2. **Server lifecycle:** is it acceptable for the orchestration script to auto-start `spacetime start` as a background process on demand (and leave it running), or should it own start/stop per run? Recommendation: start-on-demand, leave running.
3. **`wait_until` upstreaming:** changes land in `upta/agentic-godot-validation` directly on main, or PR-and-review even though it's your repo?
4. **Identity/token reuse:** confirm the SDK doesn't persist and reuse a token between the main and puppet connections (would collapse them into one identity). If it does, the puppet needs an explicit fresh-token path. Verify early in Phase 3 — it changes the puppet design.

## Suggested order

Phase 1 → 2 are the value: after them, every server-touching feature gets real validation. 3 unlocks the multiplayer rendering path. 4 is bookkeeping. Each phase is independently shippable and validated by its own scenarios.
