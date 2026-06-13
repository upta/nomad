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
  - [x] Committed: kit `aa62d2f` (submodule main) + main repo `0439895` (pointer bump). No symlink change → no `./setup.ps1`. **Push pending** (both repos, incl. external kit repo).
- [x] **Checkpoint:** pure parallel green/faster, valid `suite.json`, serial fallback OK — **review with human (here)**

## Phase 2: STDB suite parallel
- [x] **Task 2 — Per-child STDB isolation** ✅
  - [x] `Start-Process -Environment` per child: clone parent env + overlay unique `NOMAD_STDB_DB` + `NOMAD_STDB_URI` + unique `NOMAD_CLIENT_ID` (→ isolated `.nomad-<id>` token, 0 token litter observed)
  - [x] No shared `$env:` (would race across runspaces); verified no cross-DB leakage
- [x] **Task 3 — `-MaxParallel` in `scripts/run_stdb_scenarios.ps1`** ✅ (design improved)
  - [x] Build module ONCE → each worker publishes prebuilt wasm to its own DB via `spacetime publish --bin-path` (concurrent-safe, ~1.5s, no cargo rebuild) — better than the planned sequential publisher
  - [x] Delete each DB after run (all paths); `-KeepDatabase` keeps failed/single; 0 orphan DBs verified
  - [x] **Verdict by `summary.json` status, not exit code** (adversarial review caught an exit-code+retry design that masked real load-sensitive failures): `status=='pass'` validates even with a teardown crash → **flaky/green**; `status != 'pass'` is a real failure, **never auto-forgiven**. Retry removed.
  - [x] Writes `client/artifacts/stdb_suites/<id>/suite.json` (per-scenario status/exit/validated/flaky) — auditable like the pure suite
  - [x] Fixed exit-code bug in BOTH suites: negative crash codes (`-1073741819`) swallowed by `-gt` max → `-ne 0`
  - [x] Default **`-MaxParallel 2`** (server-bound — see memory pt 12); 2-wide verified **45/45 validated, exit 0, ~207s**; `-MaxParallel 1` == serial
- [x] **Checkpoint:** STDB parallel green, no DB leaks, no state leakage — reviewed with human (chose 2-wide; design then hardened by adversarial review)

## Phase 3: Integrate & harden
- [x] **Task 4 — `validate_all.ps1` → parallel** ✅
  - [x] Both suites run parallel by default; optional `-PureMaxParallel` / `-StdbMaxParallel` passthroughs (separate, since optimal differs: pure 4, stdb 2); exit = max of two; pass `1` for serial
- [x] **Task 5 — Flake guard + tuning** ✅ (concurrency sweep characterized flakes)
  - [x] Pure: 45/45 multiple runs at 4-wide, zero flaky. STDB: swept 4/3/2-wide → 2-wide clean (45/45 ×2); 4-wide flakes (server-bound)
  - [x] Defaults recorded: pure **4**, stdb **2**. Status-based verdict means residual flakes are reported honestly (flaky=green only when assertions passed), not masked.
- [x] **Task 6 — Docs** ✅
  - [x] `CLAUDE.md` command table (`-MaxParallel` defaults + serial fallback)
  - [x] README concurrency/tuning note. (`validate-gameplay` skill doesn't hardcode these commands → no change.)
- [x] **Adversarial review** (multi-agent, 10 confirmed findings): fixed the 1 trust-critical masking hole (→ status-based verdict), the pure-suite negative-exit swallow, and added the STDB `suite.json`. Re-verified both suites green.
- [ ] **Checkpoint:** complete — both suites parallel/green, no masking, then `git push origin`  ← committing now

## Optional / deferred
- [ ] **Task 7 (OPTIONAL): capability-based scheduling** — `concurrency: "exclusive"` / `headless_safe` flags. Deferred: NO_FOCUS removed the need to segregate input scenarios. Build only when a concurrency-unsafe scenario appears.

## Decisions
- [x] Q1: **upstream into the kit submodule** (generic pool/aggregation in kit; STDB lifecycle stays in `scripts/`)
- [x] Q2: default `-MaxParallel` — pure **4**, stdb **2** (server-bound); `1` = serial
- [x] Q3: windowed (NOT headless) — required for screenshots + render-dependent scenarios; NO_FOCUS makes it parallel-safe
- [x] Q4: **prebuilt-wasm publish — DONE.** `spacetime publish --bin-path` (build once, publish to N DBs concurrently) is how the STDB suite parallelizes publishes.
- [x] Q5: capability-batching → optional Task 7 (deferred; NO_FOCUS solved the input case)
