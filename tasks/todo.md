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

# Task 1.3: Interaction Framework (walk-up prompts + terminal modals) 🔄 PLANNED

Reference implementation: untrailed at `C:\Code\Github\upta\untrailed` — `client/src/Core/Interaction/` (probe/target/registration/service), `client/src/App/Interaction/_Service/GameInteractionService.cs` (closest-target resolve), `client/src/Camp/_UI/CookingUI.cs` (modal open/close + exclusive context push pattern). Nomad's `GuideService` already has the exclusive `PushContext`/`PopContext` stack — no changes needed there.

Design notes:
- New code lives in `client/game/Interaction/` (`_Component/`, `_Model/`, `_Service/`) + `client/game/Ship/Terminal/` + `client/game/Ui/`.
- `InteractionService` is plain C# (service-layer rules); scenes depend on it via AutoInject `IProvider`/`[Dependency]` — providers: `Main` (game) and the validation harness controller (tests). Avoids the NodePath-export-across-scene-instances gotcha.
- Beyond untrailed: the service resolves the **focused** (closest registered) target every `Process()` and raises `FocusChanged` — drives the prompt, which untrailed didn't have.
- Pick a free collision layer for interactables after checking Player/wall layer usage in `Player.tscn` / `ShipGrid.cs`.
- Modal content for all 5 terminal types starts as `RoomInfoModal` (label, room type, power status) per resolved design decision "others show room status info initially"; specialized modals ship with their owning features.

## Subtask 1.3.1: Interaction core (port from untrailed) — Scope: M
- [ ] Create `client/game/Interaction/_Model/ProbeData.cs` — entity id + position
- [ ] Create `client/game/Interaction/_Model/InteractionRegistration.cs` — abstract: `Position`, `Label`, `OnInteraction(ProbeData)`
- [ ] Create `client/game/Interaction/_Model/CallbackInteractionRegistration.cs` — delegate-based concrete registration (untrailed's `SimpleInteractionRegistration`)
- [ ] Create `client/game/Interaction/_Service/InteractionService.cs` — register/unregister, pending-trigger queue, `Process()` resolves closest via distance-squared, `Focused` + `FocusChanged` event recomputed each `Process()`
- [ ] Create `client/game/Interaction/CollisionLayers.cs` — flags enum; verify existing layer usage first
- [ ] Create `client/game/Interaction/_Component/InteractProbe.cs` — Area2D, interactable layer/mask
- [ ] Create `client/game/Interaction/_Component/InteractTarget.cs` — Area2D, `[Export] GuideActionBinding Trigger`, registers/unregisters on probe enter/exit, fires `Service.NotifyTriggered` on `Trigger.JustTriggered` (verify `GuideActionBinding` exposes `JustTriggered`; add if missing)
- [ ] `Player.tscn`: add `%InteractProbe` (Area2D + CircleShape2D radius ~40, scene-declared); `Player.cs`: `[Node]` bind, `[Dependency]` InteractionService, update probe data + `Process()` in `_PhysicsProcess`
- [ ] `Main.cs`: own + provide `InteractionService` via `IProvider`
- [ ] Acceptance: `dotnet build` clean; walking within probe range of a test `InteractTarget` registers it, leaving unregisters (provable in 1.3.5 harness)

## Subtask 1.3.2: Interact input + prompt UI — Scope: S
- [ ] `AppRoot.EnsureInputActions()`: register `interact` → Key.E in `InputMap`
- [ ] Verify `Interact.tres` is mapped in `CharacterContextKbm.tres` (E) and `CharacterContextController.tres` (already authored — confirm trigger=pressed→JustTriggered semantics)
- [ ] Create `client/game/Ui/InteractPrompt.tscn` + `InteractPrompt.cs` — small Control/Label ("[E] {Label}"), subscribes `FocusChanged`, positions above focused target's world position, hidden when none
- [ ] Add `InteractPrompt` to `Main.tscn` (scene-declared)
- [ ] Acceptance: prompt appears when near an interactable, names it, disappears on walk-away

## Subtask 1.3.3: Modal host + UI input context — Scope: M
- [ ] Create `client/game/Ui/_Input/Actions/UiCancel.tres` GUIDE action + `UiContextKbm.tres` (Esc + E) / `UiContextController.tres` (B) mapping contexts + `UiModeContext.tres` (`InputModeContext`)
- [ ] `AppRoot.EnsureInputActions()`: register `ui_cancel_modal` → Key.Escape for validation
- [ ] Create `client/game/Ui/ModalHost.cs` + add `ModalHost` CanvasLayer to `Main.tscn` — `[Export] PackedScene` per `TerminalType`, `Open(TerminalType, roomContext)` instantiates the modal, pushes `UiModeContext` exclusive; `Close()` pops; `IsOpen` exposed
- [ ] Create `client/game/Ui/Modals/RoomInfoModal.tscn` + `RoomInfoModal.cs` — Control panel showing room label, type, power state; wires `UiCancel` → deferred `Close()` (untrailed `CookingUI` pattern incl. `_ExitTree` context cleanup)
- [ ] Wire all 5 `TerminalType` entries to `RoomInfoModal.tscn` in `Main.tscn`
- [ ] Acceptance: opening a modal freezes movement (exclusive context), `UiCancel` closes it and restores movement; no orphaned contexts after repeated open/close

## Subtask 1.3.4: Room terminals — Scope: M
- [ ] Create `client/game/Ship/Terminal/Terminal.tscn` + `Terminal.cs` — visual (ColorRect/_Draw marker) + `InteractTarget` child (scene-declared); exports for slot index, room label, `TerminalType`; registration opens modal via `ModalHost`
- [ ] Spawn terminals data-driven: terminal-spawner node declared in `ShipGrid.tscn` (or ShipGrid itself) instantiates `[Export] PackedScene Terminal` per assigned slot at slot-center cell, reacting to the same assignment data (server `RoomAssignments` + `SetTestAssignment` test path)
- [ ] Terminal label/type resolved through `RoomTypeRegistry`
- [ ] Acceptance: 7 terminals appear in the 7 assigned Corvette rooms in-game; each opens a modal titled with its room's label

## Subtask 1.3.5: Validation scenarios — Scope: M
- [ ] Create `client/validation/harnesses/InteractionHarness.tscn` + `InteractionHarnessController.cs` — ShipGrid + Player + registries + ModalHost + InteractPrompt; controller provides `InteractionService`, seeds test assignments, bridges `interact`→E and `ui_cancel_modal`→Esc in `ActionKeyBridge`; `get_observed_state()` exposes player pos, focused-target id/label, prompt visibility, modal open/title
- [ ] Scenario `terminal_interact_opens_modal.json` (red first): walk to nearest terminal (`wait_until` focused != none) → checkpoint prompt visible → `press_action interact` → assert modal open + title → press move while open, assert position unchanged → `press_action ui_cancel_modal` → assert modal closed + movement restored → screenshots at each checkpoint
- [ ] Scenario `interact_requires_target.json`: interact pressed away from any terminal → no modal; walk near then away → prompt shown then hidden
- [ ] Scenario `scenarios_stdb/terminals_spawn_from_server_assignments.json`: connected harness (reuse `ConnectedGameHarness` pattern) asserts 7 terminals spawn from live `RoomAssignments`
- [ ] Run new scenarios red → implement → green; visually review checkpoint screenshots

## Definition of Done (Task 1.3)
- [ ] All new scenarios pass; screenshots reviewed
- [ ] `./scripts/validate_all.ps1` — both suites green, no regressions
- [ ] Game boots clean ≥10s, zero `ERROR:` lines (`run-game` skill)
- [ ] `dotnet build` + `dotnet csharpier format .` (client), `spacetime build` + format (server — untouched unless stdb scenario needs seeding help)
- [ ] `git push origin`
