# Implementation Plan: Parallel Validation Test Runner

> Scope note: this is a **tooling** plan for the validation runner, kept separate from the live
> game-feature plan in [`tasks/plan.md`](plan.md) / [`tasks/todo.md`](todo.md). Do not merge the two.
> Companion task list: [`tasks/validation-parallelization-todo.md`](validation-parallelization-todo.md).

## Overview

The validation suites run scenarios **serially**: one Godot process at a time, `Start-Process … -Wait`.
The pure suite is ~170s for 45 scenarios (~3.8s each); the SpacetimeDB (STDB) suite runs another 45
scenarios, each preceded by a `spacetime publish` to its own ephemeral DB, so it dominates total
wall-clock. `validate_all.ps1` runs both back-to-back. On a 16-core box this leaves ~14 cores idle.

Goal: run multiple scenarios **concurrently** to cut suite wall-clock several-fold, without weakening
isolation, breaking artifact tooling, or introducing timing flakiness — and without editing the
validation-kit submodule or game code.

## Feasibility (investigation findings)

Confirmed by reading the kit runtime, the runner scripts, and `DbManager`:

- **Input is in-process.** The driver injects `InputEventAction` via `Input.parse_input_event()`
  (`…/runtime/drivers/scenario_driver.gd:330`). Each Godot process owns its `Input` singleton, so
  concurrent windows do **not** steal each other's keystrokes. No OS-level SendInput / focus.
- **Screenshots are in-process viewport capture** (`get_viewport().get_texture().get_image().save_png()`,
  `…/runtime/inspector/runtime_inspector.gd:239`) and don't require window focus — **but headless mode
  skips them** (`runtime_inspector.gd:240`). Since the Definition of Done requires *visual* screenshot
  review, runs must stay **windowed**, not `--headless`.
- **Artifacts are already per-run** (`artifacts/<scenario_id>/<timestamp>/`, `run_scenario.ps1:99`).
  No path collisions between concurrent runs.
- **STDB isolation already exists per-scenario**: each scenario gets its own DB name
  (`run_stdb_scenarios.ps1:89`). The client reads `NOMAD_STDB_DB` / `NOMAD_STDB_URI` from the
  environment (`DbManager.cs:44`), and the identity token file is `.nomad-<clientId>` where `clientId`
  comes from `--client` or `NOMAD_CLIENT_ID` (`DbManager.cs:40,63-71`).
- **Toolchain supports it**: pwsh 7.6.0 (`Start-Process -Environment` per-child env block, added 7.4),
  16 cores.

### The two real coupling points to solve

1. **Shared global env var.** The current STDB runner passes the DB name via `$env:NOMAD_STDB_DB`, which
   is process-global and would race under in-process parallelism. Fix: give each child process its **own
   environment block** (clone parent env + overlay `NOMAD_STDB_URI` / `NOMAD_STDB_DB` /
   `NOMAD_CLIENT_ID`) via `Start-Process -Environment`. No game-code change needed.
2. **Shared identity token file.** All STDB scenarios currently run as the default `main` identity →
   `.nomad-main`. Two concurrent writers collide. Fix: a **unique `NOMAD_CLIENT_ID` per worker slot**
   (e.g. `test-<slot>`), isolating each `.nomad-<id>` token file.

Everything else (pure suite) needs only a worker pool + single end-of-suite prune.

## Architecture Decisions

- **Upstream the generic machinery into the kit submodule** (decided). The reusable parts — the worker
  pool, `-MaxParallel`, and `suite.json` aggregation — go into the kit's `run_all_scenarios.ps1`, and a
  small **generic per-run passthrough** (env overlay + extra Godot user-args) goes into the kit's
  `run_scenario.ps1` so callers can inject identity/DB selection without the kit knowing Nomad specifics.
  The **Nomad-specific STDB lifecycle** (`spacetime publish`, `nomad-test-*` DB naming, delete) stays
  project-owned in `scripts/run_stdb_scenarios.ps1`, building on the improved kit primitive. This keeps
  the kit game-agnostic while giving a single source of pool/aggregation logic.
  - *Submodule workflow:* kit edits live in `submodules/agentic_godot_validation/` (symlinked into
    `tools/`). They must be committed in the **kit repo** (`upta/agentic-godot-validation`) and the
    submodule pointer bumped in this repo; re-run `./setup.ps1` if symlinks need recreating.
- **Per-child environment isolation, not `ForEach-Object -Parallel` + `$env:`.** Each scenario launches
  as its own pwsh/Godot child with a cloned-and-overlaid env block. This sidesteps the process-global
  `$env:` race entirely.
- **Windowed + `WINDOW_FLAG_NO_FOCUS` (the enabling runtime fix).** Headless skips screenshots (DoD)
  and voids render-frame-dependent scenarios, so runs stay windowed. But naive windowed parallel is
  unsafe: concurrent instances steal OS focus and Godot releases held input on focus-out (Task 0
  blocker). Fix: the test bootstrap opens the window with `WINDOW_FLAG_NO_FOCUS`, so instances never
  contend for focus while still rendering. Validated: held-input + modal/GUI-focus scenarios both pass
  4-wide, and the full 45-scenario suite is 45/45 at 4-wide with correct screenshots.
- **Single prune at suite end.** Children run with `-SkipArtifactPrune` (already supported); the
  orchestrator prunes once after all workers finish, avoiding filesystem races on `artifacts/index.json`.
- **STDB = one sequential publisher feeding a parallel run pool.** `spacetime publish` shares the cargo
  target dir and isn't safe to run concurrently, so publishing stays sequential (fast when the build is
  warm) and *pipelines* into the parallel Godot-run pool, hiding publish latency behind runs. Each DB is
  deleted right after its run.
- **Tunable concurrency, default `-MaxParallel 4`.** Safe while the machine is in use (~4 windowed
  Godot+C# instances on the 16-core box); push higher via the flag. `-MaxParallel 1` reproduces today's
  serial behavior for debugging.
- **Same `suite.json` schema.** Aggregation must emit the existing suite/summary shapes so the
  `debug-validation-failure` skill and `prune_artifacts.ps1` manifest generation keep working unchanged.

## Dependency Graph

```
kit run_scenario.ps1  (+ generic env/user-arg passthrough — small kit change)
        │
        ▼
Task 0  baseline + parallel-launch spike  ← highest-risk unknowns, do first
        │
        ▼
Task 1  -MaxParallel in kit run_all_scenarios.ps1  ──►  (lowest-risk full vertical slice)
        │            proves: worker pool, env-block launch, RESULT capture,
        │            suite.json aggregation, single prune
        ▼
Task 2  per-child STDB isolation (env block: unique DB + CLIENT_ID, token-file isolation)
        │
        ▼
Task 3  stdb parallel orchestrator (sequential publisher → parallel run pool → delete DB)
        │
        ▼
Task 4  validate_all switches to parallel (with -MaxParallel passthrough + serial fallback)
        │
        ▼
Task 5  flake guard (repeat runs, confirm no NEW flakes vs serial; tune default)
        │
        ▼
Task 6  docs (CLAUDE.md command table, validate-gameplay skill, README)
```

Build order follows the graph bottom-up. Each task leaves both suites runnable and green.

## Task List

### Phase 0: De-risk

#### Task 0: Baseline timings + parallel-launch spike
**Description:** Capture authoritative serial baselines for both suites, then validate the one mechanism
the whole plan rests on: launching Godot scenarios concurrently with isolated per-child env blocks.
This is read/measure + a throwaway spike, not the orchestrator.

**Acceptance criteria:**
- [ ] Recorded serial wall-clock for the pure suite and the STDB suite (and `validate_all`), with core/RAM headroom noted.
- [ ] A verified snippet launches ≥4 pure scenarios concurrently via `Start-Process -Environment` where each child sees a distinct `NOMAD_CLIENT_ID`, `godot.exe` still resolves on PATH (env overlay did not wipe PATH), and all 4 produce screenshots.
- [ ] Confirmed `Start-Process -Environment` merge semantics (overlay vs replace) and the chosen env-cloning approach written down.

**Verification:** Inspect the 4 spike artifact dirs — 4 distinct `summary.json` (status pass) + non-empty screenshots; no cross-talk.

**Dependencies:** None. **Files touched:** none persisted (spike is throwaway; record findings in this plan). **Scope:** S.

**Results (2026-06-13) — DONE, and it surfaced a blocker:**
- ✅ Concurrency works mechanically: 4 pure scenarios ran in **~5s wall-clock** (vs ~15s serial → ~3× at 4-wide). `Start-Process -Environment` **overlays** (cloned parent env + overlay), PATH preserved, `godot.exe` resolved, all 4 produced screenshots, no cross-talk.
- ❌ **BLOCKER: windowed parallel breaks held-input scenarios.** `player_moves_right` failed **3/4** runs at 4-wide windowed — actor moved exactly **0px** (`delta_x=0`). Same scenario **headless 4-wide passed 12/12**. Root cause: concurrent windows steal OS focus; Godot **releases held input actions on focus-out**, so an unfocused instance holding a move action registers zero movement. Non-input scenarios (load/render/state) never flaked.
- ⚠️ Headless is **not** an acceptable workaround: it skips screenshots (DoD visual review) and renders point-10 render/idle-frame-dependent scenarios meaningless.
- **Consequence:** the "windowed, never headless" assumption stands, but **naive windowed parallel is unsafe**. A focus/input fix in the validation **runtime** is now a prerequisite (new Task, pending direction — see Decision below). Recorded in memory `validation-scenario-driving-gotchas` (point 11).

### Phase 1: Pure suite parallel

#### Task 1: `-MaxParallel` worker pool in the kit's `run_all_scenarios.ps1`
**Description:** Add a worker pool to the kit's `run_all_scenarios.ps1`: run scenarios through a pool of
`run_scenario.ps1 -SkipArtifactPrune` children (each its own process), capture each child's
`RESULT`/`ARTIFACTS` output, aggregate into the **same** `suite.json` schema it already emits, and prune
once at the end. Add the generic per-run passthrough to `run_scenario.ps1` (env overlay + extra Godot
user-args) that Task 3 will use. Exposes `-MaxParallel` (default 4; 1 = today's serial path).

**Acceptance criteria:**
- [ ] Pure suite runs green via the new script and is meaningfully faster than serial (target: ≥2.5× at the chosen concurrency).
- [ ] Emits a `suite.json` whose schema matches `run_all_scenarios.ps1` output (same keys: `suite_status`, `scenario_aggregate`, `final_exit_code`, …); `prune_artifacts.ps1` manifest generation still succeeds.
- [ ] Per-scenario screenshots exist and are visually correct (spot-check ≥3).
- [ ] `-MaxParallel 1` reproduces serial behavior and result.
- [ ] Exit code is non-zero iff any scenario failed.

**Verification:** `./tools/run_all_scenarios.ps1 -MaxParallel 4`; diff its `suite.json` keys against a `-MaxParallel 1` run; open 3 screenshots.

**Dependencies:** Task 0. **Files touched (kit submodule):** `tools/run_all_scenarios.ps1` (+ `bootstrap/test_bootloader.gd` NO_FOCUS fix from Task 0). `run_scenario.ps1` did **not** need changes — child processes inherit env transparently, so STDB isolation (Task 3) works via a per-child env block at launch. **Scope:** M.

**Results (2026-06-13) — DONE:**
- ✅ Added `-MaxParallel` (default 4) to `run_all_scenarios.ps1`: a throttled `ForEach-Object -Parallel` run phase feeds the unchanged serial aggregation; `-MaxParallel 1` keeps the exact serial path. `suite.json` schema preserved (+ new `max_parallel` field).
- ✅ Full pure suite: **45/45 pass, 52.2s at 4-wide** vs ~170s serial → **~3.2×**. Zero failed, zero flaky.
- ✅ Screenshots correct under NO_FOCUS (spot-checked `player_moves_right` actor sprite + `ship_layout_renders` full ship — rooms/labels/breakers/terminals all render).

### Checkpoint: Pure suite parallel
- [ ] Pure suite green in parallel, faster, valid `suite.json`, screenshots reviewed.
- [ ] Serial fallback (`-MaxParallel 1`) verified.
- [ ] Review with human before tackling STDB.

### Phase 2: STDB suite parallel

#### Task 2: Per-child STDB isolation
**Description:** Establish the per-worker isolation contract used by the STDB orchestrator: each child
gets a cloned env block overlaying a unique `NOMAD_STDB_DB`, the shared `NOMAD_STDB_URI`, and a unique
`NOMAD_CLIENT_ID` (→ isolated `.nomad-<id>` token file). Verify the secondary `PuppetClient` connections
used by multiplayer scenarios also stay isolated.

**Acceptance criteria:**
- [ ] Two STDB scenarios run concurrently against distinct DBs with no state leakage (assertions reference only their own DB's state).
- [ ] No two concurrent workers share a `.nomad-<id>` token file; confirm token-file location and that unique `NOMAD_CLIENT_ID` isolates it.
- [ ] `PuppetClient` (multiplayer scenarios) connects to the correct per-run DB.

**Verification:** Run two known-good STDB scenarios concurrently by hand using the env-block launcher; confirm both pass and DBs differ.

**Dependencies:** Task 0. **Files touched:** none game-side (env-only); findings feed Task 3. **Scope:** S.

#### Task 3: `scripts/run_stdb_scenarios_parallel.ps1`
**Description:** Extend the project-owned `scripts/run_stdb_scenarios.ps1` with a **sequential publisher**
(build once up front, then `spacetime publish` each unique DB) that **pipelines** published DBs into a
**parallel run pool** built on the kit's improved `run_scenario.ps1` (passing per-run env overlay +
unique client-id from Task 1's passthrough); each DB is deleted after its run. Aggregates results,
prunes once, cleans up all DBs even on failure. Exposes `-MaxParallel` (default 4) and `-KeepDatabase`.

**Acceptance criteria:**
- [ ] Full STDB suite runs green in parallel, meaningfully faster than serial (target: ≥2.5×).
- [ ] Every ephemeral DB is deleted at the end (none orphaned); `-KeepDatabase` keeps only failed DBs.
- [ ] No cross-scenario state leakage; reducer-authority assertions unaffected.
- [ ] `spacetime publish` never runs concurrently (no cargo build-lock errors); publish latency is overlapped with runs.
- [ ] `-MaxParallel 1` reproduces serial behavior.

**Verification:** `./scripts/run_stdb_scenarios.ps1 -MaxParallel 4`; `spacetime list --server local` shows no leftover `nomad-test-*` DBs afterward; compare pass set to serial.

**Dependencies:** Tasks 1, 2. **Files touched:** `scripts/run_stdb_scenarios.ps1`. **Scope:** M–L.

### Checkpoint: STDB suite parallel
- [ ] STDB suite green in parallel, faster, no DB leaks, no state leakage.
- [ ] Reducer-authority/determinism unaffected.
- [ ] Review with human before integrating.

### Phase 3: Integrate & harden

#### Task 4: Switch `validate_all.ps1` to parallel
**Description:** Point `validate_all.ps1` at the two parallel orchestrators with a `-MaxParallel`
passthrough; keep `-MaxParallel 1` as the serial fallback. Preserve the combined exit-code semantics.

**Acceptance criteria:**
- [ ] `./scripts/validate_all.ps1` runs both suites in parallel, green, faster end-to-end; exit code = max of the two.
- [ ] `-MaxParallel 1` reproduces today's serial behavior.

**Verification:** Run `validate_all.ps1` at default and at `-MaxParallel 1`; compare wall-clock and results.

**Dependencies:** Tasks 1, 3. **Files touched:** `scripts/validate_all.ps1`. **Scope:** S.

#### Task 5: Flake guard + concurrency tuning
**Description:** Parallel load steals CPU per instance; confirm it didn't introduce **timing** flakes
(scenarios with wall-clock waits are the risk). Run each parallel suite ≥3× (or via the existing
`-RepeatCount`), compare the flaky/failed set against the serial baseline, and pick the default
`-MaxParallel`. If a scenario flakes only under load, lower throttle and/or note it for a frame-based
wait fix (a separate follow-up if it needs a scenario/framework change).

**Acceptance criteria:**
- [ ] 3 consecutive parallel runs of each suite produce **no new** flaky/failed scenarios vs the serial baseline.
- [ ] A default `-MaxParallel` is chosen and recorded with the headroom rationale.
- [ ] Any load-only flaky scenario is documented (and, if trivially a wall-clock wait, filed as a follow-up).

**Verification:** Three back-to-back parallel `validate_all` runs at the chosen default; inspect aggregated flaky sets.

**Dependencies:** Task 4. **Files touched:** defaults in the orchestrators; notes here. **Scope:** S–M.

#### Task 6: Documentation
**Description:** Update the command table in `CLAUDE.md`, the `validate-gameplay` skill, and `README`
to describe the parallel runners, the `-MaxParallel` knob, the serial fallback, and that runs are
windowed (not headless) because screenshots feed the DoD.

**Acceptance criteria:**
- [ ] `CLAUDE.md` command table lists the parallel suite commands + `-MaxParallel`.
- [ ] `validate-gameplay` skill references the parallel runner and the serial-debug fallback.
- [ ] README mentions concurrency + tuning.

**Verification:** Re-read each doc; commands copy-paste-run.

**Dependencies:** Tasks 4, 5. **Files touched:** `CLAUDE.md`, `.claude/skills/validate-gameplay/…`, `README.md`. **Scope:** S.

#### Task 7 (OPTIONAL): Capability-based scheduling
**Description:** Let a scenario contract declare scheduling needs the runner respects — e.g.
`concurrency: "exclusive"` (run alone, not in the pool) or `headless_safe: false`. Only build this when
a genuinely concurrency-unsafe scenario actually appears; the NO_FOCUS fix means none exists today.

**Acceptance criteria:**
- [ ] A scenario flagged `concurrency: "exclusive"` runs outside the parallel pool; all others still parallelize.
- [ ] Unflagged scenarios behave exactly as today (no contract churn required).

**Dependencies:** Task 1. **Files touched:** kit `run_all_scenarios.ps1` (+ scenario schema docs). **Scope:** S–M. **Status:** deferred / not scheduled.

### Checkpoint: Complete
- [ ] Both suites parallel, green, faster; no new flakes; DBs clean.
- [ ] Serial fallback documented and working.
- [ ] `git push origin`.

## Risks and Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Timing flakiness under CPU contention (wall-clock waits) | Med | Tunable throttle + flake guard (Task 5); frame-based waits already used for known modal-key flakes |
| `spacetime publish` concurrency (cargo build-lock) | Med | Sequential publisher pipelined into parallel run pool (Task 3); optional prebuilt-wasm investigation |
| `Start-Process -Environment` wipes PATH (godot/spacetime unresolved) | Low | Clone parent env + overlay; validated in Task 0 |
| Token-file collision (`.nomad-main`) across concurrent STDB runs | Med | Unique `NOMAD_CLIENT_ID` per worker slot (Task 2) |
| Aggregation schema drift breaks `debug-validation-failure` / prune manifests | Med | Reuse exact `suite.json` schema; validate against existing consumers (Task 1 AC) |
| Concurrent artifact prune races | Low | Children `-SkipArtifactPrune`; single end-of-suite prune |
| Many live DBs ticking scheduled reducers raise server load | Low–Med | Throttle; delete each DB promptly after its run |
| Concurrent windows grab OS focus, disrupting the human at the machine | Low | Document; cannot go headless (screenshots). Optional off-screen `-Screen` placement |
| Kit submodule edits not committed/pushed (symlinks point at uncommitted kit code) | Low | Commit in `upta/agentic-godot-validation`, bump submodule pointer, re-run `./setup.ps1` (Task 6 / wrap-up) |

## Decisions (resolved)

1. **Code location — upstream into the kit submodule.** Generic pool + `-MaxParallel` + aggregation in
   the kit's `run_all_scenarios.ps1`; generic env/user-arg passthrough in the kit's `run_scenario.ps1`;
   Nomad STDB lifecycle stays in `scripts/run_stdb_scenarios.ps1`. (Submodule edits approved for this work.)
2. **Default `-MaxParallel` = 4**, tunable via the flag; `-MaxParallel 1` = serial.
3. **Focus blocker → `WINDOW_FLAG_NO_FOCUS` runtime fix** (Task 0), validated; this is what makes
   windowed parallel safe for input-driven scenarios.
4. **Capability-based batching — deferred as optional (Task 7).** Brian raised flagging each scenario's
   required capabilities (input, etc.) to route scheduling. The NO_FOCUS fix removed the concrete need:
   input scenarios now run safely alongside all others in parallel (45/45 at 4-wide), so there's nothing
   to *segregate* today. It stays valuable as a lightweight safety valve — a future scenario could
   declare `concurrency: "exclusive"` (run alone) or `headless_safe: false` (memory point 10) — so it's
   captured as optional Task 7, to pull in only when a genuinely concurrency-unsafe scenario appears
   rather than built speculatively now.

## Open Questions (still open)

3. **Windowed-only acceptable?** Headless would be faster/quieter but skips screenshots, which the DoD
   requires for visual review. Recommend staying windowed. *(Assumed yes unless you object.)*
4. **Optimize publishes?** Optionally investigate publishing a prebuilt `.wasm` to N DBs to allow
   concurrent publish — an optimization beyond the sequential-publisher baseline. Defer unless publish
   proves to be the floor.
