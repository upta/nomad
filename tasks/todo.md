# Task 0.1: Project Restructuring — Todo ✅ DONE

## Subtask 0.1.1: Rename C# files + add namespaces ✅
- [x] Rename `src/bootstrap/app_root.cs` → `client/bootstrap/AppRoot.cs`
- [x] Add `namespace Nomad.Bootstrap;` to `AppRoot.cs`
- [x] Rename `src/game/main.cs` → `client/game/Main.cs`
- [x] Add `namespace Nomad.Game;` to `Main.cs`

## Subtask 0.1.2: Update .tscn files + rename ✅
- [x] Rename `src/bootstrap/app_root.tscn` → `client/bootstrap/AppRoot.tscn`
- [x] Update script path in `AppRoot.tscn` to `res://bootstrap/AppRoot.cs`
- [x] Rename `src/game/main.tscn` → `client/game/Main.tscn`
- [x] Update script path in `Main.tscn` to `res://game/Main.cs`

## Subtask 0.1.3: Rename project identity files ✅
- [x] Update `project.godot`: name="Nomad", assembly_name="Nomad", main_scene="res://bootstrap/AppRoot.tscn"
- [x] Rename `MyPrototype.csproj` → `Nomad.csproj`, update RootNamespace to "Nomad"
- [x] Rename `MyPrototype.sln` → `Nomad.sln`, update references to Nomad

## Subtask 0.1.4: Move src/ → client/ ✅
- [x] Delete `.godot/mono/` build artifacts
- [x] Rename `src/` → `client/`

## Subtask 0.1.5: Update path references ✅
- [x] Update `symlink-config.txt`: `src/addons/` → `client/addons/`
- [x] Update `.gitignore`: `src/addons/` → `client/addons/`
- [x] Update `README.md`: `src/` → `client/`, `MyPrototype` → `Nomad`

## Subtask 0.1.6: Symlinks + build ✅
- [x] Run `.\setup.ps1` to create symlinks in `client/`
- [x] Run `dotnet build` from `client/` — 0 warnings, 0 errors
- [x] Verify no validation scenarios broken (none exist yet)

---

# Task 0.2: SpacetimeDB Server Scaffold ✅ DONE

## Convention Migration (SDK 2.4.x)
- Lifecycle hooks: `ClientConnected` / `ClientDisconnected` (NO "On" prefix — STDB0010 error otherwise)
- Attributes: `[PrimaryKey]`, `[AutoInc]`, shorthand (no `SpacetimeDB.` prefix on well-known attrs)
- Tables nested inside `public static partial class Module`, split across files via `partial`
- Module path: `server/src/` (not `spacetimedb/`)
- `.csproj`: `Microsoft.NET.Sdk` + `wasi-wasm` + `SpacetimeDB.Runtime 2.4.*`
- `.csproj` MUST be named `StdbModule.csproj` (SpacetimeDB expects this name)

## Subtask 0.2.1: Scaffold + reorganize ✅
- [x] Run `spacetime init` into `server/` (done)
- [x] Delete generated artifacts: `.cursor/`, `.windsurfrules` (not present — clean)
- [x] Move module from `server/spacetimedb/` → `server/src/` (not present — already in src/)
- [x] Keep `StdbModule.csproj` name (required by SpacetimeDB — renaming breaks build)
- [x] Update `spacetime.json` module-path to `./src`
- [x] Add `server/.gitignore` for wasm artifacts

## Subtask 0.2.2: Types + GlobalUsings ✅
- [x] Create `server/src/GlobalUsings.cs` — `global using SpacetimeDB;`
- [x] Create `server/src/Types/EntityType.cs` — `enum EntityType : uint { None, Player }`

## Subtask 0.2.3: Table definitions ✅
- [x] Create `server/src/Tables/Player.cs` — Identity PK, IsConnected, PlayerEntityId (int)
- [x] Create `server/src/Tables/Entity.cs` — EntityId PK+AutoInc (int), EntityTypeId (uint), PositionX/Y (float)
- [x] Create `server/src/Tables/EntityOwnership.cs` — EntityId PK (int), Owner (Identity) with BTree index, Public=false

## Subtask 0.2.4: Reducers ✅
- [x] Create `server/src/Reducers/Connect.cs` — ClientConnected: upsert player + spawn player entity + create ownership
- [x] Create `server/src/Reducers/Disconnect.cs` — ClientDisconnected: mark player disconnected
- [x] Create `server/src/Reducers/MoveEntity.cs` — ownership check + position update with `with` expression

## Subtask 0.2.5: Build + publish + docs ✅
- [x] `spacetime build --module-path ./src` succeeds
- [x] `spacetime publish nomad --delete-data=always --yes --server local --module-path ./src` succeeds
- [x] Generate client bindings — `spacetime generate --lang csharp --out-dir ../client/Db --module-path ./src`
- [x] `server.instructions.md` already reflects SDK 2.4.x conventions
- [x] `server-reducers.instructions.md` already reflects SDK 2.4.x conventions

---

# Task 1.1: Hull Template Data Model ✅ DONE

## Subtask 1.1.1: Create RoomSlot Resource ✅
- [x] Create `client/game/Ship/RoomSlot.cs` — `[GlobalClass] Resource` with `SlotIndex`, `PositionX`, `PositionY`, `Width`, `Height`

## Subtask 1.1.2: Create HullTemplate Resource ✅
- [x] Create `client/game/Ship/HullTemplate.cs` — `[GlobalClass] Resource` with `HullId`, `GridWidth`, `GridHeight`, `ArmorRating`, `RoomSlots`
- [x] Static `CreateCorvette()` factory with 7 room slots on 8×6 grid

## Subtask 1.1.3: Create CorvetteHull.tres ✅
- [x] Create `client/game/Ship/CorvetteHull.tres` — serialized HullTemplate with 7 RoomSlot sub-resources
- [x] 7 room layout: center corridor, rooms of varying widths

## Subtask 1.1.4: Validation scenario ✅
- [x] Create `client/validation/harnesses/HullHarness.tscn` + `HullHarnessController.cs`
- [x] Create `client/validation/scenarios/hull_corvette_loads.json` — 5 assert_value checks
- [x] Scenario passes: room_count=7, grid_width=8, grid_height=6, hull_id="corvette", armor_rating=1

## Subtask 1.1.5: Fix script auto-detection ✅
- [x] Fix `tools/run_scenario.ps1` — auto-detect `project.godot` in `client/` subdirectory
- [x] Fix `tools/run_all_scenarios.ps1` — same auto-detect
- [x] Fix `Resolve-ScenarioPath` to try both repo-root-relative and project-relative paths
- [x] All 3 scenarios pass with no project selector

---

# Task 0.3: SpacetimeDB Client Integration ✅ DONE

## Subtask 0.3.1: Add NuGet package ✅
- [x] Add `SpacetimeDB.ClientSDK` v2.4.1 to `Nomad.csproj`
- [x] `dotnet restore` + `dotnet build` succeeds

## Subtask 0.3.2: Create DbManager ✅
- [x] Create `client/game/Db/DbManager.cs` — Godot Node that uses `DbConnection.Builder()` pattern
- [x] Connect to `http://localhost:3000` / `nomad`
- [x] Subscribe to all tables (`Players` + `Entities`) on connect via `SubscriptionBuilder().SubscribeToAllTables()`
- [x] Call `conn.FrameTick()` in `_Process(double delta)` each frame
- [x] `OnConnectError` and `OnDisconnect` callbacks with GD.PrintErr logging
- [x] Expose `Tables` (RemoteTables) and `Reducers` (RemoteReducers) properties
- [x] Proper `#nullable enable` with `new` keyword for IsConnected (hides GodotObject.IsConnected)
- [x] `dotnet build` succeeds

## Subtask 0.3.3: Wire into Main scene ✅
- [x] `Main.cs` instantiates `DbManager` as child in `_Ready()`
- [x] Updated label text to "Nomad"
- [x] `dotnet build` succeeds

## Subtask 0.3.4: End-to-end verification ✅
- [x] Godot headless run confirms: `Connecting to ws://localhost:3000 nomad`
- [x] Client connected: `[DbManager] Connected. Identity: ...`
- [x] Subscription applied: `[DbManager] Subscription applied — initial table data loaded.`
- [x] Player row created in SpacetimeDB with `player_entity_id` set
- [x] `spacetime sql nomad "SELECT * FROM Players"` shows the new Player row
- [x] `spacetime build` and `dotnet build` still pass after formatting

---

# Task 0.4: Tile Grid + Camera ✅ DONE

## Subtask 0.4.1: ShipGrid tile map ✅
- [x] Create `client/game/Map/ShipGrid.cs` — Node2D with `_Draw()` rendering a ship floor plan
- [x] 8×6 tile grid (TileSize=64), centered at origin, with 4 rooms (3×2 tiles each) in a 2×2 layout
- [x] Flat colors: hull background (dark blue-gray), room interiors, wall outlines — RimWorld-inspired vector style per GDD §7.1
- [x] Create `client/game/Map/ShipGrid.tscn` scene
- [x] Wire ShipGrid into `Main.cs`

## Subtask 0.4.2: Camera2D ✅
- [x] Add Camera2D to Main scene in `Main.cs`
- [x] Top-down 2D: position smoothing enabled (speed=5), zoom 2.0×
- [x] Camera currently static — follow target will be assigned to player in Task 0.5
- [x] `dotnet build` + `spacetime build` both pass
- [x] Headless run: zero warnings, zero errors, SpacetimeDB connection still works

---

# Task 1.2: Room System 🔄 IN PROGRESS

## Subtask 1.2.1: Server Room Tables + Types ✅ DONE
- [x] Create `server/src/Types/TerminalType.cs` — enum: None, StarChart, PowerRouter, Fabricator, Cloning, Info
- [x] Create `server/src/Types/RoomTypeId.cs` — enum: None, Reactor, Bridge, CloningBay, Hydroponics, Workshop, Kitchen, CargoBay
- [x] Create `server/src/Tables/RoomAssignment.cs` — partial struct with SlotIndex PK, RoomTypeId, IsPowered, IsPressurized, BreakerOn, Health
- [x] `spacetime build` succeeds
- [x] `spacetime generate` client bindings

## Subtask 1.2.2: Server AssignRoomType Reducer + Init Seeding ✅ DONE
- [x] Create `server/src/Reducers/AssignRoomType.cs` — reducer: slot_index, room_type_id, ownership/phase check
- [x] Create `server/src/Reducers/Init.cs` — Init lifecycle seeds Corvette room defaults
- [x] `spacetime build` + `spacetime publish` + generate bindings
- [x] Verify via `spacetime sql` that room assignments appear

## Subtask 1.2.3: Client RoomType Resource + 7 Room Types ✅ DONE
- [x] Create `client/game/Ship/TerminalType.cs` — client-side enum
- [x] Create `client/game/Ship/RoomType.cs` — `[GlobalClass]` Resource: RoomId, Label, PowerDraw, TerminalType, Color
- [x] Create 7 `.tres` RoomType files in `client/game/Ship/RoomTypes/`
- [x] Create `RoomTypeRegistry` — loads all RoomTypes, provides lookup by RoomId
- [x] `dotnet build` succeeds

## Subtask 1.2.4: Client Room Rendering in ShipGrid ✅ DONE
- [x] Rewrite `ShipGrid.cs` — accept HullTemplate, RoomAssignment table, RoomTypeRegistry
- [x] Render rooms with type-specific colors and labels per slot
- [x] Subscribe to RoomAssignment OnInsert/OnUpdate for reactive rendering
- [x] Wire into `Main.cs`
- [x] `dotnet build` + headless run verification

## Subtask 1.2.5: Validation Scenarios ✅ DONE
- [x] Create `client/validation/harnesses/RoomTypeHarness.tscn` + `RoomTypeHarnessController.cs`
- [x] Create `client/validation/harnesses/RoomRenderHarness.tscn` + `RoomRenderHarnessController.cs`
- [x] Create `client/validation/scenarios/room_types_load.json` — verify 7 room types
- [x] Create `client/validation/scenarios/rooms_assigned_to_slots.json` — verify rendering
- [x] Run all 5 scenarios → all pass
- [x] `dotnet csharpier format .` in both client/ and server/

---

# Task 1.2: Room System 🔄 IN PROGRESS → ✅ DONE

---

# Task 1.3: Interaction Framework (walk-up prompts + terminal modals) ✅ DONE

Reference implementation: untrailed at `C:\Code\Github\upta\untrailed` — `client/src/Core/Interaction/` (probe/target/registration/service), `client/src/App/Interaction/_Service/GameInteractionService.cs` (closest-target resolve), `client/src/Camp/_UI/CookingUI.cs` (modal open/close + exclusive context push pattern). Nomad's `GuideService` already has the exclusive `PushContext`/`PopContext` stack — no changes needed there.

Design notes:
- New code lives in `client/game/Interaction/` (`_Component/`, `_Model/`, `_Service/`) + `client/game/Ship/Terminal/` + `client/game/Ui/`.
- `InteractionService` is plain C# (service-layer rules); scenes depend on it via AutoInject `IProvider`/`[Dependency]` — providers: `Main` (game) and the validation harness controller (tests). Avoids the NodePath-export-across-scene-instances gotcha.
- Beyond untrailed: the service resolves the **focused** (closest registered) target every `Process()` and raises `FocusChanged` — drives the prompt, which untrailed didn't have.
- Pick a free collision layer for interactables after checking Player/wall layer usage in `Player.tscn` / `ShipGrid.cs`.
- Modal content for all 5 terminal types starts as `RoomInfoModal` (label, room type, power status) per resolved design decision "others show room status info initially"; specialized modals ship with their owning features.

## Subtask 1.3.1: Interaction core (port from untrailed) — Scope: M
- [x] Create `client/game/Interaction/_Model/ProbeData.cs` — entity id + position
- [x] Create `client/game/Interaction/_Model/InteractionRegistration.cs` — abstract: `Position`, `Label`, `OnInteraction(ProbeData)`
- [x] Create `client/game/Interaction/_Model/CallbackInteractionRegistration.cs` — delegate-based concrete registration (untrailed's `SimpleInteractionRegistration`)
- [x] Create `client/game/Interaction/_Service/InteractionService.cs` — register/unregister, pending-trigger queue, `Process()` resolves closest via distance-squared, `Focused` + `FocusChanged` event recomputed each `Process()`
- [x] Create `client/game/Interaction/CollisionLayers.cs` — flags enum; verify existing layer usage first
- [x] Create `client/game/Interaction/_Component/InteractProbe.cs` — Area2D, interactable layer/mask
- [x] Create `client/game/Interaction/_Component/InteractTarget.cs` — Area2D, `[Export] GuideActionBinding Trigger`, registers/unregisters on probe enter/exit, fires `Service.NotifyTriggered` on `Trigger.JustTriggered` (verify `GuideActionBinding` exposes `JustTriggered`; add if missing)
- [x] `Player.tscn`: add `%InteractProbe` (Area2D + CircleShape2D radius ~40, scene-declared); `Player.cs`: `[Node]` bind, `[Dependency]` InteractionService, update probe data + `Process()` in `_PhysicsProcess`
- [x] `Main.cs`: own + provide `InteractionService` via `IProvider`
- [x] Acceptance: `dotnet build` clean; walking within probe range of a test `InteractTarget` registers it, leaving unregisters (provable in 1.3.5 harness)

## Subtask 1.3.2: Interact input + prompt UI — Scope: S
- [x] `AppRoot.EnsureInputActions()`: register `interact` → Key.E in `InputMap`
- [x] Verify `Interact.tres` is mapped in `CharacterContextKbm.tres` (E) and `CharacterContextController.tres` (already authored — confirm trigger=pressed→JustTriggered semantics)
- [x] Create `client/game/Ui/InteractPrompt.tscn` + `InteractPrompt.cs` — small Control/Label ("[E] {Label}"), subscribes `FocusChanged`, positions above focused target's world position, hidden when none
- [x] Add `InteractPrompt` to `Main.tscn` (scene-declared)
- [x] Acceptance: prompt appears when near an interactable, names it, disappears on walk-away

## Subtask 1.3.3: Modal host + UI input context — Scope: M
- [x] Create `client/game/Ui/_Input/Actions/UiCancel.tres` GUIDE action + `UiContextKbm.tres` (Esc + E) / `UiContextController.tres` (B) mapping contexts + `UiModeContext.tres` (`InputModeContext`)
- [x] `AppRoot.EnsureInputActions()`: register `ui_cancel_modal` → Key.Escape for validation
- [x] Create `client/game/Ui/ModalHost.cs` + add `ModalHost` CanvasLayer to `Main.tscn` — `[Export] PackedScene` per `TerminalType`, `Open(TerminalType, roomContext)` instantiates the modal, pushes `UiModeContext` exclusive; `Close()` pops; `IsOpen` exposed
- [x] Create `client/game/Ui/Modals/RoomInfoModal.tscn` + `RoomInfoModal.cs` — Control panel showing room label, type, power state; wires `UiCancel` → deferred `Close()` (untrailed `CookingUI` pattern incl. `_ExitTree` context cleanup)
- [x] Wire all 5 `TerminalType` entries to `RoomInfoModal.tscn` in `Main.tscn`
- [x] Acceptance: opening a modal freezes movement (exclusive context), `UiCancel` closes it and restores movement; no orphaned contexts after repeated open/close

## Subtask 1.3.4: Room terminals — Scope: M
- [x] Create `client/game/Ship/Terminal/Terminal.tscn` + `Terminal.cs` — visual (ColorRect/_Draw marker) + `InteractTarget` child (scene-declared); exports for slot index, room label, `TerminalType`; registration opens modal via `ModalHost`
- [x] Spawn terminals data-driven: terminal-spawner node declared in `ShipGrid.tscn` (or ShipGrid itself) instantiates `[Export] PackedScene Terminal` per assigned slot at slot-center cell, reacting to the same assignment data (server `RoomAssignments` + `SetTestAssignment` test path)
- [x] Terminal label/type resolved through `RoomTypeRegistry`
- [x] Acceptance: 7 terminals appear in the 7 assigned Corvette rooms in-game; each opens a modal titled with its room's label

## Subtask 1.3.5: Validation scenarios — Scope: M
- [x] Create `client/validation/harnesses/InteractionHarness.tscn` + `InteractionHarnessController.cs` — ShipGrid + Player + registries + ModalHost + InteractPrompt; controller provides `InteractionService`, seeds test assignments, bridges `interact`→E and `ui_cancel_modal`→Esc in `ActionKeyBridge`; `get_observed_state()` exposes player pos, focused-target id/label, prompt visibility, modal open/title
- [x] Scenario `terminal_interact_opens_modal.json` (red first): walk to nearest terminal (`wait_until` focused != none) → checkpoint prompt visible → `press_action interact` → assert modal open + title → press move while open, assert position unchanged → `press_action ui_cancel_modal` → assert modal closed + movement restored → screenshots at each checkpoint
- [x] Scenario `interact_requires_target.json`: interact pressed away from any terminal → no modal; walk near then away → prompt shown then hidden
- [x] Scenario `scenarios_stdb/terminals_spawn_from_server_assignments.json`: connected harness (reuse `ConnectedGameHarness` pattern) asserts 7 terminals spawn from live `RoomAssignments`
- [x] Run new scenarios red → implement → green; visually review checkpoint screenshots

## Definition of Done (Task 1.3)
- [x] All new scenarios pass; screenshots reviewed
- [x] `./scripts/validate_all.ps1` — both suites green, no regressions
- [x] Game boots clean ≥10s, zero `ERROR:` lines (`run-game` skill)
- [x] `dotnet build` + `dotnet csharpier format .` (client), `spacetime build` + format (server — untouched unless stdb scenario needs seeding help)
- [x] `git push origin`

---

# Task 1.4: Power Grid + Breaker Switches ✅ DONE

Full plan: `C:\Users\upta\.claude\plans\parsed-sniffing-quill.md`. Scheduled-table syntax reference: untrailed `server/src/Tables/TravelTimer.cs`.

Design notes (user-confirmed):
- Varied draw per RoomTypeId (server-owned): Reactor 0 (generator), Bridge 2, CloningBay 2, Hydroponics 1, Workshop 2, Kitchen 1, CargoBay 0 — total 8. `ReactorRoom.tres` PowerDraw 3→0.
- `PowerGrid` single-row public table: Id (PK 0), ReactorOutput (seed 10), GraceMillis (seed 10000), GridStatus Status (Stable/Overload/Blackout), Timestamp BlackoutAt.
- `RecomputePowerGrid(ctx)`: demand = Σ draws of assigned breaker-on rooms; output = reactor assigned && breaker on ? ReactorOutput : 0. demand ≤ output → Stable (cancel timers, every room IsPowered = BreakerOn — doubles as blackout recovery). demand > output from Stable → Overload, BlackoutAt = now + GraceMillis, insert scheduled `GridBlackoutTimer`. `GridBlackoutTick`: still overloaded → Blackout, all IsPowered = false. Rooms stay powered during grace; flicker is client-side rendering of Overload.
- PowerRouter modal = overview + remote toggles (same `ToggleBreaker` reducer as wall breakers).

## Subtask 1.4.1: Server power model + reducers — Scope: M ✅
- [x] Create `server/src/Types/GridStatus.cs` — `[SpacetimeDB.Type]` enum Stable/Overload/Blackout
- [x] Create `server/src/Tables/PowerGrid.cs` — single-row public table per design notes
- [x] Create `server/src/Tables/GridBlackoutTimer.cs` — private scheduled table (`Scheduled = nameof(GridBlackoutTick)`, AutoInc ulong Id + ScheduleAt ScheduledAt)
- [x] Create `server/src/Power/PowerRules.cs` — partial Module: `PowerDrawFor(RoomTypeId)` switch + `RecomputePowerGrid(ctx)` (lazily insert PowerGrid row if missing)
- [x] Create `server/src/Reducers/ToggleBreaker.cs` — validate slot 0–6 + assignment exists + sender is known player; flip BreakerOn; recompute
- [x] Create `server/src/Reducers/SetReactorOutput.cs` — validate 0–100; update row; recompute
- [x] Create `server/src/Reducers/SetBlackoutGrace.cs` — validate > 0; update GraceMillis
- [x] Create `server/src/Reducers/GridBlackoutTick.cs` — scheduled; if still overloaded → Blackout, all rooms unpowered
- [x] Modify `server/src/Reducers/Init.cs` — seed PowerGrid row; `AssignRoomType.cs` — recompute at end
- [x] Modify `client/game/Ship/RoomTypes/ReactorRoom.tres` — PowerDraw = 0
- [x] Format → `spacetime build` → `spacetime publish nomad --delete-data=always --yes --server local --module-path ./src` → `spacetime generate --lang csharp --out-dir ../client/Db --module-path ./src` → `dotnet build` (client)
- [x] Acceptance: `spacetime sql` shows PowerGrid (output 10, grace 10000); `toggle_breaker 5` flips Kitchen breaker+power; `set_blackout_grace 1000` + `set_reactor_output 3` → Overload → Blackout ~1s later; logs clean (CLI reducer names are snake_case)

## Subtask 1.4.2: stdb validation of the reducer loop — Scope: S ✅
- [x] `ConnectedGameHarnessController.cs`: `power` section in `get_observed_state()` (per-slot breaker_on/is_powered, grid status/reactor_output/grace_millis); harness-registered InputMap test actions (`test_toggle_breaker_5`, `test_reactor_output_low`/`_high`, `test_short_grace`(500ms)/`test_long_grace`(3s)) firing reducers on edge-detected polls
- [x] Scenario `scenarios_stdb/power_breaker_reducer_round_trip.json` — toggle off → unpowered → toggle back → powered
- [x] Scenario `scenarios_stdb/power_overload_blackout.json` — short grace → low output → Overload (rooms still powered, BlackoutAt set) → Blackout (all unpowered) → high output → Stable + repowered
- [x] Scenario `scenarios_stdb/power_overload_grace_recovery.json` — 3s grace → overload → restore output within grace → wait past window → still Stable, never blacked out
- [x] `./scripts/run_stdb_scenarios.ps1` green (7/7); screenshots reviewed
- Gotcha discovered: poll driver-pressed test actions in `_PhysicsProcess` with manual edge detection — the driver's press/release window spans physics frames that can share one idle frame, so `_Process` + `IsActionJustPressed` both miss it. Hold presses ≥10 frames in scenarios.

## Subtask 1.4.3: Client rendering — dim + flicker — Scope: M ✅
- [x] `ShipGrid.cs`: subscribe `PowerGrids` OnInsert/OnUpdate (+ fix `_ExitTree` to unsubscribe everything); `[Export]` `UnpoweredDimFactor` (0.35), `FlickerIntervalSeconds` (0.12), `FlickerDimFactor` (0.6) set in `ShipGrid.tscn`; dim inside `GetRoomColor`; `_Process` flicker + `QueueRedraw` only while Overload; public `FlickerCycles`
- [x] `ShipGrid.cs` test paths: `SetTestAssignment(slot, type, isPowered = true, breakerOn = true)`, `SetTestPower(slot, breakerOn, isPowered)`, `SetTestGridStatus(status)`; observed state gains per-room is_powered/breaker_on + power.status/flicker_cycles
- [x] Create `client/validation/harnesses/PowerHarness.tscn` + `PowerHarnessController.cs` (clone InteractionHarness; seeds 7 rooms; ActionKeyBridge; pure test actions → ShipGrid test setters)
- [x] Scenario `power_unpowered_room_renders_dim.json` — cut Kitchen → dimmed color vs powered baseline (assert_pipeline); screenshot reviewed
- [x] Scenario `power_overload_flickers.json` — flicker_cycles increases during Overload, stops on Stable
- [x] `./tools/run_all_scenarios.ps1` green (14/14; `room_types_load` updated for Reactor PowerDraw 3→0 design change); `dotnet build` + format

## Subtask 1.4.4: Physical wall breakers — Scope: M ✅
- [x] Create `client/game/Ship/Breaker/Breaker.tscn` + `Breaker.cs` — pattern-copy Terminal: ColorRect box + lever (`[Export]` on/off colors), `InteractTarget` with `Interact.tres`, `SlotIndex`, `SetState(roomLabel, breakerOn)`, `Interacted` event, label "{Room} Breaker"
- [x] `ShipGrid.cs`: `[Export] PackedScene? BreakerScene`, `_breakers` dict, `EnsureRoomNodes()` (terminal + breaker) at every assignment-change site, placement top-left interior cell (+0.5 tile), `BreakerInteracted` event, `breaker_count` observed; export wired in `ShipGrid.tscn`
- [x] `Main.cs`: `BreakerInteracted` → `ToggleBreaker` reducer (null-guarded, unsubscribe in `_ExitTree`); `PowerHarnessController`: local `SetTestPower` flip
- [x] Scenario `breaker_interact_toggles_power.json` — breaker_count == 7 → walk to "Kitchen Breaker" (wall-clamp navigation: 400px/s + release latency overshoots position waits; wedging into the room corner with held diagonal input is deterministic) → interact → breaker off + dimmed → interact → restored
- [x] Pure suite green (15/15); screenshot shows breakers as wall fixtures with state-colored levers

## Subtask 1.4.5: PowerGridService + PowerRouter modal — Scope: M ✅
- [x] Create `client/game/Ship/_Service/PowerGridService.cs` (+ `PowerRoomEntry.cs`) — plain C#: `Changed` event, Status/ReactorOutput/TotalDemand/room entries, `SetRoomCatalog`, `BindConnection`/`Unbind`, `RequestToggleBreaker` (reducer when connected, local flip in test mode), test seeders
- [x] `Main.cs` provides `PowerGridService` (alongside InteractionService), feeds catalog + connection, routes breakers through it, exposes `Interaction` for harness observation; `PowerHarnessController` provides/seeds it and syncs `Changed` → ShipGrid test setters
- [x] Create `client/game/Ui/Modals/PowerRouterModal.tscn` + `.cs` (IRoomModal, `[Dependency]` service) — title, "Output X / Demand Y — Status" line, row container + `[Export] PackedScene RowScene`; rows update **in place** on `Changed` (rebuild would steal focus); first toggle grabs focus on open
- [x] Create `client/game/Ui/Modals/PowerRouterRow.tscn` + `.cs` — labels + focusable toggle Button → `RequestToggleBreaker`
- [x] `ModalHost.tscn`: repoint `PowerRouterModal` export to the new scene; `ConnectedGameHarnessController` exposes game.modal.open/title + game.focused_label
- [x] Scenario `power_router_modal_toggles_breaker.json` (pure) — `test_assign_kitchen_reactor` puts a PowerRouter terminal on the walkable Kitchen path → modal → `modal_down`×5/`modal_accept` toggles slot-5 row → breaker off + dark → accept restores → Esc closes. (Skipped InputEventAction risk entirely: harness bridges custom `modal_accept`/`modal_down` aliases to real Enter/Down key events.)
- [x] Scenario `scenarios_stdb/power_router_modal_server_toggle.json` — real game: corner-wedge navigation into the Reactor room, open modal, remote-toggle Kitchen row → server breaker_on flips, modal stays open with live rows
- [x] Both suites green (16 pure + 8 stdb); screenshots show modal layout + focus ring + live status line

## Subtask 1.4.6 / Definition of Done (Task 1.4)
- [x] All new scenarios pass; screenshots reviewed (dim, flicker, breakers, modal focus ring, live status line)
- [x] `./scripts/validate_all.ps1` — both suites green (16 pure + 8 stdb), no regressions
- [x] Game boots clean 13s, zero `ERROR:` lines, SpacetimeDB connected + subscription applied (`run-game` skill)
- [x] `dotnet build` + `dotnet csharpier format .` (client), `spacetime build` + format (server)
- [x] `tasks/plan.md` + `tasks/todo.md` updated; `git push origin`

---

# Task 1.5: Room Pressurization (+ corridors become pressurizable rooms) 🔄 PLANNED

Full plan: `C:\Users\upta\.claude\plans\buzzing-nibbling-meteor.md`.

Design notes (user-confirmed):
- Corridor = room slot 7 (`RoomTypeId.Corridor`), one pressure unit for the whole corridor network. Draw 0, not player-assignable, no terminal/breaker (ShipGrid's slot lookup already no-ops for non-RoomSlot indices), hidden from PowerRouter modal. Hull geometry stays in `HullTemplate.Corridors`.
- Depressurized visual = `color.Lerp(VacuumTint, DepressurizedBlend)` **before** power dimming — `VacuumTint ≈ (0.35, 0.45, 0.6)`, `DepressurizedBlend ≈ 0.55`, set in `ShipGrid.tscn`. For Kitchen (0.9, 0.8, 0.2): red falls AND blue rises — no `Darkened()` can do that, so asserts distinguish tint from dim. Composes with unpowered (tint then darken); Overload flickers the tinted color.
- `SetPressurization(slotIndex, isPressurized)` reducer must NOT call `RecomputePowerGrid` (pressurization is orthogonal to power; why-comment required since neighbors all recompute).
- RoomTypeRegistry refactor approved: `[Export] Array<RoomType>` wired in scenes replaces the `GD.Load` path list (CLAUDE.md anti-pattern cleanup).
- Modal is snapshot-at-open — an open RoomInfoModal won't live-update pressure; pre-existing, accept.

## Subtask 1.5.1: Server — corridor slot + SetPressurization reducer — Scope: M ✅
- [x] `server/src/Types/RoomTypeId.cs` — append `Corridor`
- [x] `server/src/Power/PowerRules.cs` — no edit needed: `PowerDrawFor`'s `_ => 0` default already covers Corridor (same convention as Reactor/CargoBay)
- [x] `server/src/Reducers/Init.cs` — `SeedRoom(ctx, 7, RoomTypeId.Corridor)`
- [x] `server/src/Reducers/AssignRoomType.cs` — reject `RoomTypeId.Corridor` (alongside `None`)
- [x] Create `server/src/Reducers/SetPressurization.cs` — sender auth + slot lookup (`is not { } room` → throw) + `Update(room with { IsPressurized = ... })`; no power recompute
- [x] Format → `spacetime build` → `spacetime publish nomad --delete-data=always` → `spacetime generate` → `dotnet build` (client). Note: `client/Db` is gitignored — bindings regenerate locally, nothing to commit there
- [x] Acceptance verified via CLI: 8 rows incl. slot 7 `(corridor = ())`; `set_pressurization 5 false` flipped slot 5 with PowerGrid row identical before/after; slot 7 round-trip; bad slot 9 rejected; `assign_room_type 2 '{"corridor":[]}'` rejected (variant names are camelCase in CLI JSON)
- [x] stdb suite green against new seed (8/8) before commit

## Subtask 1.5.2: stdb validation of the reducer loop — Scope: S ✅
- [x] `ConnectedGameHarnessController.cs`: `TestReducerActions` += `test_depressurize_kitchen`/`test_repressurize_kitchen` (slot 5) + `test_depressurize_corridor`/`test_repressurize_corridor` (slot 7); edge-detected `_PhysicsProcess` polling kept; `BuildPowerState()` rooms += `is_pressurized`
- [x] Scenario `scenarios_stdb/pressurization_reducer_round_trip.json` — confirmed red first (assertion_failure on missing path), green after harness changes; seed asserts (slots 5+7) → kitchen round trip with Stable/is_powered/breaker_on guards → corridor round trip
- [x] Full stdb suite was green against the new seed before this subtask (8/8, run during 1.5.1); screenshots reviewed — rendering intact, vacuum visually identical to initial as expected (tint lands in 1.5.4)

## Subtask 1.5.3: Client — corridor as room + registry refactor — Scope: M ✅
- [x] Create `client/game/Ship/RoomTypes/CorridorRoom.tres` — RoomId "Corridor", Label "Corridor", PowerDraw 0, TerminalType None, Color (0.30, 0.33, 0.38)
- [x] `RoomTypeRegistry.cs` — `[Export] Godot.Collections.Array<RoomType> RoomTypes` replaces `GD.Load` paths; all 8 `.tres` wired in `Main.tscn` + `{CornerSlide,ShipWalk,Interaction,RoomRender,Power}Harness.tscn`; `RoomTypeHarness.tscn` gained scene-declared registry node, controller switched `new` → `GetNode`
- [x] `ShipGrid.cs` — `CorridorSlotIndex => HullTemplate?.RoomSlots.Count ?? -1`; `_Draw()` tints corridor GridRects via `GetRoomColor(CorridorSlotIndex)` (no label); observed state appends corridor entry
- [x] `PowerGridService.cs` — `ApplyAssignment` + `SeedTestRoom` skip Corridor (modal rows stay 7)
- [x] `room_types_load.json` updated 7→8 + Corridor asserts (confirmed red before implementing); `rooms_assigned_to_slots.json` uses per-index asserts only — unaffected
- [x] Acceptance: builds clean; pure suite green 16/16; screenshot shows corridor with subtle neutral tint, rooms/terminals/breakers intact

## Subtask 1.5.4: Client — vacuum-tint rendering + test surface — Scope: S ✅
- [x] `ShipGrid.cs` — `VacuumTint`/`DepressurizedBlend` exports + `GetRoomColor` lerp before power branches; `SetTestAssignment` gains `bool isPressurized = true`; new `SetTestPressurization(slot, isPressurized)`; observed rooms + corridor entry += `is_pressurized`
- [x] `ShipGrid.tscn` — `VacuumTint = Color(0.35, 0.45, 0.6, 1)`, `DepressurizedBlend = 0.55`
- [x] `ModalHost.cs` — `public RoomModalInfo? CurrentInfo => _currentInfo;`
- [x] PowerHarnessController — pressurization test actions (kitchen + corridor) + corridor seed + `modal.pressure_nominal` observed (pulled forward from 1.5.5 so the tint scenario could go red→green in one cycle)
- [x] `pressure_depressurized_room_renders_vacuum_tint.json` — red first, green after; screenshots show warm Kitchen → cold desaturated vacuum, clearly distinct from power-dim
- [x] Acceptance: builds clean; full pure suite green 17/17

## Subtask 1.5.5: Pure validation — PowerHarness scenarios — Scope: M ✅
- [x] `PowerHarnessController.cs` — corridor seed + pressurization test actions + `modal.pressure_nominal` (landed with 1.5.4 so the first scenario could go red→green)
- [x] Scenario `pressure_depressurized_room_renders_vacuum_tint.json` — fingerprint `r_delta < 0` AND `b_delta > 0`; round trip `abs ≤ 0.001` (committed with 1.5.4)
- [x] Scenario `pressure_composes_with_unpowered.json` — vacuum → cut power: r drops below vacuum, b stays above powered baseline; repower returns exactly to vacuum color
- [x] Scenario `pressure_corridor_depressurizes.json` — `b_delta > 0` AND `(b_delta − r_delta) > 0` hue-shift assert (corridor base is near the tint hue); kitchen-stays-pressurized guard
- [x] Scenario `pressure_modal_shows_lost.json` — depressurize first, walk-up via `move_down` until "Kitchen Terminal" focused (terminal is straight down the corridor door path; no corner-wedge needed), modal `pressure_nominal == false`, Esc closes
- [x] `./tools/run_all_scenarios.ps1` green 20/20; screenshots reviewed — tint vs dim vs composed read distinctly; corridor band shifts cold blue; modal shows "Pressure: Lost"

## Subtask 1.5.6: End-to-end render assert + DoD sweep — Scope: S ✅
- [x] `ConnectedGameHarnessController.cs` — real Main ShipGrid observed state surfaced as `game.grid`; stdb scenario waits on `game.grid.rooms.5.is_pressurized` and asserts the kitchen r↓/b↑ fingerprint in the connected client (screenshot shows the tinted Kitchen in the real Main)
- [x] `./scripts/validate_all.ps1` — both suites green: 20/20 pure + 9/9 stdb, no regressions; all new checkpoints visually reviewed
- [x] Game boots clean 13s, zero `ERROR:` lines, DbManager connected + subscription applied, registry loaded 8 types (local dev DB was wiped during 1.5.1's `--delete-data=always` publish)
- [x] `dotnet build` + csharpier (client), `spacetime build` + format (server); stale RoomTypeRegistry anti-pattern note in `client/CLAUDE.md` updated to reflect the refactor
- [x] `tasks/plan.md` + `tasks/todo.md` checked off; `git push origin`

---

# Phase 2: Character Systems 🔄 PLANNED

Full plan: `C:\Users\upta\.claude\plans\elegant-weaving-tarjan.md`.

Design notes (user-confirmed):
- `Meter` shared SpacetimeDB type (`Current`/`Max` floats, fields-only — DbVector2 precedent) for Health/Oxygen/Hunger. Nested `with` updates accepted; no indexing on nested fields needed.
- Room tracking: client computes slot from position + HullTemplate via `RoomLocator`, calls `SetPlayerRoom(slotIndex)` on transitions only; stored as `Player.CurrentSlotIndex` (−1 = none).
- Suit rack lives in the CargoBay-assigned slot (Terminal/Breaker spawn pattern). Suit equip mutates `Oxygen.Max` directly (×2 default); speed factor 0.8 applied client-side via existing `_speedModifier`.
- `VitalsConfig` single-row table holds all tick tunables (DB-driven for fast validation, SetBlackoutGrace precedent). `VitalsTick` = repeating scheduled reducer (500ms default).
- Oxygen: refills in pressurized+powered rooms, holds in pressurized-unpowered, depletes otherwise; empty → Suffocation damage. Hunger depletes everywhere; empty → Starvation damage. Dead/disconnected players skipped.
- Ghost exception: ghosts can interact with the Cloning Bay terminal ONLY (avoids all-dead softlock, enables solo validation). Living players can clone dead crew via the CloningModal (`RequestRespawn(Identity target)`).
- `ShipStores.Biomass` seed 3; deposits land with Phase 3.4 (Load verb). Respawn needs CloningBay assigned + powered + biomass ≥ cost; resets vitals, teleports entity to bay center (client snaps on IsDead true→false).

# Task 2.1: Character health + damage pipeline ✅ DONE

## Subtask 2.1.1: Server — Vitals table + damage pipeline — Scope: M ✅
- [x] Create `server/src/Types/DamageType.cs` — `[SpacetimeDB.Type]` enum Debug/Suffocation/Starvation/Fire/Creature
- [x] Create `server/src/Types/Meter.cs` — `[SpacetimeDB.Type] partial struct Meter { float Current; float Max; }` (fields-only, object initializers — matches DbVector2; no ctor to keep codegen risk zero)
- [x] Create `server/src/Tables/Vitals.cs` — Accessor "VitalsRows", public; Identity PK, `Meter Health`, `bool IsDead`
- [x] Create `server/src/Character/DamageRules.cs` — `ApplyDamage(ctx, identity, amount, type)`: skip dead, clamp at 0, set IsDead; `EnsureVitals` seeding helper
- [x] Modify `server/src/Reducers/Connect.cs` — seed Vitals (100/100) if missing
- [x] Create `server/src/Reducers/ApplyDebugDamage.cs` + `ResetVitals.cs` — sender-auth debug/test paths
- [x] Format → build → publish `--delete-data=always` → generate → client build
- [x] Acceptance proven end-to-end by the stdb scenario (stronger than CLI): damage 30 → 70; kill → 0 + IsDead, further damage no-ops; reset restores

## Subtask 2.1.2: stdb validation — pipeline round trip — Scope: S ✅
- [x] `ConnectedGameHarnessController.cs`: `vitals` observed state (health/max_health/is_dead, Meter flattened) + test actions `test_damage_30`/`test_damage_kill`/`test_reset_vitals`
- [x] Scenario `scenarios_stdb/health_damage_pipeline_round_trip.json` — confirmed red first (path not found on `vitals.health` with connection healthy), green after publish + harness wiring
- [x] Full stdb suite green (10/10); screenshots reviewed — ship rendering intact, no visual change expected pre-HUD

## Subtask 2.1.3: Client — VitalsService + health bar HUD — Scope: M ✅
- [x] Create `client/game/Character/_Service/VitalsService.cs` — plain C#: Changed event, BindConnection/Unbind (local-identity filter on VitalsRows), SetTestVitals seeder
- [x] Create `client/game/Ui/VitalsHud.tscn` + `.cs` — CanvasLayer, bottom-left health bar (ColorRect fill/track + label, "DECEASED" dead presentation), color exports in scene
- [x] `Main.tscn`/`Main.cs`: declare VitalsHud, provide VitalsService, bind connection in InstantiatePlayer, unbind in _ExitTree
- [x] Create `VitalsHudHarness.tscn` + controller (provides seeded service, test_seed_health_full/40/zero actions); scenario `vitals_health_bar_renders.json` confirmed red first (missing harness), green after
- [x] Pure suite green (21/21); screenshots reviewed — 40% red fill + "HP 40/100", dead state empty track + "DECEASED"

## Subtask 2.1.4: DoD sweep — Scope: S ✅
- [x] `./scripts/validate_all.ps1` both suites green (21 pure + 10 stdb); connected-client screenshot shows HP 70/100 bar in real Main after reducer damage
- [x] Game boots clean 13s headless, zero ERROR lines, DbManager connected + subscription applied
- [x] Builds + csharpier both sides; plan/todo checked; push

# Task 2.2: Oxygen tether + spacesuits ✅ DONE

## Subtask 2.2.1: Server room tracking — Scope: S ✅
- [x] `Player` table += `int CurrentSlotIndex` (seed −1); `SetPlayerRoom.cs` reducer (validate −1 or existing slot)
- [x] Publish + generate + builds; acceptance proven end-to-end by `player_room_tracking.json`

## Subtask 2.2.2: Client RoomLocator + stdb proof — Scope: M ✅
- [x] Create `client/game/Ship/_Service/RoomLocator.cs` — position → slot (RoomSlot rects, corridor rects + door cells → corridor slot, else −1); TileSize const must match ShipGrid
- [x] `Player.cs`: `Hull` property set by Main, `TrackCurrentRoom()` in `_PhysicsProcess` calls `SetPlayerRoom` on change only
- [x] Observed `vitals.current_slot` (server Players row); scenario `scenarios_stdb/player_room_tracking.json` red first, green after — corridor 7 → Kitchen 5 → corridor 7. Gotcha confirmed: wait for `game.terminal_count` before pressing movement (GUIDE context must be active when the bridged key-press edge fires — held state never re-registers); diagonal wedge into the door per 1.4.4
- [x] Full stdb suite green (11/11)

## Subtask 2.2.3: Server oxygen model + VitalsTick — Scope: M ✅
- [x] `Vitals` += `Meter Oxygen` (100/100), `bool SuitEquipped`
- [x] Create `VitalsConfig.cs` (single-row tunables: 500ms tick, drain 0.85/tick ≈60s tank, refill 5/tick, suffocation 2/tick ≈25s, suit ×2 / speed 0.8, biomass cost 1) + `VitalsTickTimer.cs` (repeating `ScheduleAt.Interval`) + `VitalsTick.cs` (pressurized+powered refills, pressurized-unpowered holds, else depletes; empty → Suffocation via pipeline; dead/disconnected skipped)
- [x] Create `SetSuitEquipped.cs` (mutates Oxygen.Max ×2 / restores + clamps), `SetVitalsConfig.cs` (re-schedules tick on interval change), `SetOxygen.cs`; `Init.cs` seeds config + tick row; `VitalsRules.cs` holds defaults + reschedule helper
- [x] Publish + generate + builds; acceptance proven end-to-end by the two stdb scenarios

## Subtask 2.2.4: stdb validation — oxygen loop — Scope: M ✅
- [x] Harness: vitals += oxygen/max_oxygen/suit_equipped; test actions `test_fast_vitals` (250ms/5/25/5)/`test_set_oxygen_low`/`test_equip_suit`/`test_unequip_suit`
- [x] `oxygen_depletes_and_refills.json` (walk to Kitchen → depressurize → drains → repressurize → refills, health guarded) + `oxygen_empty_suffocates.json` (corridor vacuum + low tank → health falls → repressurize → health holds, assert_pipeline delta 0) — red first, green after; suite green (13/13)

## Subtask 2.2.5: Client — oxygen HUD + suit rack — Scope: M ✅
- [x] VitalsHud oxygen bar (cyan, "[SUIT]" tag); VitalsService += oxygen/suit/SuitSpeedFactor (reads VitalsConfigs) + SetTestOxygen
- [x] Create `client/game/Ship/SuitRack/SuitRack.tscn` + `.cs` (Breaker pattern; rack hanger empties when suit taken); ShipGrid spawns in CargoBay-assigned slot top-right interior, frees on reassignment; `SuitRackInteracted` event + `SetSuitRackState`
- [x] `Main.cs` → `SetSuitEquipped` reducer + `OnVitalsChanged` syncs player/rack; `Player.cs` `SetSuitEquipped(equipped, factor)` — speed modifier + orange suit tint (%Sprite bound)
- [x] Pure scenarios red first → green: `vitals_oxygen_bar_renders.json` (ratio tracks Current/Max incl. suit tank 150/200) + `suit_rack_equip_toggles.json` (speed_modifier observed 1.0→0.8→1.0; suit tint screenshot reviewed). Navigation gotcha: diagonal toward a convex door-corner WEDGES (inverse of the helpful wall-slide); sequence straight-line legs + wall-slide wedges instead
- [x] Pure suite green (23/23)

## Subtask 2.2.6: stdb suit round trip + DoD sweep — Scope: S ✅
- [x] `scenarios_stdb/suit_equip_round_trip.json` — real walk to rack in connected Main, interact → server SuitEquipped + Oxygen.Max 100→200 → interact → restored; screenshot shows suited player at emptied rack with "O2 100/200 [SUIT]" HUD
- [x] `./scripts/validate_all.ps1` green (23 pure + 14 stdb); boots clean 13s zero ERROR; builds + csharpier; push

# Task 2.3: Food/hunger meter ✅ DONE

## Subtask 2.3.1: Server — hunger on the tick — Scope: S ✅
- [x] `Vitals` += `Meter Hunger` (100/100); `VitalsTick` → `TickPlayerVitals` (oxygen + hunger in one row write); hunger depletes everywhere (0.17/tick ≈5min), 0 → Starvation via pipeline; `ResetVitals` now restores all three meters
- [x] Create `RestoreHunger.cs` (Phase 3 meal entry point + validation path) + `SetHunger.cs` debug setter; publish + generate + builds

## Subtask 2.3.2: Validation + HUD + mini-sweep — Scope: S ✅
- [x] VitalsHud hunger bar (amber FOOD, third perimeter bar); pure scenario `vitals_hunger_bar_renders.json` red first → green; screenshot shows HP/O2/FOOD row
- [x] Harness hunger observed + `test_fast_hunger` (oxygen rates zeroed to isolate starvation)/`test_set_hunger_low`/`test_restore_hunger`; `scenarios_stdb/hunger_starvation_round_trip.json` — hunger → 0 → health falls (Starvation) → restore → health holds
- [x] **Suite-isolation gotcha found + fixed:** stdb scenarios share one ephemeral DB and one client identity, so config changes and vitals damage LEAK between scenarios (fast-hunger config starved the oxygen scenario's health guard). Every timing-sensitive vitals scenario now sets its own config and fires `test_reset_vitals` up front — preconditions are the scenario's own job
- [x] DoD sweep: both suites green (24 pure + 15 stdb), boot clean 13s zero ERROR, builds + format, push

# Task 2.4: Death, ghost state, cloning bay ✅ DONE

## Subtask 2.4.1: Server — ShipStores + respawn — Scope: M ✅
- [x] Create `server/src/Tables/ShipStores.cs` — Id PK 0, `int Biomass` (seed 3 in Init via `GetShipStores`)
- [x] Create `server/src/Reducers/RequestRespawn.cs` — `(Identity target)` form (living crew clone others; dead sender may self-respawn — the ghost exception); validates target dead + CloningBay assigned/powered + biomass ≥ cost with distinct messages; deducts, resets all meters, entity → bay slot center, velocity zeroed
- [x] Create `server/src/Ship/HullGeometry.cs` — Corvette slot-center constants (why-comment → CorvetteHull.tres)
- [x] Create `SetBiomass.cs` debug setter; publish + generate + builds; `Main` snaps local player to server entity on IsDead true→false (`ResetPhysicsInterpolation` for clean teleport)

## Subtask 2.4.2: stdb validation — respawn rules — Scope: S ✅
- [x] Harness: `stores.biomass` observed + test actions (`test_request_respawn` self-target, `test_set_biomass_zero`/`_full`, `test_toggle_breaker_2`)
- [x] `cloning_respawn_round_trip.json` (red first) — kill → respawn → biomass 3→2, vitals full, player node snapped to exactly (144,−144) bay center; `cloning_respawn_requires_power_and_biomass.json` — unpowered rejected, biomass-0 rejected, both restored → succeeds. Defensive preconditions (reset + biomass seed) per the suite-isolation convention

## Subtask 2.4.3: Client ghost mode — Scope: M ✅
- [x] `Player.SetGhostMode` — collision layer/mask cleared/restored, translucent `GhostColor` tint (composes with suit via `UpdateSpriteColor`); `InteractionRegistration.GhostAccessible` + `InteractionService.IsGhost` filtering (focus + trigger); Cloning terminal opts in via `Terminal.SetRoomState`
- [x] `RemoteEntity.SetGhost` tint; `Main` maps VitalsRows → Players.PlayerEntityId → remote node (incl. already-dead on node spawn); VitalsHud "DECEASED" landed in 2.1
- [x] `GhostHarness` (provides VitalsService, mirrors Main's vitals→ghost wiring) + `ghost_passes_through_walls.json` (alive blocked at wall, ghost floats into Bridge — screenshot shows translucent ghost inside the room) + `ghost_cannot_interact.json` (Kitchen terminal: no focus/prompt/modal; Cloning terminal focuses + opens). Navigation lesson: wait on `focused_label` during the final approach leg instead of position waits (release overshoot at 400px/s is ~40-90px)

## Subtask 2.4.4: Client — CloningModal — Scope: M ✅
- [x] `VitalsService` grew the crew roster (all VitalsRows; "You"/"Crew xxxxxx" labels) + `Biomass` (ShipStoresRows) + `RequestRespawn(key)` (reducer connected / local mirror in test mode)
- [x] `CloningModal.tscn` + `.cs` (PowerRouterModal pattern; rows rebuild on roster change, first Clone button grabs focus, "No deceased crew" empty state) + `CloningRow`; `ModalHost.tscn` Cloning slot repointed; `ModalHost.CurrentModal` exposed for observation
- [x] Pure `cloning_modal_lists_dead.json` — walk to bay terminal alive → modal lists seeded dead crew + Biomass: 3 → Clone → row gone, biomass 2 → Esc closes; stdb `cloning_modal_respawn.json` — die → ghost floats through walls to bay → interact (ghost exception) → modal → accept → server respawn, biomass deducted; screenshot shows DECEASED HUD + modal with You/Clone row

## Subtask 2.4.5: DoD sweep + Phase 2 checkpoint — Scope: S ✅
- [x] `./scripts/validate_all.ps1` both suites green (27 pure + 18 stdb), no regressions; new checkpoints visually reviewed
- [x] Game boots clean 13s, zero ERROR lines, DbManager connected + subscription applied
- [x] Builds + csharpier both sides; plan/todo checked incl. **Checkpoint: Characters**; push

**Checkpoint: Characters** ✅ — Characters have vitals (health, oxygen, hunger) rendered on a perimeter HUD, suffocate in vacuum and starve when unfed through one typed damage pipeline, equip spacesuits trading speed for tank capacity, die into ghost mode (float through walls, cloning-terminal-only interaction), and respawn at the Cloning Bay for biomass. Phase 2 complete.

---

# Phase 3: Inventory & Logistics 🔄 PLANNED

Full plan: `C:\Users\upta\.claude\plans\polished-watching-liskov.md` (note: the plan file's headings predate the renumbering — its "3.2 Item types" is Task 3.1 here, its "3.1 hotbar" is Task 3.2). untrailed is the reference, deliberately improved (one Item row vs container indirection; explicit-slot reducers with server reach/alive checks vs client-trust holes; `InventoryConfig` table vs hardcoded capacities; `[Export]` registry vs switch statement; no stacking per GDD).

Design notes (user-confirmed):
- One `Item` table: `LocationKind` World/Hotbar/Stored + `Holder`/`SlotIndex`/`RoomSlotIndex` discriminator fields, `DbVector2 Position`. No Quantity. `InventoryConfig` (Id 0): HotbarSlots 4, PickupRadius 96, LoadRadius 160, CargoCapacity 12.
- Holistic deposits: storage rooms (Cargo Bay) hold withdrawable items; machine intakes are tanks (Reactor+FuelCell→`ShipStores.Fuel`, CloningBay+Biomass→`Biomass`). One `LoadItem(hotbarSlot, roomSlot)` verb, branch on room type, server reach check vs `SlotCenter` — never trusts modal-open state.
- Reactor burns fuel on a scheduled tick while generating; dry ⇒ output 0 ⇒ overload→blackout (1.4 flow). `FuelPerBurn = 0` disables — power-asserting stdb scenarios add one `test_disable_fuel_burn` precondition.
- Death drops hotbar items at death position (`DamageRules.ApplyDamage` is the single IsDead-flip site); stored items unaffected; ghosts can't pick up.
- Pickup = walk-up + E interact. Hotbar selection client-only; direct-select keys 1–4 + drop Q (repurpose leftover `HotbarDropItem.tres`, delete `HotbarCycleSlot.tres` — stale mappings would shadow new bindings).
- Meals moved whole to 4.4. Phase 3 types: RawOre, FuelDeposit, Biomass, FuelCell, Scrap, Components.
- Gotcha to honor: pickup/deposit are row UPDATEs — `InventoryService` must evict world entries on OnUpdate, not just OnDelete.

# Task 3.1: Item types

## Subtask 3.1.1: Server — item schema + world-spawn debug reducers — Scope: M ✅
- [x] `ItemTypeId`/`ItemLocationKind` enums, `Item` table (full schema, one publish), `InventoryConfig`, `ItemRules` (config getter + `FindFreeHotbarSlot`), `TerminalType` += Storage
- [x] `SpawnWorldItem` + `ClearItems` reducers; Init seeds config
- [x] Format → build → publish `--delete-data=always` → generate → client build; CLI acceptance: config row (4/96/160/12) visible, `spawn_world_item rawOre` inserts, `none` rejected, `clear_items` empties

## Subtask 3.1.2: stdb validation — Scope: S ✅
- [x] Harness `items` observed state (world_count + by_type) + `game.world_item_nodes` (ItemSpawner child count) + spawn/clear test actions; `world_items_spawn_and_render.json` confirmed red first (timeout on world_item_nodes, server half already green), green after 3.1.3

## Subtask 3.1.3: Client — ItemType resources + WorldItem + ItemSpawner + InventoryService (world half) — Scope: M ✅
- [x] `ItemType` resource + `ItemTypeRegistry` + 6 `.tres`; `WorldItem.tscn` (InteractTarget, "Pick up {Label}", GhostAccessible false)
- [x] `InventoryService` world half (OnUpdate evicts non-World rows — the untrailed gotcha) + test seeders; `ItemSpawner` node in `Main.tscn` ([Dependency] service, registry handed by Main)
- [x] Main provides service, binds connection in InstantiatePlayer, hands registry to spawner; full stdb suite green 19/19; screenshots reviewed (FuelCell glyph tile in Kitchen, pickup prompt over ore, clear empties)

## Subtask 3.1.4: Pure validation — Scope: S ✅
- [x] `InventoryHarness` + controller (provides Interaction + Inventory services, seed/remove/clear test actions, observed item_types + world_items w/ color/glyph/position + focused_label); `item_types_load.json` (6 types, ids/labels/glyphs) + `world_items_render.json` (seed 2 → nodes at positions w/ color fingerprint → remove → freed) confirmed red first (missing harness); pure suite 29/29; screenshots reviewed

## Subtask 3.1.5: DoD sweep — Scope: S ✅
- [x] `validate_all.ps1` both suites green (29 pure + 19 stdb); boot clean 13s zero ERROR, DbManager connected + subscription applied, ItemTypeRegistry loads 6 types; builds + csharpier both sides; plan/todo checked; push

# Task 3.2: Fixed-size hotbar ✅ DONE

## Subtask 3.2.1: Server — GiveItem debug reducer — Scope: S ✅
- [x] `GiveItem(typeId, slotIndex)` — known-player + alive + type-not-None + slot bounds (vs `InventoryConfig.HotbarSlots`) + occupancy checks, distinct throw messages; reducer-only publish + generate + builds

## Subtask 3.2.2: stdb validation — Scope: S ✅
- [x] Harness `items.hotbar` (slot→type for local identity) + `items.hotbar_count` + `items.config.hotbar_slots` observed; test actions `test_give_biomass_slot0`/`test_give_ore_slot2`/`test_give_ore_slot0` (occupied probe)
- [x] `hotbar_state_round_trip.json` red first (timed out at the `game.hotbar` HUD wait with the server half already green — pulled-forward pattern), fully green after 3.2.3; occupied-slot give leaves slot 0 = Biomass and count = 2; clears items at scenario end (suite-isolation convention)

## Subtask 3.2.3: Client — service hotbar half + HotbarHud + GUIDE actions — Scope: M ✅
- [x] InventoryService hotbar half: `_hotbarItems` keyed by itemId (local-identity + Hotbar filter on the same Items subscriptions), `Slots`/`HotbarSlotCount` (from InventoryConfigs, default 4), `SelectedSlot` (pure client UI state)/`SelectSlot`/`SetTestSlot`
- [x] `HotbarSlot1..4.tres`; KBM context rewired (keys 1–4, drop G→Q), controller context (slots → dpad, drop stays X); `HotbarCycleSlot.tres` + its Tab/axis-5 mappings deleted; `EnsureInputActions` += `hotbar_slot_1..4` + `hotbar_drop`
- [x] `ItemSlotPanel.tscn`/`.cs` shared slot visual (type color fill + glyph, empty state, selection ring; colors scene-exported); `HotbarHud.tscn`/`.cs` (CanvasLayer, bottom-center HBox of 4 panels, `[Export] GuideActionBinding` ×5, `[Dependency]` InventoryService, registry handed by Main, `DropRequested` inert until 3.3) declared in `Main.tscn`

## Subtask 3.2.4: Pure validation — Scope: M ✅
- [x] InventoryHarness gains HotbarHud + ModalHost (TerminalInteracted wired), hotbar key bridges, slot-seed + `test_open_kitchen_modal` test actions (drives the real `ModalHost.Open` exclusive push — the walk-up→modal flow is already covered by `terminal_interact_opens_modal`), observed `hotbar` (per-slot occupied/glyph/color/selected via `HotbarHud.GetObservedState()`) + `modal`
- [x] `hotbar_renders_items.json` (4 empty slots → seed Biomass@0 + Ore@2 → glyph/color-fingerprint asserts → key 2 moves the selection ring) + `hotbar_inert_while_modal_open.json` (modal open mutes key 3, Esc restores it) — both red first; screenshots reviewed
- [x] stdb scenario asserts `game.hotbar.slots.*` from the real Main HUD

## Subtask 3.2.5: DoD sweep — Scope: S ✅
- [x] `validate_all.ps1` both suites green; boot clean zero ERROR; builds + csharpier both sides; uids imported; plan/todo checked; push

# Task 3.3: Item pickup/drop ✅ DONE

## Subtask 3.3.1: Server — PickUpItem / DropItem + death-drop — Scope: M ✅
- [x] `PickUpItem(itemId)` — known-player + alive + World-located + reach (distance² vs `PickupRadius`²) + `FindFreeHotbarSlot`; `DropItem(slotIndex)` — alive + held-at-slot, drop position read from the server's own Entities row (no client coordinates)
- [x] `DropAllHotbarItems(ctx, holder, position)` with deterministic `slotIndex * 12px` X spread; hooked into `DamageRules.ApplyDamage` on the alive→dead transition, guarded so missing Player/Entity rows skip the drop but never the damage; reducer-only publish + generate + builds

## Subtask 3.3.2: stdb validation — Scope: M ✅
- [x] Harness: `test_pickup_nearest_world_item` (client-side nearest-row lookup → direct `PickUpItem`), `test_drop_slot0`, `test_fill_hotbar` (4× Scrap), `test_spawn_ore_far` (2000,2000); `hotbar_drop`→Q key bridge; `items.world` ItemId-ordered {type,x,y} array in observed state
- [x] `item_pickup_drop_round_trip.json` red first (focused + E pressed, hotbar stayed 0 — exactly the missing 3.3.3 wiring), green after; covers prompt label, HUD glyph, world-node free, prompt clear, walk-away Q drop landing at the server entity position
- [x] `item_pickup_rejected_out_of_reach.json` (direct probe vs far ore — the closed untrailed hole), `item_pickup_rejected_when_full.json` (direct probe with ore in reach + focused, isolating the full-slot rule), `death_drops_hotbar.json` (give 2 → walk to Kitchen → kill → 2 world rows at the body with the 24px slot-2 spread, ghost gets no focus, respawn at Cloning Bay leaves items in the Kitchen) — all green on the server slice alone, as expected for server-rule probes

## Subtask 3.3.3: Client — pickup/drop wiring — Scope: M ✅
- [x] `InventoryService.RequestPickUp(itemId)` / `RequestDrop(slotIndex, position)` — connected mode calls the reducers (drop position server-resolved; the position arg only feeds the test mirror), test mode mirrors world↔hotbar moves via `FindFreeTestSlot`
- [x] Main wires `ItemSpawner.Interacted` → RequestPickUp and `HotbarHud.DropRequested` → RequestDrop(SelectedSlot, player pos), unhooked in `_ExitTree`; cleanup chain (row UPDATE → service evicts → spawner frees node → InteractTarget unregisters → prompt clears) proven in-scenario by the `focused_label == ""` wait

## Subtask 3.3.4: Pure validation — Scope: M ✅
- [x] InventoryHarness mirrors Main's pickup/drop wiring; `test_kill`/`test_revive` (direct `SetGhostMode`); `player.is_ghost` observed
- [x] `item_pickup_prompt_and_mirror.json` (walk-up prompt → E → slot 0 glyph + node freed + prompt gone → walk right → Q → node at player position) + `ghost_cannot_pickup.json` (ghost over ore: no focus/prompt; revived at same spot: focuses) — both red first; screenshots reviewed

## Subtask 3.3.5: DoD sweep — Scope: S ✅
- [x] `validate_all.ps1` both suites green (pure PASS + stdb 24/24); boot clean zero ERROR (connected + subscribed); builds + csharpier both sides; import pass (no new uids needed); plan/todo checked; push

# Task 3.4: Load verb — tank deposits + reactor fuel burn ✅ DONE

## Subtask 3.4.1: Server — ShipStores.Fuel + LoadItem + FuelBurnTick — Scope: M ✅
- [x] `ShipStores.Fuel` (seed 10); `PowerGrid.FuelBurnMillis` (120000, ulong) + `FuelPerBurn` (1; 0=off); `FuelBurnTimer` scheduled table (VitalsTickTimer precedent), `RescheduleFuelBurn` in PowerRules
- [x] `LoadItem(hotbarSlot, roomSlot)` — tank branch (`AcceptsTankDeposit`: CloningBay+Biomass, Reactor+FuelCell), reach vs `SlotCenter` within LoadRadius (never trusts modal-open state), fuel deposits recompute grid (dry-tank recovery)
- [x] `FuelBurnTick` (burn while reactor assigned + breaker on; crossing 0 recomputes); `ComputeLoad` output gains the fueled condition (`Fuel > 0 || FuelPerBurn == 0`); `SetFuel`/`SetFuelBurn` setters (interval 0 = keep current; both recompute)
- [x] Publish `--delete-data=always` → generate → builds; stdb suite 25/25 green against new seed before commit

## Subtask 3.4.2: stdb validation — Scope: M ✅
- [x] `load_reducer_validation.json` (wrong-type + reach rejections from the corridor spawn, then walk into the Reactor room — wall-clamp up+right through the door — and load: fuel +1, slot empty) red first
- [x] `reactor_fuel_burn_blackout_recovery.json` — fast burn (500ms) drains 2→0, grid leaves Stable through Overload into Blackout, slow-burn re-pace kills the tick race, deposit recovers Stable + rooms repowered; burn disabled + fuel restored at scenario end
- [x] Harness: `stores.fuel` + `power.fuel_per_burn`/`fuel_burn_millis` observed; load/give/set-fuel/burn-tuning test actions; six power-asserting scenarios gain the `test_disable_fuel_burn` precondition

## Subtask 3.4.3: Client — deposit UI + fuel readout — Scope: M ✅
- [x] `RoomModalInfo` += `SlotIndex` (5 construction sites incl. InventoryHarness's kitchen modal); shared `DepositRow.tscn`/`.cs` (held-count label + focusable Deposit button, in-place `Update`)
- [x] InventoryService `CountOf`/`FirstSlotOf`/`RequestLoad` (+ `TestLoadRequested` mirror event); CloningModal biomass row (first focus when no crew rows); PowerRouterModal fuel section with red DRY cue (`PowerGridService` mirrors ShipStores.Fuel + FuelPerBurn); Ghost/Power harnesses provide InventoryService
- [x] `load_biomass_modal_round_trip.json` (stdb, red first) — carry biomass → Cloning terminal → deposit → store 3→4, slot empty, modal stays open

## Subtask 3.4.4: Pure validation — Scope: M ✅
- [x] `cloning_modal_deposit_biomass.json` (row enabled+focused → accept → store 4, row disables in place, modal open) + `power_router_modal_deposit_fuel.json` (fuel readout, focus-up to deposit row, fuel 5→6) — red first via missing harness probes; screenshots reviewed

## Subtask 3.4.5: DoD sweep — Scope: S ✅
- [x] `validate_all.ps1` both suites green (35 pure / 28 stdb); boot clean zero ERROR (connected + subscribed, Init seeds fuel + burn timer); builds + csharpier both sides; import pass (DepositRow uid staged); plan/todo checked; push

# Task 3.5: Cargo Bay storage (store/withdraw) ✅ DONE

## Subtask 3.5.1: Server — storage branch + WithdrawItem — Scope: M ✅
- [x] `AcceptsStorage` (CargoBay; rooms opt in here) + `FindFreeStoreSlot` (vs `InventoryConfig.CargoCapacity`); `LoadItem` storage branch — room accepts storage → UPDATE to Stored at the free store slot (any type, capacity-checked); tank branch unchanged
- [x] `WithdrawItem(itemId)` — alive + Stored + reach vs `SlotCenter(item.RoomSlotIndex)` within `LoadRadius` + `FindFreeHotbarSlot` → UPDATE to Hotbar; reducer-only publish + generate + builds; death-drop already only iterates Hotbar rows so Stored items stay the ship's

## Subtask 3.5.2: stdb validation — Scope: M ✅
- [x] Harness: `items.stored` (room → slot-ordered {item_id,type,slot}) + `stored_count` + `config.cargo_capacity` observed; test actions `test_store_slot0_to_cargo`/`test_load_slot0_to_kitchen`/`test_withdraw_first_stored`/`test_fill_cargo` (12× give+store pairs ride per-connection reducer ordering)
- [x] `cargo_store_withdraw_round_trip.json` — first run red (door overshoot: wait_until polls the lagging server x, player wall-clamped right of the CargoBay door); fixed with the overshoot-then-down+left wall-slide (mirror of the Reactor entry); store empties hotbar, withdraw restores RawOre
- [x] `cargo_store_rejections.json` — out-of-reach store from spawn, ore→Kitchen (accepts neither storage nor that tank item), 13th store into a full cargo (12/12 stays), withdraw with full hotbar (item stays Stored)

## Subtask 3.5.3: Client — StorageModal (untrailed dual-grid, improved) — Scope: M ✅
- [x] `ItemSlotButton.tscn`/`.cs` (focusable Button wrapping the shared `ItemSlotPanel`; selection ring on focus; empty slots leave focus navigation) + `ItemSlotGrid.tscn`/`.cs` (`SetSlots` in-place — fixed counts per grid mean no rebuild, focus survives; `SlotPressed(index)`)
- [x] `StorageModal.tscn`/`.cs` — title "Cargo Storage (N/12)", hotbar + cargo grids, hotbar press → `RequestStore`, cargo press → `RequestWithdraw`, first-occupied focus on open, focus re-homes when the focused slot empties, empty-cargo label
- [x] `CargoBayRoom.tres` TerminalType Info→Storage (the behavioral switch — left for last as the pure scenario's red lever); ModalHost `Storage` export + route; InventoryService `StoredIn`/`RequestStore`/`RequestWithdraw`/`SeedTestStoredItem`/`CargoCapacity` + Stored eviction in `Apply`; Main + InventoryHarness provide `ItemTypeRegistry` (modal resolves type colors/glyphs via `[Dependency]`)

## Subtask 3.5.4: Pure validation + multiplayer checkpoint — Scope: M ✅
- [x] `storage_modal_store_withdraw.json` red first (storage_title missing — Info modal opened on the unflipped .tres), green after the flip; deposit updates title 0/12→1/12 + both grids in place, modal_down + accept withdraws, Esc closes; screenshots reviewed (focus ring, dual grid, HUD hotbar mirror)
- [x] `scenarios_stdb/items_multiplayer_visibility.json` — PuppetClient exposes its own table view (`world_item_count`/`stored_item_count`); puppet sees the main client's drop become World and store become Stored — the **Checkpoint: Inventory** second-client proof

## Subtask 3.5.5: DoD sweep + Phase 3 checkpoint — Scope: S ✅
- [x] Full `validate_all.ps1` both suites green (36 pure + 31 stdb); boot clean zero ERROR; builds + csharpier both sides; uids imported; plan/todo checked incl. **Checkpoint: Inventory**; push

---

# Phase 4: Resource Economy 🔄 PLANNED

Full plan: `C:\Users\upta\.claude\plans\nested-sauteeing-sketch.md`.

Closes the economy loop — Harvest (nodes → raw materials) → Refine/Craft (workshop/kitchen benches → consumables) → Load (3.4 sinks) → Consume (meals → hunger). Prerequisite for Phase 5 node activities.

Design notes (user-confirmed 2026-06-12):
- **Node placement:** ship-interior placeholder positions, Init-seeded on open east-corridor floor (clear of door lanes + the 3 dev items). Position-agnostic — Phase 5.2 relocates spawning to exterior grids with no rework.
- **Channeled harvest + progress ticker:** `StartHarvest` pins `StartedAt`/`CompletesAt` + `Progress=0` on a one-per-player `ActiveHarvest`; one shared repeating `ChannelTick` (~150ms) recomputes `Progress = clamp((now−StartedAt)/dur)` and completes when `now ≥ CompletesAt`. **No one-shot timers, no `TimerId` bookkeeping, no module-identity guard** (completion gated on real server time → client-invoking the ticker is harmless). Client reads `Progress` off the row (no clock-skew math); movement cancels (`CancelHarvest`), server re-checks reach at completion.
- **Bench input/output storage:** Workshop/Kitchen benches reuse the Cargo Bay stack (`Item.LocationKind.Stored` on the bench `RoomSlotIndex`, `FindFreeStoreSlot`, `ItemSlotGrid`, `WithdrawItem`). Config-defined `SlotIndex` ranges split **input** (`BenchInputSlots`, e.g. 0–3, accepts only the bench's recipe ingredients) from **output** (`BenchOutputSlots`, e.g. 4–7, reserved completion target — no full-store race). `QueueCraft` sources each ingredient hotbar-first then bench-input. No `Item` schema change.
- **Kitchen owns meals:** Kitchen terminal → Fabricator (Biomass → Meal); Workshop → Fuel Cell (FuelDeposit + RawOre). Recipes carry a bench `RoomTypeId`; one system, two benches.
- **RadialProgress port:** trail's `radial_progress.gd`+`.gdshader` → C#, `[Export] ProgressMode` Chunked (tweened easing toward each discrete target, original behavior) / Continuous (direct set). Ring fed by server `Progress`; Chunked smooths the per-tick updates. ShaderMaterial declared in `.tscn` (`resource_local_to_scene`), not built in `_Ready`.
- **Eat:** new GUIDE action `HotbarUse` on **F**; InputMap `hotbar_use`.
- **Publish ritual** every schema change: `spacetime publish nomad --delete-data=always --yes --server local --module-path ./src` + regenerate bindings + re-green stdb suite before commit. `Meal` appended at END of `ItemTypeId`.

## Task 4.1: Resource nodes — Scope: M ✅ DONE

### Subtask 4.1.1: Server — node table + seeding — Scope: S ✅
- [x] `Types/ResourceNodeTypeId.cs` — None, OreVein, WreckageDebris, FuelDepositNode, BiomassPatch
- [x] `Tables/ResourceNode.cs` — `NodeId` PK AutoInc int, `ResourceNodeTypeId`, `Position DbVector2`, `YieldRemaining`/`YieldMax` int, `Public = true`, accessor `ResourceNodes` (SQL/subscribe name `resource_nodes`)
- [x] `Harvest/HarvestRules.cs` — `YieldItemFor(ResourceNodeTypeId)` switch → RawOre/Scrap/FuelDeposit/Biomass (consumed by 4.2's channel; documents the mapping now)
- [x] `Reducers/SpawnResourceNode.cs` (+ `yieldMax` arg) + `ClearResourceNodes.cs` (mirror `SpawnWorldItem`/`ClearItems`: known-player auth, reject None / `yield <= 0`)
- [x] `Init.cs` seeds 4 nodes: OreVein (96,0), WreckageDebris (192,0), FuelDepositNode (256,0), BiomassPatch (384,0), yield 5
- [x] Publish `--delete-data=always` + generate + builds; `spacetime sql` shows 4 nodes. Reducers gate on known-player auth, so spawn/clear round-trip is proven by the stdb scenario (not raw CLI — same as SpawnWorldItem). `SetNodeYield` debug reducer NOT needed for 4.1 (depletion proof is the pure scenario via service seeders; revisit if 4.2 wants server-driven depletion validation)

### Subtask 4.1.2: stdb validation — Scope: S ✅
- [x] `ConnectedGameHarnessController`: `nodes` observed section (count + `list` per-node type/yield/x/y ordered by NodeId) + `game.resource_node_nodes` render count + `test_spawn_ore_node`/`test_clear_nodes`
- [x] `scenarios_stdb/resource_nodes_spawn_and_render.json` — seed asserts (4 nodes, per-type x/yield) → render 4 in connected Main → spawn → 5 → clear → 0. Green; rendered.png shows nodes in the real Main east corridor

### Subtask 4.1.3: Client — node rendering + service half — Scope: M ✅
- [x] `client/game/Harvest/ResourceNodeType.cs` (`[GlobalClass]` Resource: Color/Glyph/Label/NodeId/YieldItemId) + 4 `.tres` + `ResourceNodeTypeRegistry` (`[Export]` array — wired in Main.tscn + HarvestHarness.tscn)
- [x] `ResourceNode.tscn`/`.cs` (Node2D, WorldItem pattern: `Visual` wrapper(ColorRect + glyph) + `InteractTarget`, label "Harvest {Label}", **no blocking collider** — Area2D on the Interactable layer only; depletion color + scale lerp full→husk from YieldRemaining/YieldMax). Interact routed to a no-op `Interacted` event (harvest lands in 4.2)
- [x] `_Service/HarvestService.cs` (plain C#: `Changed`, `BindConnection` on `ResourceNodes`, `ResourceNodeEntry` list, seeders `SeedTestNode`/`SetTestYield`/`ClearTestNodes`). Aliased `StdbResourceNode` to dodge the scene-class/row-type name clash
- [x] `ResourceNodeSpawner` (ItemSpawner pattern, update-in-place on yield) declared in `Main.tscn`; `Main.cs` provides `HarvestService`, binds on connect, unbinds in `_ExitTree`

### Subtask 4.1.4: Pure validation — Scope: S ✅
- [x] `HarvestHarness.tscn` + controller (InventoryHarness pattern; provides Interaction + Harvest; Player only needs Interaction)
- [x] `resource_node_types_load.json` (4 types: ids/labels/glyphs/yield-item-ids) + `resource_nodes_render_depletion_states.json` (full → yield 1 partial → yield 0 husk: monotonic r/b darkening + scale shrink + husk≈0.18 fingerprint). Screenshots reviewed — OreVein depletes to a dark shrunk husk while W/F/B stay full

### Subtask 4.1.5: DoD sweep — Scope: S ✅
- [x] stdb suite fully green (new node scenario + zero regressions — the seeded corridor nodes don't steal interaction focus in any cargo/cloning/load navigation scenario). Pure suite: my 2 new scenarios + every non-flaky scenario green
- [x] **Pre-existing flake caveat:** 3 PowerHarness modal scenarios (`power_router_modal_deposit_fuel`, `power_router_modal_toggles_breaker`, `pressure_modal_shows_lost`) flake on bridged modal-key navigation. **Proven pre-existing** — they fail identically on a clean-HEAD build with all 4.1 work stashed; none load any 4.1 code. NOT a 4.1 regression
- [x] Game boots clean ≥13s headless, zero ERROR, DbManager connected + subscription applied, registry loaded 4 node types
- [x] `dotnet build` + csharpier (client), `spacetime build` + format (server); uids imported (`*.cs.uid` sidecars staged)
- [x] plan/todo ticked; `git push origin`

## Task 4.2: Harvest verb (channeled) — Scope: L ✅ DONE

### Subtask 4.2.1: Server — channel + shared ticker — Scope: M ✅
- [x] `Tables/HarvestConfig.cs` (single-row Id 0: `HarvestMillis` 2000, `HarvestRadius` 96f, `TickMillis` 150; `GetHarvestConfig`/`RescheduleChannelTick` in HarvestRules) + `Reducers/SetHarvestConfig.cs` (any non-positive arg = keep current, SetFuelBurn convention)
- [x] `Tables/ActiveHarvest.cs` (`Identity` PK, `NodeId`, `StartedAt`/`CompletesAt Timestamp`, `Progress float`, `Public = true`)
- [x] `Tables/ChannelTickTimer.cs` (private, repeating `Scheduled = nameof(Module.ChannelTick)`, `ScheduleAt.Interval` from `TickMillis`) seeded in `Init`
- [x] `Reducers/ChannelTick.cs` — collect-then-mutate over `ActiveHarvest`: set `Progress = clamp((now−StartedAt)/dur)` via `Timestamp.TimeDurationSince(...).Microseconds`; if `now.CompareTo(CompletesAt) ≥ 0` → `CompleteHarvest` (delete row first, then re-validate alive + node yield + reach off row `Identity`; reach check BEFORE decrement) → `FindFreeHotbarSlot` insert (**full → World item at node**), decrement yield
- [x] `Reducers/StartHarvest.cs` (auth + alive + node exists + "Node is depleted." + reach + free-hotbar-slot precheck; upsert sender's `ActiveHarvest`) + `Reducers/CancelHarvest.cs` (delete sender's; no-op if none)
- [x] `DamageRules.ApplyDamage` death hook clears victim's harvest (before the hotbar scatter); `SetNodeYield.cs` debug setter (for depleted-node validation). Publish `--delete-data=always` + generate + builds; HarvestConfig/ChannelTickTimer seeded (verified via `spacetime sql`)
- Confirmed `SpacetimeDB.Timestamp` API by DLL reflection: `MicrosecondsSinceUnixEpoch`, `TimeDurationSince`, `op_Addition(Timestamp,TimeDuration)`, `CompareTo`, `op_LessThan/GreaterThan`; `Timestamp + TimeSpan` works via implicit `TimeSpan→TimeDuration`.

### Subtask 4.2.2: stdb validation — Scope: M ✅
- [x] `ConnectedGameHarnessController`: `harvest` observed section (active_exists/node_id/progress) + `test_fast_harvest`, `test_spawn_node_at_player`/`test_spawn_node_far`, `test_start_harvest_nearest`, `test_cancel_harvest`, `test_deplete_nearest_node`, `test_teleport_player_far`; `FindNearestNodeId`/`PlayerEntityPos` helpers. (Nodes spawned AT the player's server position to guarantee reach without flaky wall-clamp navigation.)
- [x] `harvest_round_trip.json` (start → progress > 0.5 → **restart upsert resets progress** → completes once → item + yield 5→4) + `harvest_cancel_and_rejections.json` (cancel mid-channel / out-of-reach / depleted-at-start) + `harvest_full_hotbar_drops_world.json` (fill hotbar mid-channel → world drop, no hotbar ore, yield down)

### Subtask 4.2.3: Client — RadialProgress port — Scope: M ✅
- [x] `client/game/Ui/RadialProgress/RadialProgress.gdshader` (verbatim copy from trail) + `RadialProgress.cs` (port of `radial_progress.gd`; tween methods via `Callable.From<float>`; `Reset(value)` snap-without-tween for channel start) + `RadialProgress.tscn` (ShaderMaterial `resource_local_to_scene = true`, declared in scene not built in `_Ready`)
- [x] `[Export] ProgressMode Mode` — Chunked (tweened ease toward each target) / Continuous (direct set, tweens bypassed); `radial_progress_modes.json` (continuous snaps to set value exactly; chunked still mid-ease at the same early frame, settles later) + ring screenshots reviewed (two 40% arcs)

### Subtask 4.2.4: Client — harvest flow — Scope: M ✅
- [x] `HarvestService` active-harvest half (subscribe `ActiveHarvests` filtered to local identity; `Progress` read off the row), `RequestStartHarvest`/`RequestCancelHarvest` (reducer when connected, local mirror + `AdvanceTestHarvest`/`Harvested` event in test mode)
- [x] `ResourceNode` `%HarvestRing` (RadialProgress instance, Chunked, hidden) + `SetHarvestProgress(active, progress)`; `ResourceNodeSpawner.UpdateRings` drives it from the local active channel; `Player.Harvest` settable prop (not an AutoInject dep) → movement while harvesting fires `RequestCancelHarvest()` (no movement lock); `Main` wires `ResourceNodeSpawner.Interacted` → start + hands the player the service

### Subtask 4.2.5: Pure validation — Scope: M ✅
- [x] `HarvestHarnessController`: pure mirror (`_PhysicsProcess` advances `AdvanceTestHarvest`; `Harvested` → `InventoryService.AddTestHotbarItem` via registry YieldItemId), `test_seed_node_at_origin` (player overlaps node → no walk-up), `harvest`/`hotbar`/`ring_visible` observed
- [x] `harvest_channel_yields_item.json` (interact → ring visible + progress climbs → RawOre in hotbar, yield 5→4, ring gone) + `harvest_move_cancels.json` (interact → move → ring gone, hotbar empty, yield untouched); ring-over-node screenshot reviewed

### Subtask 4.2.6: DoD sweep + adversarial review — Scope: S ✅
- [x] `./scripts/validate_all.ps1` — both suites green (pure + 38 stdb), no regressions; game boots clean 13s (zero ERROR, DbManager connected + subscription + 4 node types); `dotnet build` + csharpier (client), `spacetime build` + format (server); uids imported
- [x] **Adversarial review** (4-lens workflow → skeptic verification): 12 raised, 7 confirmed, 5 rejected as nits. Applied: move-cancel reducer-spam latch (optimistic local end on cancel, cf. TrackCurrentRoom) + gate `Changed` to self-rows only (skip remote harvesters' per-tick churn); added coverage for the **completion-time reach re-check** (`harvest_completion_revalidates.json` — teleport server entity out of reach via MoveEntity without CancelHarvest, also depleted-mid-channel), **death-during-channel clear** (`harvest_death_clears_channel.json`), and channel **restart/upsert** (folded into round_trip). Rejected nits: HarvestService rebind symmetry (latent only), `fill_easing` export drop (default-identical), Continuous no-op signal emit (no subscribers), `[Tool]` omission (scene-first material handles editor preview), unused `test_fast_harvest`.
- [ ] **Deferred (4.3):** two simultaneous channels through the one shared `ChannelTick` are not yet exercised by a scenario (the collect-then-mutate multi-row path) — needs puppet-driven harvest infra (PuppetClient only moves). Land it when 4.3 extends ChannelTick to CraftingJob, where multi-row coverage matters more.

## Task 4.3: Bench crafting + Fabricator modal (Refine verb) — Scope: L ✅ DONE

### Subtask 4.3.1: Server — bench storage + recipes + queue — Scope: M ✅
- [x] `Types/RecipeId.cs` (None, FuelCell, Meal — Meal recipe rules in 4.4) + `Crafting/CraftingRules.cs` (recipe catalog `RecipeFor`/`AllRecipes`: FuelCell = Workshop [FuelDeposit, RawOre]; `IsBench`/`BenchAcceptsType`; `GetCraftingConfig`; `FindFreeBenchInput/OutputSlot` + `OccupiedBenchSlots`)
- [x] `Tables/CraftingConfig.cs` (`CraftMillis` 5000, `BenchInputSlots` 4, `BenchOutputSlots` 4) + `Reducers/SetCraftingConfig.cs` (SetHarvestConfig "0 = keep" convention)
- [x] `Tables/CraftingJob.cs` — **`Timestamp?` codegen spike SUCCEEDED** (generated clean `SpacetimeDB.Timestamp? StartedAt/CompletesAt`; no fallback needed). null `CompletesAt` = queued
- [x] `LoadItem.cs`: bench branch (restricted store — `BenchAcceptsType` + free input-zone slot) before the CargoBay storage / tank branches; `WithdrawItem` unchanged (withdraws any Stored item by id, reach vs SlotCenter)
- [x] `Reducers/QueueCraft.cs` (auth + alive + recipe + room-type-matches-bench + IsPowered + reach LoadRadius + `FindIngredientRow` hotbar-then-bench-input distinct rows, validate-all-then-delete; idle → activate timestamps, else enqueue null)
- [x] Extend `ChannelTick`: active `CraftingJob` (CompletesAt != null) advance `Progress`; `CompleteCraft` re-validates bench (gone/recipe-unknown/reassigned → delete + activate next; unpowered → hold 1.0; else output to free output slot / fallback World item) + `ActivateNextQueued` by `(QueuedAt, JobId)`
- [x] `Init.cs` seeds `CraftingConfig`; publish `--delete-data=always` + generate + builds

### Subtask 4.3.2: stdb validation — Scope: M ✅
- [x] `ConnectedGameHarnessController`: `crafting` section (per-bench `job_count`/`active`/`active_recipe`/`active_progress`/`queued_count`/`input_count`/`output_count`) + `test_teleport_to_workshop` (MoveEntity to SlotCenter(4) = (-288,144), dodges nav flake), `test_fast_craft`/`test_medium_craft`, `test_load_fueldeposit_to_workshop`, `test_load_scrap_to_workshop`, `test_queue_fuelcell`(+`_at_kitchen`), `test_queue_two_jobs`, `test_toggle_breaker_4`, `test_withdraw_first_bench_output`
- [x] `craft_queue_round_trip.json` (pre-loaded bench input + hotbar → queue consumes both → progress → output zone → withdraw) + `craft_rejections.json` (out-of-reach / wrong-bench / unpowered / non-ingredient deposit / missing ingredient) + `craft_queue_ordering_and_power.json` (two jobs FIFO; cut Workshop power mid-craft → Progress holds 1.0, no output → restore → completes). All green

### Subtask 4.3.3: Client — Recipe + CraftingService + FabricatorModal — Scope: M ✅
- [x] `client/game/Crafting/Recipe.cs` (`[GlobalClass]`: RecipeId/Label/BenchRoomId/IngredientItemIds/OutputItemId) + `Recipes/FuelCellRecipe.tres` + `RecipeRegistry` (`[Export]` array + `ForBench`, wired in Main.tscn + CraftHarness.tscn)
- [x] `_Service/CraftingService.cs` (per-bench `ActiveJobAt`/`QueuedCountAt`, `Progress` off row, `BenchInput/OutputSlots`, `RequestQueueCraft` → QueueCraft reducer / `TestQueueRequested`; BindConnection on `CraftingJobs`; test mirror `SeedTestJob`/`AdvanceTestActiveJobs`/`JobCompleted`)
- [x] `Ui/Modals/FabricatorModal.tscn`/`.cs` (recipe list w/ `RecipeRow` Queue buttons disabled when unavailable; hotbar→deposit grid + input/output `ItemSlotGrid`; Chunked `RadialProgress` on active job) + `RecipeRow.tscn`/`.cs`; `ModalHost.tscn` repoints Fabricator export; `RoomModalInfo`/`Terminal` gain `RoomId` so the modal filters recipes by bench; `Main.cs` provides `CraftingService` + `RecipeRegistry`
- [x] `InventoryService` test helpers `SeedTestStoredItemAt`/`RemoveTestStoredItem` for bench-zone mirrors

### Subtask 4.3.4: Pure validation — Scope: M ✅
- [x] `CraftHarness.tscn` + `CraftHarnessController` (provides Interaction/Inventory/Crafting/RecipeRegistry/ItemTypeRegistry; opens modal directly via `test_open_fabricator` to dodge nav flake; test mirror consumes ingredients hotbar-then-input + deposits output)
- [x] `fabricator_modal_lists_recipes.json` (Fuel Cell row + ingredient summary; Queue disabled→enabled with ingredients; grids render) + `fabricator_queue_progress_mirror.json` (pre-loaded input + hotbar → queue consumes both → ring animates + progress climbs → FuelCell in output grid). Screenshots reviewed — modal layout, white progress arc, output tile land correctly

### Subtask 4.3.5: DoD sweep — Scope: S ✅
- [x] `./scripts/validate_all.ps1` both suites green (44 pure + 41 stdb), zero flaky (the 3 historically-flaky PowerHarness modal scenarios passed); game boots clean 13s (zero ERROR, DbManager connected + subscription, RecipeRegistry loaded); `dotnet build` + csharpier (client), `spacetime build` + format (server); uids imported; plan/todo ticked; push

## Task 4.4: Meals feature whole + economy checkpoint — Scope: M ✅ DONE

### Subtask 4.4.1: Server — Meal + EatItem — Scope: S ✅
- [x] `ItemTypeId` += `Meal` (appended last); `CraftingRules.AllRecipes` += `Meal` + `RecipeFor` Meal (Kitchen, [Biomass]→Meal) — `BenchAcceptsType`/`IsBench` derive it, so Kitchen now accepts Biomass into its input zone
- [x] `VitalsConfig` += `float MealHungerRestore` (seeded 50); `VitalsRules.RestoreHungerFor(ctx, identity, amount)` helper (clamps + writes, throws on missing row); `RestoreHunger.cs` delegates to it (signature + semantics unchanged)
- [x] `Reducers/EatItem.cs` (known-player auth + alive guard "Dead players cannot eat." + hotbar-slot item is Meal → delete + `RestoreHungerFor` with `GetVitalsConfig().MealHungerRestore`, never client-supplied); publish `--delete-data=always` + generate bindings + client build. CLI confirmed `meal_hunger_restore = 50` + `eat_item` registered

### Subtask 4.4.2: stdb validation — Scope: S ✅
- [x] `ConnectedGameHarnessController`: `test_give_meal_slot0`/`test_eat_slot0`/`test_teleport_to_kitchen`/`test_load_biomass_to_kitchen`/`test_queue_meal`/`test_withdraw_first_kitchen_output`; `FindFirstBenchOutputItemId` generalized to a roomSlot param (`test_set_hunger_low` already existed)
- [x] `meal_craft_and_eat_round_trip.json` (biomass → Kitchen input → queue Meal → output slot → set hunger low → withdraw → eat → hunger restored by config amount via delta assert, meal gone) + `meal_eat_rejections.json` (non-meal RawOre: no consume, no restore; dead: no restore — alive guard short-circuits)

### Subtask 4.4.3: Client — Kitchen bench + eat input — Scope: M ✅
- [x] `KitchenRoom.tres` TerminalType Info(4) → Fabricator(2); `MealItem.tres` (orange "M") wired into every `ItemTypeRegistry` scene (Main + Inventory + Craft harnesses); `MealRecipe.tres` (Kitchen, [Biomass]→Meal) wired into every `RecipeRegistry` scene (Main + CraftHarness)
- [x] `HotbarUse.tres` GUIDE action mapped KBM (F=70) + controller (joy button 3); `AppRoot.EnsureInputActions()` += `hotbar_use` → Key.F; `HotbarHud` `UseAction` export + `UseRequested` event; `InventoryService.RequestUse(slot)` (EatItem reducer connected / `TestUseRequested` mirror); `Main` routes `HotbarHud.UseRequested` → `RequestUse(SelectedSlot)`; `VitalsService.RestoreTestHunger` for the pure mirror

### Subtask 4.4.4: Pure validation — Scope: S ✅
- [x] `hotbar_use_eats_meal.json` (InventoryHarness gained VitalsService provider + VitalsHud node + eat wiring + `OnTestUse` edibility mirror): meal + low hunger → F → meal gone + FOOD 10→60; non-edible RawOre → F → no-op (item stays, hunger unchanged). Screenshots reviewed (orange "M" slot, FOOD bar restored)
- [x] `fabricator_kitchen_lists_meal.json` (CraftHarness `test_open_kitchen_fabricator`): Kitchen Fabricator lists only the Meal recipe (per-bench `ForBench` filter). Screenshot shows the single Meal row
- [x] Regression fix: Kitchen→Fabricator broke `terminal_interact_opens_modal` + `pressure_modal_shows_lost` (both walked to the now-Fabricator Kitchen terminal, whose modal needs crafting services those dep-light harnesses lack). Confirmed via clean-HEAD stash test. Fixed faithfully: InteractionHarness swaps Kitchen↔Hydroponics so the straight-down walk-up slot keeps an Info terminal; pressure scenario retargets to Hydroponics (the only room whose RoomInfoModal a player can still open in-game) via a deterministic direct-open. `item_types_load.json` updated 6→7 types (+ Meal)

### Subtask 4.4.5: Economy checkpoint + cleanup + DoD — Scope: M ✅
- [x] `scenarios_stdb/economy_loop_end_to_end.json` (MultiplayerGameHarness): harvest RawOre + FuelDeposit → Fuel Cell at Workshop → withdraw → Load Reactor (fuel up); harvest Biomass → Meal at Kitchen → withdraw → eat (hunger up). Puppet client asserts it observes node depletion (`total_node_yield` 5→4, new on PuppetClient) + the Fuel Cell bench-output item (`stored_item_count`). Screenshot shows FOOD 60/100 after the loop
- [x] **Cleanup:** removed the 3 dev world-item seeds + `SeedWorldItem` helper from `Init.cs`; deleted `world_items_seeded_on_init.json` (the only scenario asserting them — every other item scenario clears items first, so the 0-baseline is unaffected); publish `--delete-data=always`
- [x] Both suites green (47 pure + 47 stdb); game boots clean 13s (zero ERROR, DbManager connected + subscription, 7 item types incl. Meal, 2 recipes); `dotnet build` + csharpier (client), `spacetime build` + format (server); **Checkpoint: Economy** marked; push

---

# Phase 5: Node Activities 🔄 PLANNED

Plan authored 2026-06-13 (`/agent-skills:plan phase 5`). Phase 5 introduces the **Node Activity** layer — the ship anchors at a node, and each node type presents distinct activities/hazards. Navigation (Star Chart, Jump, Stellar Night) lands in Phase 6; until then nodes are **debug-switched** via a `SetActiveNode` reducer (the eventual jump target) wired to a DebugHud node selector (mirrors the existing top-right Reset World button from commit df20e20).

## Architecture (user-confirmed 2026-06-13)

- **Ship-as-component / map host:** the ship interior becomes a reusable `Ship.tscn` component placed into `Map` scenes. `Main` becomes a `MapHost` that loads the map matching the active node's `NodeKind`, places the `Ship` component, and re-seats the player. Default node = `Quiet` → `QuietMap` (the current world, ship in space). Exterior maps (`PlanetsideMap`, `WreckMap`, `TradingStationMap`) place the same Ship component beside their exterior grid, reached via airlock; defense/maintenance are ship-side node states (no separate map). Spawners/interaction/HUDs/camera are already grid-decoupled (confirmed by exploration) — they survive the refactor untouched, so the extraction is **regression-gated** (all existing scenarios stay green).
- **Node state model:** a single-row `NodeActivity` table (`Kind`, `ArrivedAt`) is the source of truth. `SetActiveNode(kind)` clears prior **transient** node state (exterior resource nodes, creatures, threats, trade availability) and seeds the new node's via a `NodeRules.SeedNode` dispatcher each node task extends. **Persistent** ship state (rooms, power, pressure, vitals, stores, hotbar+cargo items) and **ship hazards** (fire, hull breaches — they don't vanish on a jump) survive node changes.
- **Hazard framework (fire first):** positioned `Hazard` entities carry intensity; a `HazardTick` scheduled reducer (VitalsTick/ChannelTick pattern) grows intensity, spreads **deterministically** (lowest-index un-ignited adjacent floor cell — no RNG in reducers), and applies proximity damage via the **already-declared** `DamageType.Fire`. `DamageType.Creature` (also already declared) is the 5.2 hook. Proximity = iterate hazards × connected-alive player entities, `distance² < radius²` (the established reach pattern; no spatial index needed at ≤8 players).
- **Per-weapon ammo (5.5):** weapons are ship fixtures with their own ammo counter, powered via their room's breaker; `Ammo` item + `AmmoCrate` Workshop recipe + a 4th per-weapon `LoadItem` ammo branch (the `ItemRules` "Ammo joins in 5.5" hook already comment-marked). Auto-target via `WeaponTick`; no player aiming (§8).
- **Credits trading (5.4):** `ShipStores.Credits` + a static `TradeRules` price catalog; `BuyItem`/`SellItem` reducers gated on a TradingPost node + terminal reach.
- **Spatial validation:** proximity features (fire, creatures, weapons↔threats) validate with the proven **spawn-at-player + `MoveEntity` teleport** pattern (dodges nav flake). New pure harnesses (Hazard/Creature/Weapon/Breach) follow the `IProvide` + `ActionKeyBridge` + `get_observed_state` + `_PhysicsProcess` edge-detection template. The exploration flagged these as the only net-new validation infra; trading reuses the modal-harness pattern.
- **Publish ritual** every schema change: `spacetime publish nomad --delete-data=always --yes --server local --module-path ./src` + regenerate bindings + re-green stdb suite before commit. New `ItemTypeId`/`RecipeId`/enum values **append at END** to keep client-binding ordinals stable.

## Dependency graph

```
5.1 Node framework (Ship component + MapHost + NodeActivity state) + hazard framework + FIRE
    │
    ├── 5.2 Planetside map (exterior grid + AIRLOCK system + CREATURES + relocate resource nodes)
    │       ├── 5.3 Wreck map (reuses airlock/exterior grid; reuses fire + creatures; salvage loot)
    │       └── 5.4 Trading station map (reuses airlock/exterior grid; Credits + TradeRules + modal)
    │
    ├── 5.5 Defense event (ship-side; per-weapon Ammo + Load branch + threats)   ┐ parallel after 5.1
    └── 5.6 Maintenance + HULL BREACHES (ship-side; depressurize via 1.5 + patch) ┘
```

Order rationale: 5.1 is the foundation (highest refactor risk → first, regression-gated). 5.2 builds the novel exterior-grid+airlock system (fail-fast on the one structurally-new piece); **5.3 and 5.4 both reuse it** (wreck + trading station), so building 5.2 well pays off three times. 5.5/5.6 are ship-side, additive on proven patterns, and may be reordered/parallelized after 5.1.

---

## Task 5.1: Node framework + map host + hazard system (fire) — Scope: L

The foundation. Establishes node state, the ship-as-component / map-host seam, and the hazard framework proven end-to-end with fire (a ship hazard that works in the default Quiet map — no exterior grid needed yet). Riskiest subtask (the refactor) goes first, fail-fast.

### Subtask 5.1.1: Client refactor — Ship component + MapHost (regression-gated) — Scope: M ✅
- [x] Extract `client/game/Ship/ShipBody.tscn` + `ShipBody.cs` wrapping ShipGrid (+ the terminal/breaker/suit-rack spawning it already owns), with exported `PlayerSpawn`/`AirlockMount` `Marker2D`s. (Class is `ShipBody`, not `Ship` — a class literally named `Ship` collides with the `Nomad.Game.Ship` namespace used module-wide as `Ship.Breaker`/`Ship.PowerGridService`/…; the scene root node is still "Ship".)
- [x] Create `client/game/Map/QuietMap.tscn` (`GameMap.cs` base + ShipBody instance + a `CanvasLayer` space backdrop); `client/game/Main.cs` is now a MapHost that `LoadMap`s QuietMap into a scene-declared `MapMount`, wires the ShipGrid via a `ShipGrid` accessor, and keeps seating the player itself. Only ShipGrid moved out of Main — Player/spawners/UI/camera stayed, so the regression surface was tiny. (No stub second map needed: the dynamic `LoadMap(PackedScene)` seam IS the swap mechanism, exercised by the QuietMap load.)
- [x] **Acceptance met: 47 pure + 48 stdb scenarios green, zero regressions**; game boots clean ≥13s zero ERROR (DbManager connected, subscription applied, 8 room types loaded); rooms/terminals/breakers/suit-rack render identically (screenshot reviewed) + new dark space backdrop in the void.
- [x] Only two harnesses embed Main.tscn (`ConnectedGameHarness`, `MultiplayerGameHarness`, both via `ConnectedGameHarnessController`); the other 9 instance `ShipGrid.tscn` directly and were untouched. Sole controller change: the `GetNodeOrNull<ShipGrid>("ShipGrid")` lookup → `_main.ShipGrid` accessor.

### Subtask 5.1.2: Server — NodeActivity + hazard framework + fire reducers — Scope: M ✅
- [x] `Types/NodeKind.cs` (Quiet, Planetside, Wreck, TradingPost, DefenseEvent), `Types/HazardTypeId.cs` (None, Fire) — `[SpacetimeDB.Type] enum : uint`
- [x] `Tables/NodeActivity.cs` (Id 0 singleton, Public), `Tables/Hazard.cs` (HazardId PK AutoInc, HazardTypeId, `DbVector2 Position`, `float Intensity 0→1`, `int RoomSlotIndex`, Public), `Tables/HazardConfig.cs` (Public), `Tables/HazardTickTimer.cs` (scheduled)
- [x] `Node/NodeRules.cs` — `GetNodeActivity`, `SeedNode(kind)` (Quiet → ReseedResourceNodes; other kinds stub for 5.2–5.6) / `ClearTransientNodeState` (deletes resource nodes; leaves ship hazards + persistent ship state)
- [x] `Hazard/HazardRules.cs` — config getter/reschedule, `IgniteHazardAtCell` (dedup per cell), `TickHazards` (grow→spread→proximity damage), `SpreadFire` (lowest-(Y,X) un-ignited adjacent floor cell, deterministic), `ApplyHazardProximityDamage` (any-fire-in-radius → one Fire damage tick), `DeleteAllHazards`
- [x] `Ship/HullGeometry.cs` — server floor-cell geometry mirroring the client's `BuildMap` (rooms + corridor + 14 doors, integer 14/8 offset), `WorldToCell`/`CellToWorld`/`SlotForCell`/`SlotCenterCell`/`Neighbors4`. `SlotCenterCell` routes through `SlotCenter` (review fix: keeps room-targeted ignition aligned with the float room center)
- [x] Reducers: `SetActiveNode(NodeKind)` (clear+seed, hazards persist), `IgniteHazard(typeId, roomSlot)` (room-center) + `IgniteHazardAt(typeId, x, y)` (validation/spawn-at-player), `ExtinguishHazard(hazardId)` (known-player + alive + reach), `HazardTick`, `SetHazardConfig` (SetVitalsConfig pattern)
- [x] `Init.cs` seeds NodeActivity(Quiet) + HazardConfig + HazardTickTimer + `SeedNode(Quiet)`; `ResetWorld.cs` clears hazards + resets to Quiet; published `--delete-data=always` + regenerated bindings + builds clean
- [x] Adversarial multi-lens review (determinism/authority/geometry/tick-logic/regression) before publish: non-blocking, two "critical" findings confirmed false positives; the one real fix (SlotCenterCell alignment) applied
- [x] Acceptance proven end-to-end by the stdb scenarios (stronger than CLI; debug reducers require a known player, which only the connected Godot client is)

### Subtask 5.1.3: stdb validation — fire damage + node switch — Scope: S ✅
- [x] `ConnectedGameHarnessController`: `hazards` observed (count + list id/type/intensity/pos/room_slot), `node.kind` observed; `FindNearestHazardId`; test actions `test_fast_hazards`(MaxHazards 1, no spread)/`test_fast_hazards_spread`(MaxHazards 6)/`test_ignite_at_player`/`test_ignite_reactor`/`test_extinguish_nearest`/`test_set_node_{planetside,quiet}`
- [x] `scenarios_stdb/fire_proximity_damages_player.json` — ignite on player's cell → health falls via Fire (assert_pipeline delta < 0) → extinguish → count 0 + health holds (abs delta ≤ 0.5)
- [x] `scenarios_stdb/fire_spreads_deterministically.json` (**added** — the plan under-specified spread, the review flagged it): teleport clear, ignite at Reactor, intensity crosses threshold → count grows 1→>1 bounded by MaxHazards, all Fire on real slots
- [x] `scenarios_stdb/node_switch_clears_transient.json` — Quiet (4 nodes + fire) → Planetside clears resource nodes + fire persists → Quiet reseeds 4 nodes + fire still persists
- [x] All 3 green; full suite **47 pure + 51 stdb**, no regressions; screenshots reviewed (ship intact, HP dropping from server-side fire, O2 held in corridor — fire *visual* lands in 5.1.4)

### Subtask 5.1.4: Client — fire rendering + extinguish — Scope: M
- [ ] `client/game/Hazard/HazardType.cs` (`[GlobalClass]`: Color/Glyph/Label/HazardId) + `.tres` + `HazardTypeRegistry` (`[Export]` array, wired Main + harness scenes)
- [ ] `Fire.tscn` (flicker visual + Area2D + InteractTarget "Extinguish", GhostAccessible false) + `FireSpawner` (ResourceNodeSpawner pattern, update-in-place on intensity, free on delete)
- [ ] `_Service/HazardService.cs` (subscribe `Hazards`, `Changed`, entries + test seeders); Main provides HazardService + binds connection; extinguish interact → `ExtinguishHazard`

### Subtask 5.1.5: Pure validation — fire — Scope: S
- [ ] `FireHarness.tscn` + controller (provides Hazard + Interaction services; seeders)
- [ ] `fire_renders_and_spreads.json` (ignite → fire node + intensity ramp → spread spawns adjacent → extinguish frees), `fire_extinguish_interact.json` (walk-up → "Extinguish" prompt → interact → fire gone)

### Subtask 5.1.6: DoD sweep — Scope: S
- [ ] `./scripts/validate_all.ps1` both suites green, no regressions; screenshots reviewed (fire over a room, post-extinguish clear)
- [ ] Game boots clean ≥13s zero ERROR; builds + csharpier both sides; plan/todo ticked; `git push origin`

---

## Task 5.2: Planetside map + airlock system + creatures — Scope: L

Builds the reusable exterior-grid + airlock-transition system on the 5.1 MapHost seam. Suit-required-on-surface falls out of existing oxygen rules (exterior = no room → `CurrentSlotIndex = -1` → oxygen depletes). Creatures move **server-authoritatively** (deterministic chase-nearest/patrol, no RNG), interpolated client-side like remote players; contact damage via the already-declared `DamageType.Creature`. (5.2.1 is large — split into an airlock/zone slice and a creature slice if it exceeds one focused session.)

### Subtask 5.2.1: Server — zones + airlock + planetside seeding — Scope: M
- [ ] `Player.InExterior bool` (or `Zone` enum); `EnterExterior`/`EnterInterior` reducers (known-player + alive + reach vs airlock position; flips zone + teleports the player's Entity to the exterior/interior landing point)
- [ ] `NodeRules.SeedNode(Planetside)` — seed exterior resource nodes (relocate `ReseedResourceNodes` to exterior coords; `ResourceNode` is position-agnostic per the 4.1 decision — no schema change) ; clear on departure
- [ ] publish/generate/builds; Acceptance (CLI): SetActiveNode(Planetside) seeds exterior nodes; EnterExterior flips zone + moves the entity

### Subtask 5.2.2: Server — creatures + CreatureTick — Scope: M
- [ ] `Types/CreatureTypeId.cs`, `Tables/Creature.cs` (CreatureId PK AutoInc, CreatureTypeId, Position/Velocity, `float Health`, `Identity Target`), `Tables/CreatureConfig.cs`, `Tables/CreatureTickTimer.cs` (scheduled)
- [ ] `Creature/CreatureRules.cs` — deterministic movement (chase nearest exterior player in range, else patrol fixed waypoints), contact damage (`distance² < contactRadius²` → `ApplyDamage` Creature); `SpawnCreature`/`ClearCreatures` debug reducers; `SeedNode(Planetside)` also seeds creatures
- [ ] publish/generate/builds; Acceptance (CLI): creature tick moves toward an exterior player; contact damages

### Subtask 5.2.3: stdb validation — surface loop + creature + oxygen — Scope: M
- [ ] Harness: `node`/`zone`/`creatures` observed + `test_enter_exterior`/`test_spawn_creature_at_player`/`test_move_creature` actions
- [ ] `planetside_seeds_surface_nodes.json`, `creature_contact_damages_player.json` (spawn at player → health falls Creature → teleport away → holds), `surface_requires_suit.json` (exterior → oxygen depletes without suit → suit holds), `planetside_harvest_on_surface.json`

### Subtask 5.2.4: Client — PlanetsideMap + exterior grid + airlock — Scope: M
- [ ] Generalize grid rendering (extract a shared renderer from ShipGrid or a simpler `TerrainGrid`); `PlanetsideMap.tscn` (exterior grid + placed Ship + landing pad)
- [ ] `Airlock.tscn` (InteractTarget "Exit to surface"/"Enter ship") → Enter/Exit reducer + MapHost reposition + RoomLocator swap (interior vs none); camera follows across

### Subtask 5.2.5: Client — creatures + surface nodes — Scope: M
- [ ] `Creature.tscn` + `CreatureSpawner` (server-driven, interpolated via the RemoteEntity/EntityMover pattern) + `CreatureTypeRegistry`; resource nodes render on the exterior grid (spawner already position-driven)

### Subtask 5.2.6: Pure validation + DoD sweep — Scope: M
- [ ] Exterior/airlock harness (new) + `CreatureHarness`; `airlock_transition.json`, `creature_chases_and_contacts.json`, `surface_nodes_render.json`
- [ ] `validate_all.ps1` both suites green; boot clean; builds+format; screenshots (surface, suited player, creature near player); plan/todo; push

---

## Task 5.3: Abandoned wreck salvage (reuse exterior system) — Scope: M–L

Reuses 5.2's airlock/exterior-grid; new hand-crafted derelict layout; salvage = scattered World items + WreckageDebris nodes (exist) hauled back to cargo (exists); reuses 5.1 fire + 5.2 creatures as wreck hazards. Rare `Components` drops (item exists).

### Subtask 5.3.1: Server — wreck node seeding + salvage loot — Scope: M
- [ ] `NodeRules.SeedNode(Wreck)` — seed wreck loot (World items: Components/Scrap) + WreckageDebris nodes + optional fire/creature hazards; `SpawnSalvageLoot` debug; publish/generate/builds

### Subtask 5.3.2: stdb validation — salvage + haul-back — Scope: S
- [ ] `wreck_seeds_loot_and_hazards.json`, `wreck_salvage_haul_to_cargo.json` (pick up loot in wreck → cross back to ship → store in CargoBay; puppet sees the store)

### Subtask 5.3.3: Client — WreckMap — Scope: M
- [ ] `WreckMap.tscn` (derelict HullTemplate-like layout, dim/vacuum aesthetic) placing the Ship + dock airlock; reuse item/node/fire/creature spawners

### Subtask 5.3.4: Pure validation + DoD sweep — Scope: S
- [ ] `wreck_renders.json` + fire/creature reuse in wreck context; both suites green; boot clean; builds+format; screenshots; plan/todo; push

---

## Task 5.4: Trading station (credits, docked-station map) — Scope: L · depends on 5.2

Credits currency + docked-station presentation (user-confirmed 2026-06-13). The ship links with an automated depot / merchant station; players cross the 5.2 airlock onto a `TradingStationMap` to trade at the station's terminal. `ShipStores.Credits` + a static `TradeRules` buy/sell catalog (mirrors `CraftingRules`); `BuyItem`/`SellItem` gated on node==TradingPost + reach vs the station terminal. Trading mechanics are identical to a ship-side console — the delta over an M-scoped version is the station map + airlock reuse, which is exactly why this depends on 5.2's airlock/exterior-grid system.

### Subtask 5.4.1: Server — Credits + TradeRules + Buy/Sell + station seeding — Scope: M
- [ ] `ShipStores.Credits` (seed); `Trade/TradeRules.cs` (buy price + sell value per `ItemTypeId` catalog)
- [ ] `BuyItem(itemTypeId)` (known-player + alive + credits ≥ price + free hotbar slot + node==TradingPost + reach vs station terminal), `SellItem(hotbarSlot)` (item → credits, node-gated + reach); `SetCredits` debug
- [ ] `NodeRules.SeedNode(TradingPost)` enables the station/terminal (the airlock destination); publish/generate/builds

### Subtask 5.4.2: stdb validation — buy/sell + rejections — Scope: M
- [ ] `trade_buy_sell_round_trip.json` (cross to station → sell ore → credits up + slot empty; buy fuel → credits down + item in hotbar), `trade_rejections.json` (insufficient credits, full hotbar, not at trading node, out of reach)

### Subtask 5.4.3: Client — TradingStationMap + airlock + trade terminal — Scope: M
- [ ] `TradingStationMap.tscn` (reuses the 5.2 exterior-grid renderer; station-concourse layout placing the Ship + dock airlock + merchant terminal); MapHost loads it at a TradingPost node; airlock crosses ship ↔ station (reuses the 5.2 Enter/Exit transition)
- [ ] `TerminalType.Trading`; the station terminal opens the trade modal

### Subtask 5.4.4: Client — TradingModal + credits readout — Scope: M
- [ ] `_Service/TradeService.cs`; `TradingModal.tscn` (buy-offers list + sell-from-hotbar grid + credits readout; Storage/Fabricator pattern, in-place updates); `ModalHost.SceneFor` route; credits also on DebugHud

### Subtask 5.4.5: Pure validation + DoD sweep — Scope: M
- [ ] `TradingHarness` (extends Inventory + the exterior/airlock harness); `trading_modal_buy_sell.json` (credits + grids update in place; focus-nav), `trading_station_airlock.json` (cross to station → terminal reachable)
- [ ] both suites green; boot clean; builds+format; screenshots (station concourse, modal, credits); plan/todo; push

---

## Task 5.5: Automated defense node (per-weapon ammo + threats) — Scope: L

Per-weapon ammo (user-confirmed). Weapons are ship fixtures (seeded in designated rooms), powered via their room's breaker; auto-target nearest threat via `WeaponTick`; no player aiming (§8). Ships the ammo feature **whole**: `Ammo` item, `AmmoCrate` Workshop recipe, 4th per-weapon `LoadItem` ammo branch. Threats spawn at the DefenseEvent node, track the ship, and damage it on contact (a hull breach via 5.6 if landed, else room damage).

### Subtask 5.5.1: Server — Ammo item + recipe + Weapon table + Load branch — Scope: M
- [ ] `ItemTypeId.Ammo` (append), `RecipeId.AmmoCrate` (append) + `CraftingRules` recipe (Workshop: RawOre + Scrap → Ammo) + `AllRecipes`/`RecipeFor`
- [ ] `Tables/Weapon.cs` (WeaponId PK AutoInc, `int RoomSlotIndex`, Position, `int Ammo`, `int AmmoMax`, `bool Active`, `Timestamp LastFiredAt`); weapon seeding on the hull
- [ ] `ItemRules.AcceptsTankDeposit` ammo case + `LoadItem` per-weapon ammo branch (the "Ammo joins in 5.5" hook → increment the weapon in that room slot); publish `--delete-data=always` + generate + builds

### Subtask 5.5.2: Server — threats + WeaponTick — Scope: M
- [ ] `Tables/Threat.cs` (ThreatId PK AutoInc, Position/Velocity, `float Health`), `Tables/ThreatTickTimer.cs`, `Tables/DefenseConfig.cs`; `ThreatTick` (deterministic move toward ship; contact → ship/room damage), `WeaponTick` (each Active+powered+Ammo>0 weapon → nearest in-range threat → fire: −1 ammo, damage/destroy threat, set LastFiredAt); `NodeRules.SeedNode(DefenseEvent)` spawns threats; publish/generate/builds

### Subtask 5.5.3: stdb validation — ammo + auto-fire — Scope: M
- [ ] `ammo_craft_and_load_weapon.json` (craft AmmoCrate at Workshop → load into weapon → weapon Ammo up), `weapon_autofires_at_threat.json` (spawn threat in range → weapon fires → threat destroyed + ammo down), `weapon_offline_unpowered_or_dry.json` (cut room power / zero ammo → no fire → threat reaches ship → damage)

### Subtask 5.5.4: Client — Weapon + Threat visuals + load flow — Scope: M
- [ ] `AmmoCrateItem.tres` + `AmmoCrateRecipe.tres` wired into every ItemTypeRegistry/RecipeRegistry scene; `Weapon.tscn` (turret + muzzle VFX firing at target) + `WeaponSpawner` + ammo readout; `Threat.tscn` + `ThreatSpawner` (server-driven, interpolated); load-ammo via the existing Load verb (interact at weapon)

### Subtask 5.5.5: Pure validation — weapon + load — Scope: M
- [ ] `WeaponHarness`; `weapon_loads_ammo.json`, `weapon_targets_threat.json` (auto-fire state on a seeded threat)

### Subtask 5.5.6: DoD sweep — Scope: S
- [ ] both suites green; boot clean; builds+format; screenshots (turret firing, ammo readout); plan/todo; push

---

## Task 5.6: Core maintenance node + hull breaches — Scope: M

The Quiet node is the maintenance home (refine/cook/restore power already work on the ship). 5.6 owns the **hull breach** feature: a breach depressurizes its room via the 1.5 `IsPressurized` state and **persists** (ship hazard, survives node changes) until patched. `CreateBreach` is debug/event-driven now; Stellar Night overstay (6.4) + hazards become real causes later. Patch consumes a Scrap and restores pressure (if no other breach remains in the room). The vacuum tint (1.5) already renders depressurization — breach adds a rupture marker + patch interaction.

### Subtask 5.6.1: Server — Breach table + Create/Patch — Scope: M
- [ ] `Tables/Breach.cs` (BreachId PK AutoInc, `int RoomSlotIndex`, Position, `float Integrity`); `CreateBreach(roomSlot)` (depressurize room via the SetPressurization path + record breach), `PatchBreach(breachId)` (known-player + alive + reach + consume one `Scrap` from hotbar → remove breach → repressurize if last breach in room); breaches persist across `SetActiveNode`; publish/generate/builds

### Subtask 5.6.2: stdb validation — breach depressurize + patch — Scope: S
- [ ] `breach_depressurizes_room.json` (create → room vacuum + occupant oxygen drains → patch with Scrap → repressurizes → oxygen refills), `breach_persists_across_node.json`

### Subtask 5.6.3: Client — breach visual + patch interaction — Scope: M
- [ ] `Breach.tscn` (hull rupture overlay + InteractTarget "Patch breach") + `BreachSpawner`; patch consumes Scrap via `PatchBreach`; composes with the existing vacuum tint

### Subtask 5.6.4: Pure validation — breach render + patch — Scope: S
- [ ] `BreachHarness` (extend PowerHarness pressurization infra); `breach_renders_and_patches.json`

### Subtask 5.6.5: DoD sweep + Phase 5 checkpoint — Scope: S
- [ ] DebugHud node-selector switches all 5 node types; walk each: planetside (harvest / avoid creatures / suit required), wreck (salvage + haul back), trading (buy/sell for credits), defense (load ammo + weapons auto-fire + juggle power), maintenance (patch a hull breach); fire works across nodes
- [ ] `./scripts/validate_all.ps1` both suites green, no regressions; game boots clean ≥13s zero ERROR; builds + csharpier both sides; **Checkpoint: Nodes** marked; `git push origin`

**Checkpoint: Nodes** — 5 node types playable (Planetside, Wreck, Trading, Defense, Maintenance) + fire hazard, all debug-switchable via the DebugHud node selector. Phase 5 complete.
