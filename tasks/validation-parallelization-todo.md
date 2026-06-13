# Parallel Validation Runner — Todo

Companion to [`tasks/validation-parallelization-plan.md`](validation-parallelization-plan.md).
Separate from the live game plan in [`tasks/todo.md`](todo.md).

## Phase 0: De-risk
- [x] **Task 0 — Baseline + parallel-launch spike** ✅ (surfaced + solved the focus blocker)
  - [x] Serial baseline: pure suite ~170s/45; 16 cores, pwsh 7.6
  - [x] Spike: 4 pure scenarios concurrent via `Start-Process -Environment` → ~5s (~3×); env **overlays** (clone+overlay), PATH preserved, screenshots produced
  - [x] **BLOCKER found:** windowed parallel zeroes held input (`player_moves_right` 3/4 fail; actor moved 0px). Cause: focus-out releases held input. Headless 12/12 but no screenshots → not usable.
  - [x] **FIX:** `WINDOW_FLAG_NO_FOCUS` in `test_bootloader.gd` → windowed 4-wide 12/12 with screenshots; modal/GUI-focus 8/8. Recorded in memory `validation-scenario-driving-gotchas` (pt 11).

## Phase 1: Pure suite parallel  (kit submodule)
- [x] **Task 1 — `-MaxParallel` pool in kit `tools/run_all_scenarios.ps1`** ✅
  - [x] `ForEach-Object -Parallel` run phase → unchanged serial aggregation, `-MaxParallel` (default 4)
  - [x] `run_scenario.ps1` NOT changed — child env inherits transparently (STDB Task 3 uses per-child env block at launch)
  - [x] Same `suite.json` schema (+ `max_parallel`); single end-of-suite prune
  - [x] **45/45 pass, 52.2s at 4-wide vs ~170s serial (~3.2×), zero flaky**; screenshots verified
  - [x] `-MaxParallel 1` == serial path preserved
  - [ ] Commit in kit repo + bump submodule pointer; `./setup.ps1` if symlinks change  ← in progress
- [x] **Checkpoint:** pure parallel green/faster, valid `suite.json`, serial fallback OK — **review with human (here)**

## Phase 2: STDB suite parallel
- [ ] **Task 2 — Per-child STDB isolation**
  - [ ] Env block overlays unique `NOMAD_STDB_DB` + shared `NOMAD_STDB_URI` + unique `NOMAD_CLIENT_ID`
  - [ ] Verify `.nomad-<id>` token files don't collide (find token-file location)
  - [ ] `PuppetClient` connects to the correct per-run DB
  - [ ] Two scenarios concurrent on distinct DBs, no leakage
- [ ] **Task 3 — `-MaxParallel` in `scripts/run_stdb_scenarios.ps1`**
  - [ ] Sequential publisher (build once → publish each unique DB) pipelined into parallel run pool
  - [ ] Delete each DB after its run; clean up all on failure; `-KeepDatabase` keeps only failed
  - [ ] No concurrent `spacetime publish` (no cargo build-lock errors)
  - [ ] Green + ≥2.5× faster; no leftover `nomad-test-*` DBs; `-MaxParallel 1` == serial
- [ ] **Checkpoint:** STDB parallel green/faster, no DB leaks, no state leakage — review with human

## Phase 3: Integrate & harden
- [ ] **Task 4 — `validate_all.ps1` → parallel**
  - [ ] Calls both parallel orchestrators with `-MaxParallel` passthrough; exit = max of two
  - [ ] `-MaxParallel 1` reproduces serial
- [ ] **Task 5 — Flake guard + tuning**
  - [ ] 3 consecutive parallel runs of each suite → no NEW flaky/failed vs serial baseline
  - [ ] Pick + record default `-MaxParallel`
  - [ ] Document any load-only flaky scenario (file follow-up if wall-clock-wait fix needed)
- [ ] **Task 6 — Docs**
  - [ ] `CLAUDE.md` command table (+ `-MaxParallel`, serial fallback)
  - [ ] `validate-gameplay` skill references parallel runner + serial-debug fallback
  - [ ] README concurrency/tuning note
- [ ] **Checkpoint:** complete — both suites parallel/green/faster, no new flakes, `git push origin`

## Optional / deferred
- [ ] **Task 7 (OPTIONAL): capability-based scheduling** — `concurrency: "exclusive"` / `headless_safe` flags. Deferred: NO_FOCUS removed the need to segregate input scenarios. Build only when a concurrency-unsafe scenario appears.

## Decisions
- [x] Q1: **upstream into the kit submodule** (generic pool/aggregation in kit; STDB lifecycle stays in `scripts/`)
- [x] Q2: default **`-MaxParallel 4`**, tunable; `1` = serial
- [x] Q3: windowed (NOT headless) — required for screenshots + render-dependent scenarios; NO_FOCUS makes it parallel-safe
- [x] Q5: capability-batching → optional Task 7 (deferred; NO_FOCUS solved the input case)
- [ ] Q4: investigate prebuilt-wasm publish to parallelize publishes (optional optimization)?
