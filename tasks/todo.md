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

# Task 2.2: Oxygen tether + spacesuits 🔄 PLANNED

## Subtask 2.2.1: Server room tracking — Scope: S ✅
- [x] `Player` table += `int CurrentSlotIndex` (seed −1); `SetPlayerRoom.cs` reducer (validate −1 or existing slot)
- [x] Publish + generate + builds; acceptance proven end-to-end by `player_room_tracking.json`

## Subtask 2.2.2: Client RoomLocator + stdb proof — Scope: M ✅
- [x] Create `client/game/Ship/_Service/RoomLocator.cs` — position → slot (RoomSlot rects, corridor rects + door cells → corridor slot, else −1); TileSize const must match ShipGrid
- [x] `Player.cs`: `Hull` property set by Main, `TrackCurrentRoom()` in `_PhysicsProcess` calls `SetPlayerRoom` on change only
- [x] Observed `vitals.current_slot` (server Players row); scenario `scenarios_stdb/player_room_tracking.json` red first, green after — corridor 7 → Kitchen 5 → corridor 7. Gotcha confirmed: wait for `game.terminal_count` before pressing movement (GUIDE context must be active when the bridged key-press edge fires — held state never re-registers); diagonal wedge into the door per 1.4.4
- [x] Full stdb suite green (11/11)

## Subtask 2.2.3: Server oxygen model + VitalsTick — Scope: M
- [ ] `Vitals` += `Meter Oxygen` (100/100), `bool SuitEquipped`
- [ ] Create `VitalsConfig.cs` (single-row tunables) + `VitalsTickTimer.cs` (repeating scheduled) + `VitalsTick.cs` reducer
- [ ] Create `SetSuitEquipped.cs`, `SetVitalsConfig.cs`, `SetOxygen.cs`; `Init.cs` seeds config + tick row
- [ ] Publish + generate + builds; CLI acceptance (drain/refill/suffocation/suit capacity)

## Subtask 2.2.4: stdb validation — oxygen loop — Scope: M
- [ ] Harness: vitals += oxygen fields; test actions `test_fast_vitals`/`test_set_oxygen_low`/`test_equip_suit`/`test_unequip_suit`
- [ ] Scenarios `oxygen_depletes_and_refills.json` + `oxygen_empty_suffocates.json` (red first); suite green

## Subtask 2.2.5: Client — oxygen HUD + suit rack — Scope: M
- [ ] VitalsHud oxygen bar; VitalsService += oxygen/suit
- [ ] Create `client/game/Ship/SuitRack/SuitRack.tscn` + `.cs` (Breaker pattern); ShipGrid spawns in CargoBay slot; `SuitRackInteracted` event
- [ ] `Main.cs` → `SetSuitEquipped`; `Player.cs` suit speed modifier + tint
- [ ] Pure scenarios `vitals_oxygen_bar_renders.json` + `suit_rack_equip_toggles.json`; suite green

## Subtask 2.2.6: stdb suit round trip + DoD sweep — Scope: S
- [ ] Scenario `scenarios_stdb/suit_equip_round_trip.json`; full DoD sweep + push

# Task 2.3: Food/hunger meter 🔄 PLANNED

## Subtask 2.3.1: Server — hunger on the tick — Scope: S
- [ ] `Vitals` += `Meter Hunger` (100/100); VitalsTick depletes everywhere, 0 → Starvation damage; real default rates (~5min)
- [ ] Create `RestoreHunger.cs` (Phase 3 meal entry point + validation path); publish + generate + builds; CLI acceptance

## Subtask 2.3.2: Validation + HUD + mini-sweep — Scope: S
- [ ] VitalsHud hunger bar; pure scenario `vitals_hunger_bar_renders.json`
- [ ] Harness hunger observed + test actions; `scenarios_stdb/hunger_starvation_round_trip.json` (red first)
- [ ] DoD sweep + push

# Task 2.4: Death, ghost state, cloning bay 🔄 PLANNED

## Subtask 2.4.1: Server — ShipStores + respawn — Scope: M
- [ ] Create `server/src/Tables/ShipStores.cs` — Id PK 0, `int Biomass` (seed 3 in Init)
- [ ] Create `server/src/Reducers/RequestRespawn.cs` — `(Identity target)` form; validate dead + CloningBay powered + biomass; deduct, reset vitals, entity → bay center
- [ ] Create `server/src/Ship/HullGeometry.cs` — Corvette slot centers (why-comment → CorvetteHull.tres)
- [ ] Create `SetBiomass.cs` debug setter; publish + generate + builds; CLI acceptance (all rejection paths + happy path)

## Subtask 2.4.2: stdb validation — respawn rules — Scope: S
- [ ] Harness: `stores.biomass` + test actions (`test_request_respawn`, `test_set_biomass_zero`, CloningBay breaker toggle)
- [ ] Scenarios `cloning_respawn_round_trip.json` + `cloning_respawn_requires_power_and_biomass.json` (red first); suite green

## Subtask 2.4.3: Client ghost mode — Scope: M
- [ ] `Player.cs`: IsDead → collision cleared + ghost tint + interaction filtered (`InteractTarget.GhostAccessible`, true only on Cloning terminal); revive → restore + snap to server position
- [ ] `RemoteEntity` ghost tint (Vitals → Players.PlayerEntityId → entity); VitalsHud dead presentation
- [ ] `GhostHarness` + scenarios `ghost_passes_through_walls.json` + `ghost_cannot_interact.json`; pure suite green

## Subtask 2.4.4: Client — CloningModal — Scope: M
- [ ] Create `client/game/Ui/Modals/CloningModal.tscn` + `.cs` (PowerRouterModal pattern): biomass line + dead-crew rows with Clone buttons
- [ ] `ModalHost.tscn`: Cloning slot → CloningModal
- [ ] Pure `cloning_modal_lists_dead.json` + stdb `cloning_modal_respawn.json` (die → float to bay → interact → respawn); green

## Subtask 2.4.5: DoD sweep + Phase 2 checkpoint — Scope: S
- [ ] `./scripts/validate_all.ps1` both suites green; boot clean; builds + format; plan/todo checked incl. **Checkpoint: Characters**; push
