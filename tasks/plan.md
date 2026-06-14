# Implementation Plan: Nomad

## Overview

Nomad is a cooperative, top-down 2D sci-fi survival game for up to 8 players. The crew pilots a modular gravity ship fleeing an encroaching galactic cataclysm (The Stellar Night). The core experience blends localized crew-management tension with high-stakes route navigation — played entirely in real-time co-op from a flat, top-down perspective. No direct character-vs-monster combat.

Status: Phase 0 and Phase 1 Tasks 1.1–1.2 are complete. The codebase has a working multiplayer foundation (SpacetimeDB connection, client-authoritative movement, remote-player interpolation), a walkable Corvette interior (rooms, corridors, doors, tile collision), the room type system, and two validation suites (pure + SpacetimeDB-backed).

## Architecture Decisions

- **Godot 4.x + C# client** — Godot renders the game, collects input, plays audio. All game logic lives in SpacetimeDB or is strictly client-side prediction/rendering.
- **SpacetimeDB server-authoritative** — The SpacetimeDB module owns all shared game state. Clients subscribe to tables and call reducers. Reducers are transactional and deterministic (no filesystem, network, timers, or random inside reducers). Reference: [untrailed](https://github.com/upta/untrailed) for working SpacetimeDB + Godot integration patterns.
- **Client-authoritative player position** — Each client owns their player's position (sends to server). Other entities are interpolated/extrapolated client-side. Server validates for anti-cheat.
- **Top-down 2D, tile-based grid** — Flat vector style inspired by RimWorld (§7.1). Characters are single-piece sprites (no skeletal animation). `TileMapLayer` floor/wall layers are painted from hull data; wall cells are derived as every cell touching a floor cell, so layouts stay sealed however the `.tres` is reshaped.
- **Data-driven rooms** — Hull templates and room types defined as `.tres` resources with string IDs that SpacetimeDB references for runtime state (powered, pressurized, etc.). Hull templates define room slots, corridors, and door cells.
- **Scene-first authoring (mandatory)** — `.tscn`/`.tres` are the primary authoring surface; code is for behavior only. Static structure is declared in scenes, tunables are `[Export]`s set in scene/resource files, dynamic spawning instantiates exported `PackedScene`s. See `client/CLAUDE.md` for the full rules.
- **Chickensoft AutoInject** — node binding via `[Meta(typeof(IAutoNode))]` + `[Node]` properties (using `GodotNodeInterfaces` interface types); tree-scoped DI via `IProvider`/`IDependent` instead of singletons.
- **GUIDE input system** — all gameplay input goes through GUIDE action/context resources wrapped by the typed bindings in `client/game/Guide/`; never raw `Input` calls. Scenario-driven actions must also be registered in `InputMap` (see `EnsureInputActions()` in `AppRoot.cs`).
- **Service layer split** — `_Service/` classes are plain C# (state, rules, no node types); `_Scene/` scripts are thin Godot shells that delegate to services.
- **Room-centric power model** — Power/atmosphere calculated per-room, not per-tile (§6.3). Simple binary model initially (powered/unpowered), expandable later. Each room has an interactable breaker switch.
- **Walk-up modal UI** — Complex systems (Star Chart, Power Router, Fabricator) accessed by walking a character up to a physical terminal and interacting. Opens a Godot Control-based modal dialog. No persistent floating HUD menus (§7.2).
- **Pausability** — Real-time gameplay with architectural support for pausing (Stellar Night clock stoppable, reducer gating). The hybrid model means the game is real-time by default but the system can freeze state cleanly.
- **Validation-first** — Every gameplay change includes validation scenarios. Harness controllers expose semantic game state. Validation scenarios run against the SpacetimeDB server + Godot client together.

## Project Structure

```
client/                 → Godot 4.x C# project
  bootstrap/            → App entry point with test-mode routing
  game/                 → Gameplay scenes and C# scripts (Db/, Entities/, Guide/, Map/, Player/, Ship/)
  validation/           → Harnesses, harness controllers, scenarios/ (pure) + scenarios_stdb/ (networked)
  addons/               → Symlinked addons (agentic_godot_validation, guide) — never edit
  Db/                   → Generated SpacetimeDB client bindings — never edit
server/                 → SpacetimeDB module
  src/
    Tables/             → Table definitions (schema)
    Reducers/           → Reducer functions (game logic)
    Types/              → Shared types used by server
    Views/              → Query views for client subscriptions
  spacetime.json        → Module path + generate config
  spacetime.local.json  → Local deployment config
scripts/                → run_stdb_scenarios.ps1 (ephemeral DB suite) + validate_all.ps1 (both suites)
tools/                  → Symlinked validation runner scripts
docs/                   → GDD
tasks/                  → Plan and task tracking
```

Note: shared types beyond the generated bindings have not been needed; there is no `shared/` directory and no production deployment config yet.

## Source-Driven Reference

The [untrailed](https://github.com/upta/untrailed) project is the primary reference for SpacetimeDB + Godot integration patterns:
- SpacetimeDB server module structure: `server/src/Tables/`, `Reducers/`, `Types/`, `Views/`
- Godot client integration: client subscribes to tables, calls reducers
- Client-authoritative movement with interpolation for remote entities
- Build: `spacetime build` in server dir; `dotnet build` in client dir

## Dependency Graph

```
SpacetimeDB Scaffold + Client Connection
    │
    ├── Tile Grid + Camera + Movement (Move verb)
    │       │
    │       ├── Ship Hull Templates & Room System
    │       │       │
    │       │       ├── Power Grid & Breakers (Toggle verb)
    │       │       │       │
    │       │       │       └── Life Support (Oxygen + Pressurization)
    │       │       │
    │       │       └── Interaction Framework (walk-up prompts + terminal modals)
    │       │
    │       ├── Character Core (Health, Death, Ghost)
    │       │       │
    │       │       ├── Inventory & Hotbar (Load verb)
    │       │       │
    │       │       └── Cloning Bay (Biomass + Respawn)
    │       │
    │       ├── Resource Nodes & Harvesting (Harvest verb)
    │       │       │
    │       │       └── Crafting/Refining (Refine verb)
    │       │
    │       ├── Node Activities (Planetside, Wreck, Trading)
    │       │
    │       └── Navigation (Star Chart, Fog of War, Probes, Stellar Night, Jump)
    │
    └── Multiplayer Lobby + Session Management (throughout)
```

## Task List

### Phase 0: Multiplayer Foundation

- [x] **Task 0.1: Rename project + move to client/** — Update `project.godot` config/name from "My Prototype" to "Nomad", rename `.sln` and `.csproj`, update namespaces. Move Godot project from `src/` to `client/` to align with server/client convention. Touch ~5-8 files. **Scope: S**
  - [x] 0.1.1: Rename C# files to PascalCase (`AppRoot.cs`, `Main.cs`) and add file-scoped namespaces (`Nomad.Bootstrap`, `Nomad.Game`)
  - [x] 0.1.2: Update `.tscn` files — rename to PascalCase and fix script path references
  - [x] 0.1.3: Rename project identity — `project.godot` (name/assembly), `MyPrototype.csproj`→`Nomad.csproj`, `MyPrototype.sln`→`Nomad.sln`
  - [x] 0.1.4: Move `src/` → `client/` (directory rename)
  - [x] 0.1.5: Update path references in `symlink-config.txt`, `.gitignore`, `README.md`
  - [x] 0.1.6: Recreate symlinks (`./setup.ps1`), run `dotnet build` from `client/`, verify

- [x] **Task 0.2: SpacetimeDB Server Scaffold** — Scaffold server via `spacetime init`, reorganize into `server/src/Tables/`, `Reducers/`, `Types/`. Use current SDK 2.4.x conventions (OnConnect lifecycle, shorthand attributes like [PrimaryKey], tables nested in partial class Module split across files). Define 3 tables + 3 reducers + 1 type. Build + publish. **Scope: M**
  - [x] 0.2.1: Scaffold via `spacetime init`, clean generated files, move `spacetimedb/`→`src/`, rename `.csproj` to `StdbModule.csproj`, update `spacetime.json`
  - [x] 0.2.2: Create `src/GlobalUsings.cs` + `src/Types/EntityType.cs`
  - [x] 0.2.3: Create table definitions — `Tables/Player.cs`, `Entity.cs`, `EntityOwnership.cs`
  - [x] 0.2.4: Create reducers — `Reducers/Connect.cs`, `Disconnect.cs`, `MoveEntity.cs`
  - [x] 0.2.5: `spacetime build` + `spacetime publish` + update `server.instructions.md` for SDK 2.4.x conventions

- [x] **Task 0.3: SpacetimeDB client integration** — Add SpacetimeDB client SDK to Godot project. Create client-side connection manager that connects to local SpacetimeDB, subscribes to tables, and calls reducers. Verify end-to-end: client connects → reducer called → table updated → client sees update. Reference: untrailed client integration. **Scope: M**

- [x] **Task 0.4: Tile-based grid map + 2D camera** — Create a TileMap-based grid for the ship interior. Add a top-down Camera2D. Set up rendering layers for the flat 2D vector style per GDD §7.1. Camera should follow the local player. **Scope: S**

- [x] **Task 0.5: Player character + WASD movement** — Create a Player scene (CharacterBody2D) with WASD movement. Client-authoritative position sent to SpacetimeDB. Remote players interpolated. Crib from untrailed movement patterns. Implements the Move verb (§5). Add movement validation scenario. **Scope: M**

**Checkpoint: Foundation** — Multiple clients connect to SpacetimeDB, players move on a grid, camera follows, project is named Nomad.

✅ Phase 0 complete. Project restructured, server scaffolded (tables + reducers + ActiveEntities view), client integrated (DbManager + GUIDE input + Player with MovementNetworkSync), tile grid rendered, camera follows player, remote players rendered with SnapshotInterpolator lerp. Two validation scenarios pass.

### Phase 1: Ship Architecture

- [x] **Task 1.1: Hull template data model** — Define `.tres` resource format for hull templates: grid width/height, armor rating, and a list of fixed room slots (each with position, width, height — but no type yet). Create one reference hull ("Corvette", 7 rooms). Each hull and room type has a string ID for SpacetimeDB reference. FTL-style: rooms are fixed positions on the hull, crew assigns types to them in the lobby. **Scope: M**

- [x] **Task 1.2: Room system** — Define 7 room types as `.tres` resources: Reactor, Bridge, Cloning Bay, Hydroponics, Workshop, Kitchen, Cargo Bay. Each has `room_id`, `label`, `power_draw`, `terminal_type` (star_chart, power_router, fabricator, cloning, info), `tileset_ref`. SpacetimeDB stores runtime assignment: `slot_index → room_type_id` + live state (powered, pressurized, breaker, health). Client renders rooms by looking up the assigned type and applying its tileset. **Scope: L**
    - [x] 1.2.1: Server Room Tables + Types — `TerminalType` + `RoomTypeId` enums, `RoomAssignment` table. **Scope: S**
    - [x] 1.2.2: Server AssignRoomType Reducer + Init Seeding — reducer to assign room types, `Init` lifecycle seeds Corvette defaults. **Scope: S**
    - [x] 1.2.3: Client RoomType Resource + 7 Room Types — `TerminalType` enum, `RoomType` [GlobalClass] Resource, 7 `.tres` files, `RoomTypeRegistry` lookup. **Scope: M**
    - [x] 1.2.4: Client Room Rendering in ShipGrid — rewrite `ShipGrid.cs` to read HullTemplate + RoomAssignment + RoomTypeRegistry for type-specific rendering. **Scope: M**
    - [x] 1.2.5: Validation Scenarios — `room_types_load.json`, `rooms_assigned_to_slots.json`, harness + controller. **Scope: M**

- [x] **Task 1.2b (retro): Walkable interior + infrastructure hardening** — Work completed between 1.2 and 1.3 that the plan didn't originally call for:
  - Walkable ship interior: `TileMapLayer` floor/wall rendering from hull data (`ShipTileSet.tres` + atlas), corridors and door cells in `HullTemplate`, derived wall collision, Corvette regrown to 29×16. Validated by `ship_layout_renders`, `player_blocked_by_wall`, `player_walks_corridor`, `player_enters_room_through_door`, `player_slides_past_door_corner`.
  - SpacetimeDB validation suite: `client/validation/scenarios_stdb/` with puppet-client multiplayer scenarios, `scripts/run_stdb_scenarios.ps1` (ephemeral DB), `scripts/validate_all.ps1` entry point; fixed remote-player vanish bug.
  - Scene-first refactors: `Main.tscn` declares camera/player/hull/registries, `RemoteEntity.tscn` for remote players, Chickensoft AutoInject adopted for node binding.
  - Input + movement polish: GUIDE input system with typed C# wrappers, smooth local movement, circle player collider, editor multi-client run instances.

- [x] **Task 1.3: Interaction framework (walk-up prompts + terminal modals)** — The shared system every later interactable rides on: an `Interactable` component/area that registers with the player, proximity detection picking the nearest target, an interact prompt, the Interact verb (GUIDE action already authored in `game/Player/_Input/Actions/Interact.tres`), and a modal host that opens/closes a Control-based overlay per terminal type (§7.2 — no persistent floating menus). Steal the working interaction system from [untrailed](https://github.com/upta/untrailed) (checked out at `C:\Code\Github\upta\untrailed`, `client/src/Core/Interaction/` + `client/src/App/Interaction/`) and adapt it. Scene-first: interactables and modals are scenes; behavior delegates to services. Validation: scenario proving walk-up → prompt → interact → modal opens → close. **Scope: M**
    - [x] 1.3.1: Interaction core — port `InteractProbe`, `InteractTarget`, `InteractionRegistration`, `ProbeData`, `InteractionService` into `client/game/Interaction/`; closest-target resolve plus a per-frame focused-target event (untrailed lacks this; the prompt needs it); dedicated interactable collision layer; Player carries `%InteractProbe` and pumps the service; `InteractionService` provided via AutoInject `IProvider`. **Scope: M**
    - [x] 1.3.2: Interact input + prompt — register `interact` in `InputMap` (`EnsureInputActions()`) and harness key bridges; wire the existing `Interact.tres` GUIDE binding into `InteractTarget`; `InteractPrompt` scene follows the focused interactable, rendering the bound key device-aware via GUIDE's `GUIDEInputFormatter`. **Scope: S**
    - [x] 1.3.3: Modal host + UI input context — `ModalHost` CanvasLayer declared in `Main.tscn`; `UiCancel` GUIDE action + `UiModeContext` pushed exclusive on open (movement freezes), popped on close; per-`TerminalType` `[Export] PackedScene` mapping with `RoomInfoModal` (room label/type/power status) as every type's initial content — specialized modals land with their owning tasks (1.4 PowerRouter, 4.3 Fabricator, 6.1 StarChart, 2.4 Cloning). **Scope: M**
    - [x] 1.3.4: Room terminals — `Terminal.tscn` interactable spawned data-driven per assigned room slot (slot-center cell, from the same room-assignment data ShipGrid renders), opens its room's terminal modal through the host. **Scope: M**
    - [x] 1.3.5: Validation — interaction harness + controller exposing focused target / prompt / modal / player state; pure scenarios (walk-up → prompt → interact → modal opens → movement locked → cancel closes; interact-with-no-target and walk-away-hides-prompt edges) plus one `scenarios_stdb/` scenario proving terminals spawn from live server room assignments. **Scope: M**

- [x] **Task 1.4: Power grid + breaker switches** — Binary power model with varied per-room draw (server-owned, mirrors the `.tres` PowerDraw values; Reactor is the generator, draw 0). If total demand ≤ reactor output all breaker-on rooms are powered; demand > output enters an Overload warning (client renders flicker) and full blackout follows after a database-driven grace window (`PowerGrid.GraceMillis`, default 10s) unless load is shed. Breaker/grid state lives in SpacetimeDB; players toggle physical wall breakers (Toggle verb per §5) via the Task 1.3 framework, and the Reactor terminal's PowerRouter modal (deferred from 1.3) shows the grid overview with remote toggles. **Scope: M**
    - [x] 1.4.1: Server power model — `PowerGrid` single-row table (ReactorOutput, GraceMillis, Status, BlackoutAt), `GridStatus` enum, `PowerRules` (per-RoomTypeId draws + `RecomputePowerGrid`), reducers `ToggleBreaker`/`SetReactorOutput`/`SetBlackoutGrace`, scheduled `GridBlackoutTick`, Init seeding; publish + regenerate bindings. **Scope: M**
    - [x] 1.4.2: stdb validation of the reducer loop — `power` section in `ConnectedGameHarnessController` observed state + harness test actions calling reducers; scenarios: breaker round-trip, overload→blackout→recovery (short grace), grace recovery without blackout. **Scope: S**
    - [x] 1.4.3: Client rendering — unpowered rooms dim (`GetRoomColor`), Overload flicker (`_Process` + `FlickerCycles` counter), PowerGrid subscription; `PowerHarness` + pure scenarios for dim + flicker. **Scope: M**
    - [x] 1.4.4: Physical wall breakers — `Breaker.tscn`/`.cs` interactable spawned per assigned slot (top-left interior cell) by ShipGrid, interact toggles via reducer (connected) or test setters (pure); pure scenario walk-up → toggle → dim → toggle back. **Scope: M**
    - [x] 1.4.5: PowerGridService + PowerRouter modal — plain-C# service (provided by Main/harnesses, feeds on RoomAssignments + PowerGrids), `PowerRouterModal` + focusable rows with remote toggles wired into ModalHost's Reactor-terminal slot; pure modal-toggle scenario (`ui_down`/`ui_accept`) + stdb server-toggle scenario. **Scope: M**
    - [x] 1.4.6: DoD sweep — both suites green, game boots clean, builds + format, screenshot review, push. **Scope: S**

- [x] **Task 1.5: Room pressurization (+ corridors become pressurizable rooms)** — Rooms are pressurized or unpressurized; SpacetimeDB owns pressurization state (`RoomAssignment.IsPressurized`, already in the schema). Render depressurized rooms with a vacuum tint (lerp toward a cold blue-gray before power dimming — structurally distinguishable from unpowered darkening in color asserts). The corridor becomes room slot 7 (`RoomTypeId.Corridor`, draw 0, not player-assignable, no terminal/breaker, hidden from the PowerRouter modal) so it can hold pressure state too — one pressure unit for the whole corridor network. Foundation for the oxygen tether (Phase 2). Hull breaches as a *cause* land complete with Task 5.6; this task ships a `SetPressurization` reducer as the server-side way to set state for validation. **Scope: M**
    - [x] 1.5.1: Server — `Corridor` RoomTypeId + slot-7 seed + `SetPressurization` reducer (no power recompute; `AssignRoomType` rejects Corridor); publish `--delete-data=always` + regenerate bindings. **Scope: M**
    - [x] 1.5.2: stdb validation — pressurization test reducer actions + `is_pressurized` in observed power state; `pressurization_reducer_round_trip.json` (kitchen + corridor round trips, power-grid-untouched guard asserts). **Scope: S**
    - [x] 1.5.3: Client corridor-as-room — `CorridorRoom.tres`, RoomTypeRegistry refactor to `[Export]` RoomType array (anti-pattern cleanup, wired in Main + 6 harness scenes), ShipGrid corridor tint rect + observed corridor entry, PowerGridService skips Corridor. **Scope: M**
    - [x] 1.5.4: Client vacuum-tint rendering — `VacuumTint`/`DepressurizedBlend` exports in ShipGrid.tscn, `GetRoomColor` lerp, `SetTestPressurization` + `SetTestAssignment` isPressurized param, `is_pressurized` observed, `ModalHost.CurrentInfo`. **Scope: S**
    - [x] 1.5.5: Pure validation — PowerHarness pressurization test actions + corridor seed; scenarios: vacuum-tint color fingerprint (r↓ b↑), composes-with-unpowered, corridor depressurizes, RoomInfoModal shows "Pressure: Lost". **Scope: M**
    - [x] 1.5.6: End-to-end render assert (`game.grid` in ConnectedGameHarness, fingerprint in connected client) + DoD sweep. **Scope: S**

**Checkpoint: Ship** ✅ — Hull loads, 7 rooms exist with breakers, interaction framework works, rooms can be powered/unpowered, pressurization state tracked (rooms + corridor), depressurized spaces render a vacuum tint. Phase 1 complete.

### Phase 2: Character Systems

- [x] **Task 2.1: Character health + damage pipeline** — Characters have a health meter. SpacetimeDB owns health state and a generic damage pipeline (typed damage sources, death at zero). Client renders health bar. Each damage *source* ships complete with its owning feature: suffocation with 2.2, starvation with 2.3, fire with 5.1, hostile creatures with 5.2/5.3 (§6.4). This task includes a debug/validation damage reducer so the pipeline is provable before those sources exist. **Scope: M**
    - [x] 2.1.1: Server — `Meter` + `DamageType` types, `Vitals` table, `DamageRules.ApplyDamage`, `ApplyDebugDamage`/`ResetVitals` reducers, Connect seeding; publish + bindings. **Scope: M**
    - [x] 2.1.2: stdb validation — `vitals` observed state + damage/kill/reset test actions; `health_damage_pipeline_round_trip.json`. **Scope: S**
    - [x] 2.1.3: Client — `VitalsService` (plain C#) + `VitalsHud` health bar in Main; `VitalsHudHarness` + `vitals_health_bar_renders.json`. **Scope: M**
    - [x] 2.1.4: DoD sweep. **Scope: S**

- [x] **Task 2.2: Oxygen tether + spacesuits** — Personal oxygen tank depletes in vacuum/unpressurized areas, refills in pressurized powered rooms; empty tank applies suffocation damage via the 2.1 pipeline (§6.2). SpacetimeDB owns oxygen state. Includes spacesuits: equipped at a suit rack interactable (Task 1.3 framework) in the Cargo Bay, expanding tank capacity at the cost of movement speed — the full oxygen feature lands here. Room presence tracked via client-computed `SetPlayerRoom` reducer calls on transitions. **Scope: L**
    - [x] 2.2.1: Server room tracking — `Player.CurrentSlotIndex` + `SetPlayerRoom` reducer. **Scope: S**
    - [x] 2.2.2: Client `RoomLocator` service + Player transition calls; `player_room_tracking.json` (stdb). **Scope: M**
    - [x] 2.2.3: Server oxygen — `Meter Oxygen`/`SuitEquipped` on Vitals, `VitalsConfig` table, repeating `VitalsTick` scheduled reducer, `SetSuitEquipped` + config/debug reducers. **Scope: M**
    - [x] 2.2.4: stdb validation — `oxygen_depletes_and_refills.json`, `oxygen_empty_suffocates.json`. **Scope: M**
    - [x] 2.2.5: Client — oxygen HUD bar, `SuitRack` interactable in CargoBay slot, suit speed modifier + tint; pure scenarios. **Scope: M**
    - [x] 2.2.6: stdb suit round trip + DoD sweep. **Scope: S**

- [x] **Task 2.3: Food/hunger meter** — Secondary metabolic meter that depletes slowly; at zero, starvation damage via the 2.1 pipeline. Replenished via `RestoreHunger` (the entry point Phase 3 meals will call) (§6.2). Server-authoritative. **Scope: S**
    - [x] 2.3.1: Server — `Meter Hunger` on Vitals + tick depletion + starvation damage + `RestoreHunger`. **Scope: S**
    - [x] 2.3.2: Hunger HUD bar + pure/stdb scenarios + DoD sweep. **Scope: S**

- [x] **Task 2.4: Death, ghost state, cloning bay** — Death is non-terminal. Deceased players enter Ghost Mode (visible to all players, can float anywhere, cannot interact with physical objects — exception: the Cloning Bay terminal, their anchor back to life). Cloning Bay respawns at Biomass cost (`ShipStores.Biomass`, seed 3). If no power or biomass, stay ghosted (§6.4). SpacetimeDB manages ghost/clone state. **Scope: L**
    - [x] 2.4.1: Server — `ShipStores` table, `RequestRespawn(target)` reducer (dead + CloningBay powered + biomass), `HullGeometry` slot centers, `SetBiomass` debug setter. **Scope: M**
    - [x] 2.4.2: stdb validation — respawn round trip + power/biomass rejection scenarios. **Scope: S**
    - [x] 2.4.3: Client ghost mode — collision off, ghost tint, cloning-only interaction (registration `GhostAccessible` + service `IsGhost` filter), respawn snap; remote ghost tint; `GhostHarness` + pure scenarios. **Scope: M**
    - [x] 2.4.4: `CloningModal` (biomass + dead-crew rows with Clone buttons) wired into ModalHost; pure + stdb scenarios. **Scope: M**
    - [x] 2.4.5: DoD sweep + Phase 2 checkpoint. **Scope: S**

**Checkpoint: Characters** ✅ — Characters have vitals (health, oxygen, hunger), can die, ghost, and respawn. Phase 2 complete (27 pure + 18 stdb scenarios green).

### Phase 3: Inventory & Logistics

Full plan: `C:\Users\upta\.claude\plans\polished-watching-liskov.md`. One `Item` table row per item with a `LocationKind` discriminator (World / Hotbar / Stored) — deliberately avoiding untrailed's 5-rows-per-dropped-item container indirection, its client-trust holes (reducers here take explicit slot indexes with server-side reach/alive/ownership checks; hotbar selection is pure client UI state), and its hardcoded capacities (`InventoryConfig` single-row table).

- [x] **Task 3.1: Item types** — Define the item-type system plus the types needed by Phases 3-4: raw ore, fuel deposits, biomass, fuel cells, scrap, components (meals moved to 4.4 — see Resolved Design Decisions). Items have visual representations for world rendering. Items are SpacetimeDB entities. Ammunition crates land with 5.5 and probes with 6.2 — each feature defines its own item types when it ships. **Scope: M**
    - [x] 3.1.1: Server — `ItemTypeId`/`ItemLocationKind` enums, `Item` table (full schema incl. Hotbar + Stored fields, one publish), `InventoryConfig` single-row table, `ItemRules` helpers, `SpawnWorldItem`/`ClearItems` debug reducers, Init seeding; publish `--delete-data=always` + bindings. **Scope: M**
    - [x] 3.1.2: stdb validation — `items` observed state + spawn/clear test actions; `world_items_spawn_and_render.json`. **Scope: S**
    - [x] 3.1.3: Client — `ItemType` resource + `ItemTypeRegistry` + 6 `.tres`, `WorldItem` scene (InteractTarget), `ItemSpawner` node in Main, `InventoryService` world half (OnUpdate evicts rows leaving World). **Scope: M**
    - [x] 3.1.4: Pure validation — `InventoryHarness`, `item_types_load.json`, `world_items_render.json`. **Scope: S**
    - [x] 3.1.5: DoD sweep. **Scope: S**

- [x] **Task 3.2: Fixed-size hotbar** — 4-slot hotbar (DB-configurable), no hidden inventory (§6.1). Every item = 1 slot regardless of type, no stacking. SpacetimeDB owns inventory state; hotbar selection is client-only UI state. Direct-select keys 1–4 + drop on Q (repurpose the leftover `HotbarDropItem.tres`, delete `HotbarCycleSlot.tres`). **Scope: M**
    - [x] 3.2.1: Server — `GiveItem` debug reducer (slot bounds + occupancy + alive checks). **Scope: S**
    - [x] 3.2.2: stdb validation — `items.hotbar` observed; `hotbar_state_round_trip.json` incl. occupied-slot rejection. **Scope: S**
    - [x] 3.2.3: Client — InventoryService hotbar half, `HotbarSlot1..4.tres` GUIDE actions + context rewiring, `ItemSlotPanel` shared slot visual, `HotbarHud` in Main. **Scope: M**
    - [x] 3.2.4: Pure validation — `hotbar_renders_items.json`, `hotbar_inert_while_modal_open.json` (exclusive-context proof). **Scope: M**
    - [x] 3.2.5: DoD sweep. **Scope: S**

- [x] **Task 3.3: Item pickup/drop** — Walk-up + E interact picks an item into the first free hotbar slot; Q drops the selected item at the player's position. SpacetimeDB validates every pickup/drop: reach (server-side distance vs `PickupRadius`), alive, slot occupancy. Death drops all hotbar items at the death position (single hook in `DamageRules.ApplyDamage`). **Scope: M**
    - [x] 3.3.1: Server — `PickUpItem`/`DropItem` reducers + `DropAllHotbarItems` death hook. **Scope: M**
    - [x] 3.3.2: stdb validation — round trip, out-of-reach rejection, full-hotbar rejection, `death_drops_hotbar.json`. **Scope: M**
    - [x] 3.3.3: Client — interact→pickup and drop wiring through InventoryService; world-node cleanup chain. **Scope: M**
    - [x] 3.3.4: Pure validation — prompt/mirror round trip, `ghost_cannot_pickup.json`. **Scope: M**
    - [x] 3.3.5: DoD sweep. **Scope: S**

- [x] **Task 3.4: Load verb — tank deposits + reactor fuel burn** — Deposit items from hotbar into machine intakes via walk-up modals (§5): biomass→Cloning Bay (`ShipStores.Biomass++`), fuel cell→Reactor (`ShipStores.Fuel++`) — tank model: the machine consumes the item into a counter. The Reactor burns fuel on a scheduled tick while generating (`PowerGrid.FuelPerBurn`/`FuelBurnMillis`, DB-tunable, **0 = disabled** so scenarios opt out); dry tank ⇒ output 0 ⇒ overload→blackout via the 1.4 machinery — the keep-the-reactor-fed pressure loop lands whole here. Server-side reach check vs room slot center (`LoadRadius`); never trusts modal-open state. Ammo→weapons is the same verb but lands with 5.5. **Scope: L**
    - [x] 3.4.1: Server — `ShipStores.Fuel` + `PowerGrid` burn fields + `FuelBurnTimer`/`FuelBurnTick`, `LoadItem` reducer, `SetFuel`/`SetFuelBurn` setters; publish `--delete-data=always`. **Scope: M**
    - [x] 3.4.2: stdb validation — `load_reducer_validation.json` (type + reach rejections), `reactor_fuel_burn_blackout_recovery.json`; existing power scenarios gain a `test_disable_fuel_burn` precondition. **Scope: M**
    - [x] 3.4.3: Client — `RoomModalInfo.SlotIndex`, shared `DepositRow`, CloningModal biomass deposit, PowerRouterModal fuel section. **Scope: M**
    - [x] 3.4.4: Pure validation — modal deposit scenarios for both terminals. **Scope: M**
    - [x] 3.4.5: DoD sweep. **Scope: S**

- [x] **Task 3.5: Cargo Bay storage (store/withdraw)** — The Cargo Bay holds a capacity-limited generic store ("haul back to cargo bay storage", GDD §4): deposit any item from the hotbar, withdraw later. Same `LoadItem` deposit verb (storage branch) + `WithdrawItem`; `StorageModal` on the Cargo Bay terminal is an untrailed-`WagonInventoryUI`-inspired dual slot grid (hotbar ↔ cargo), improved: service-fed, scene-styled, in-place updates, focus-navigable. Stored items are the ship's — unaffected by death. **Scope: M**
    - [x] 3.5.1: Server — `AcceptsStorage` + `FindFreeStoreSlot`, `LoadItem` storage branch, `WithdrawItem` reducer (reducer-only publish). **Scope: M**
    - [x] 3.5.2: stdb validation — store/withdraw round trip + rejection matrix (reach, non-storage room, full store, full hotbar). **Scope: M**
    - [x] 3.5.3: Client — `ItemSlotGrid` + `StorageModal` dual grid, CargoBay terminal type → Storage, ModalHost slot. **Scope: M**
    - [x] 3.5.4: Pure validation + `scenarios_stdb/items_multiplayer_visibility.json` (puppet client sees drops/stores — checkpoint proof). **Scope: M**
    - [x] 3.5.5: DoD sweep + Phase 3 checkpoint. **Scope: S**

**Checkpoint: Inventory** ✅ — Full pickup→carry→deposit loop works multiplayer. Phase 3 complete (36 pure + 31 stdb scenarios green).

### Phase 4: Resource Economy

Full plan: `C:\Users\upta\.claude\plans\nested-sauteeing-sketch.md`.

- [x] **Task 4.1: Resource nodes** — Harvestable resource nodes: ore veins, wreckage debris, fuel deposits, biomass patches. Finite yields with visual depletion states (full → desaturated → husk). SpacetimeDB owns node state (`ResourceNode` table). Ship-interior placeholder placement (Init-seeded on open corridor floor); position-agnostic so Phase 5.2 relocates to exterior grids unchanged. **Scope: M**

- [x] **Task 4.2: Harvest verb (channeled)** — Stand near a node, interact to extract one unit into the hotbar over time (§5); movement cancels. A single shared repeating `ChannelTick` writes a server `Progress` float (0→1) and completes channels when `now ≥ CompletesAt` — no one-shot timers, client reads `Progress` directly. Ports trail's `RadialProgress` shader to C# with Chunked/Continuous modes for the channel ring. **Scope: L**

- [x] **Task 4.3: Bench crafting + Fabricator modal (Refine verb)** — Interact with Workshop/Kitchen benches to queue crafts via a walk-up Fabricator modal (§5). Benches get type-restricted input/output storage (reusing the Cargo Bay `Stored`-item stack with config-defined input/output `SlotIndex` zones); `QueueCraft` sources ingredients hotbar-first then bench-input, outputs land in reserved output slots. `CraftingJob` queue ordered by `QueuedAt`; completion runs inside `ChannelTick`. Proven with the Fuel Cell recipe at the Workshop. **Scope: L**

- [x] **Task 4.4: Crafting recipes + meals whole** — Recipes carry a bench `RoomTypeId`: Fuel Cells (FuelDeposit + RawOre) at the Workshop, Meals (Biomass) at the Kitchen. Owns the **meals feature whole**: Meal item type, recipe, and eat-from-hotbar on **F** (→ the 2.3 `RestoreHunger` entry point via a shared `RestoreHungerFor` helper) land together here (moved from 3.2 per the feature-complete rule). Closes with an end-to-end multiplayer economy-loop scenario + removal of the now-obsolete dev item seeds. The Ammo Crate recipe ships with 5.5 and the Probe recipe with 6.2, alongside their consumers. **Scope: M**

**Checkpoint: Economy** ✅ — Harvest→Refine→Load→Consume loop works end-to-end in multiplayer (proven by `economy_loop_end_to_end.json`). Phase 4 complete (47 pure + 47 stdb scenarios green).

### Phase 5: Node Activities

Full subtask breakdown + design notes: **`tasks/todo.md` → Phase 5 section** (authored 2026-06-13 via `/agent-skills:plan`). The ship anchors at a node; each node type presents distinct activities/hazards. Navigation (Star Chart/Jump/Stellar Night) lands in Phase 6 — until then nodes are debug-switched via a `SetActiveNode` reducer (the eventual jump target). **Architecture (user-confirmed):** the ship interior becomes a reusable `Ship` component placed into per-node `Map` scenes; `Main` becomes a `MapHost` that swaps maps on the active `NodeKind`. A single-row `NodeActivity` table owns node state; `SetActiveNode` clears/seeds transient node content while persistent ship state and ship hazards (fire, breaches) carry over.

- [ ] **Task 5.1: Node framework + map host + hazard system (fire)** — `Ship`-component + `MapHost` refactor (regression-gated), `NodeActivity` state + `SetActiveNode` + the `NodeRules.SeedNode` dispatcher every later node task extends, and the hazard framework with **fire** complete: positioned `Hazard` entities, deterministic spread, proximity damage via the already-declared `DamageType.Fire` (§6.4), walk-up extinguish. **Scope: L**

- [ ] **Task 5.2: Planetside map + airlock + creatures** — Builds the reusable exterior-grid + airlock-transition system on the MapHost seam. Players exit airlocks onto a `PlanetsideMap` to harvest surface nodes (suit required — falls out of existing oxygen rules: exterior = no room → oxygen depletes). Ships **hostile creatures** whole: server-authoritative roaming + contact damage via the already-declared `DamageType.Creature`; avoid/flee, no combat (§6.4, §8). **Scope: L**

- [ ] **Task 5.3: Abandoned wreck salvage** — Reuses the 5.2 airlock/exterior-grid system for a `WreckMap` derelict layout; salvage scattered loot + debris nodes hauled back to cargo (existing storage); reuses 5.1 fire + 5.2 creatures as wreck hazards. **Scope: M–L**

- [ ] **Task 5.4: Trading station (credits, docked-station map)** — Reuses the 5.2 airlock/exterior-grid system for a `TradingStationMap` (an automated depot / merchant station the ship links with; players cross over to trade). `ShipStores.Credits` + a static `TradeRules` buy/sell catalog; `BuyItem`/`SellItem` at the station's terminal (node + reach gated) via a `TradingModal`. Credits currency + docked-station presentation (user-confirmed). Room upgrades remain post-prototype (§2.1). **Scope: L** (depends on 5.2)

- [ ] **Task 5.5: Automated defense node** — Per-weapon ammo (user-confirmed): weapons are ship fixtures with their own ammo counter, powered via room breakers, auto-targeting incoming threats via `WeaponTick` (no aiming, §8). Ships ammo whole: `Ammo` item, `AmmoCrate` Workshop recipe, 4th `LoadItem` ammo branch (the `ItemRules` "Ammo joins in 5.5" hook). Players keep guns fed + juggle power. **Scope: L**

- [ ] **Task 5.6: Core maintenance node + hull breaches** — The Quiet node is the maintenance home (refine/cook/restore power already work). Owns **hull breaches** end-to-end: a breach depressurizes its room via the 1.5 `IsPressurized` state and persists until patched (consumes Scrap). Hazard/event causes land here; Stellar Night overstay becomes a cause in 6.4. **Scope: M**

**Checkpoint: Nodes** — 5 node types playable (Planetside, Wreck, Trading, Defense, Maintenance) + fire hazard, all debug-switchable via a DebugHud node selector.

### Phase 6: Navigation & The Stellar Night

- [ ] **Task 6.1: Star Chart map + fog of war** — Bridge terminal shows adjacent paths clearly; distant sectors obscured (§6.5). Walk-up modal on Bridge console. SpacetimeDB owns star chart state and fog of war per-session. **Scope: L**

- [ ] **Task 6.2: Node scanning + long-range probes** — Short-range scans reveal adjacent node details. Long-range probes clear fog of war on distant nodes. Ships complete with the whole probe feature: Probe item type, Probe recipe at the workshop (metal composites, §6.5), and launch from the Bridge console. SpacetimeDB processes scan/probe results. **Scope: M**

- [ ] **Task 6.3: Jump mechanic** — Select destination on Star Chart (any player can initiate), warm up jump drive, transit with effect, anchor at new node. SpacetimeDB manages jump state and anchors. **Scope: M**

- [ ] **Task 6.4: The Stellar Night threat** — Real-time encroaching darkness on Star Chart. Hybrid: real-time by default, architecturally pausable. Staying too long doubles power consumption and inflicts structural damage via the 5.6 hull-breach system. SpacetimeDB owns the clock and threat state. Drives the Escape phase (§3). **Scope: L**

- [ ] **Task 6.5: Core loop integration** — Wire together Scan→Vote→Jump→Engage→Escape cycle. End-to-end playable session from lobby through one complete node cycle. **Scope: L**

**Checkpoint: Loop** — Full core gameplay loop functional in multiplayer.

### Phase 7: Polish & Meta

- [ ] **Task 7.1: Pre-run lobby + hull selection** — Setup screen where crew selects hull template and slots modular rooms before launching a run (§2.1). Walk-up or lobby UI. SpacetimeDB manages pre-game state. **Scope: M**

- [ ] **Task 7.2: Character specialization roles** — 8 roles (§2.2): Engineer, Scientist, Chef, Pilot, Logistics, Tactical, Doctor, Diplomat. Targeted bonuses without penalizing basic movement. **Scope: L** (can be staged — implement 2-3 roles first)

- [ ] **Task 7.3: Multiplayer polish** — Player join/leave handling, reconnection, host migration if applicable. Session management improvements. **Scope: M**

- [ ] **Task 7.4: Visual polish** — Character sprites, room tiles, UI styling in RimWorld-inspired flat 2D vector style (§7.1). No complex sprite sheets or skeletal animation. **Scope: L**

- [ ] **Task 7.5: Menu flows** — Main menu, settings, credits. Pre-game lobby. Post-run summary screen. **Scope: M**

**Checkpoint: Complete** — Feature-complete prototype ready for playtesting.

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| SpacetimeDB learning curve + integration complexity | High | Reference untrailed extensively. Start with Phase 0 connection handshake before any gameplay. The `server/AGENTS.md` in untrailed has detailed SpacetimeDB rules. |
| Client-authoritative movement enables cheating | Medium | Server validates position deltas and speed. Acceptable for prototype — tighten later. |
| SpacetimeDB reducer determinism constraints | Medium | Keep reducers pure: no I/O, no RNG, no timers. Use scheduled reducers for timed events. |
| Node activities are nebulous — need playtesting to feel right | Medium | Build the framework (5.1) first, iterate on each type. Validation proves mechanics work; human playtesting tunes feel. |
| Scope creep from GDD ideas | Medium | Feature-complete-on-implementation policy: no half-features scattered across phases. Each feature (ammo, probes, fire, creatures, breaches, spacesuits) lands whole inside its owning task. Room upgrades and specializations beyond 2-3 roles remain post-prototype. |
| Godot C# + SpacetimeDB SDK compatibility | Low | Both are .NET-based. Untrailed proves the integration works. Pin SDK versions. |

## Resolved Design Decisions

- **Godot project location:** Move from `src/` to `client/` to align with untrailed server/client convention.
- **Hull & room template model:** Hull defines fixed room positions (FTL-style). Each room has a position and size on the grid. Players assign room types to each slot in the pre-run lobby. No placement constraints — any room type in any slot. SpacetimeDB stores `slot_index → room_type_id` + runtime state (powered, pressurized, breaker, health).
- **Room set:** 7 rooms for the Corvette hull: Reactor, Bridge, Cloning Bay, Hydroponics, Workshop, Kitchen, Cargo Bay.
- **Terminal assignment:** Every room type has a terminal (for consistency). Core terminals: Bridge→Star Chart, Reactor→Power Router, Workshop→Fabricator, Cloning Bay→Clone/Respawn. Others show room status info initially.
- **Jump voting:** No formal vote — any player can select the next destination and launch. Crew self-organizes.
- **Stellar Night tick rate:** Configurable constant (e.g. `TICK_RATE_HZ = 1`). Tunable during playtesting.
- **Room type `.tres` fields (as built):** `RoomId` (string), `Label` (display name), `PowerDraw` (int), `TerminalType` (enum: None, StarChart, PowerRouter, Fabricator, Cloning, Info), `Color` (room tint — replaced the planned `tileset_ref`; floor/wall visuals come from the shared `ShipTileSet.tres`).
- **Hull template (as built):** `HullTemplate` defines `RoomSlots` plus `Corridors` (GridRect array) and `Doors` (cell list); wall cells are derived from floor cells at load. Corvette is 29×16 with a central corridor, not the originally planned 8×6.
- **Feature-complete on implementation:** when a feature first ships, it ships whole — item types, recipes, consumers, and damage sources are bundled into the task that owns the feature rather than split across phases with deferral notes. (Decided 2026-06-11.)
- **Room upgrades:** post-prototype, per GDD §2.1 ("not fully fleshed out for the initial prototype"). Trading (5.4) does not sell upgrades in the prototype.
- **Corridors are rooms (Task 1.5):** corridors need pressure state, so the corridor network becomes a runtime room slot (slot index = `RoomSlots.Count`, Corvette: 7) with `RoomTypeId.Corridor` — power draw 0, not player-assignable, no terminal/breaker interactables, hidden from the PowerRouter modal. Hull geometry stays in `HullTemplate.Corridors`; only runtime state rides the `RoomAssignment` table. One pressure unit per corridor network. (Decided 2026-06-11.)
- **Item model (Phase 3):** one `Item` table row per item with a `LocationKind` discriminator (World / Hotbar / Stored) — no container tables, no `Quantity` (GDD: 1 item = 1 slot, no stacking). Reducers take explicit slot indexes and validate sender/alive/reach server-side; hotbar selection is client-only UI state the server never trusts. Capacities and radii live in the `InventoryConfig` single-row table. (Decided 2026-06-12.)
- **Holistic deposit model (Phase 3):** depositing into a room branches on the room — storage rooms (Cargo Bay, generic, capacity-limited, withdrawable) hold items; machine intakes (Reactor+fuel cell, Cloning Bay+biomass) are tanks that consume the item into a `ShipStores` counter. One `LoadItem` verb, two behaviors. (Decided 2026-06-12.)
- **Reactor fuel burn (Task 3.4):** the Reactor consumes fuel on a scheduled tick while generating; dry tank ⇒ output 0 ⇒ overload→blackout via the 1.4 flow. `PowerGrid.FuelPerBurn = 0` disables burn entirely (validation scenarios opt out with one precondition). (Decided 2026-06-12.)
- **Death drops hotbar items** at the death position (retrieval-run gameplay); stored items are the ship's and unaffected. Ghosts cannot pick up. (Decided 2026-06-12.)
- **Pickup is walk-up + E interact** through the 1.3 framework — no auto-pickup on walk-over. (Decided 2026-06-12.)
- **Meals moved to 4.4:** meal item type + recipe + eat-from-hotbar land together with the Kitchen recipe per the feature-complete rule (ammo-with-5.5 precedent), not split across 3.2/4.4. (Decided 2026-06-12.)
- **Power model (Task 1.4):** varied draw per room type rather than flat 1/room — long-term, placeable stations inside rooms will drive power cost. Reactor draws 0 and generates `PowerGrid.ReactorOutput` (seed 10 vs total demand 8). Overload (demand > output) gives a flickering warning for a database-driven grace window (`PowerGrid.GraceMillis`, default 10s) before full blackout; shedding load during grace recovers. Reactor breaker off ⇒ output 0 (grid-wide overload). PowerRouter modal shows the grid overview **with remote breaker toggles** — physical sprinting to wall breakers stays, but reactor-room convenience is a deliberate ship-building trade-off. (Decided 2026-06-11.)
- **Resource node placement (Phase 4):** nodes are ship-interior placeholders (Init-seeded on open corridor floor) for the prototype. The `ResourceNode` table + harvest verb are position-agnostic, so Phase 5.2 relocates spawning to exterior planetside grids with no rework. (Decided 2026-06-12.)
- **Channeled harvest + server progress float (Phase 4):** harvest is a timed channel; a single shared repeating `ChannelTick` (~150ms) writes a `Progress` float (0→1) onto the channel row and completes when `now ≥ CompletesAt`. Chosen over per-channel one-shot completion timers because it removes the stale-timer double-fire hazard, the `TimerId` bookkeeping, and the module-identity guard (completion is gated on real server time), and gives the client skew-free rendering (read `Progress` directly), pausability, and correct late-join. Cost: steady row-update traffic during channels (negligible at ≤8 players), smoothed client-side by the Chunked progress ring. Movement cancels the channel; the server re-checks reach at completion regardless (bounded client-trust). (Decided 2026-06-12.)
- **Bench input/output storage (Phase 4):** Workshop/Kitchen benches reuse the Cargo Bay storage stack (`Item.LocationKind.Stored` on the bench's `RoomSlotIndex`) rather than spawning floor items. Config-defined reserved `SlotIndex` ranges split **input** (`BenchInputSlots`, accepts only the bench's recipe ingredient types) from **output** (`BenchOutputSlots`, the reserved completion target — guarantees a finished craft a home, no full-store race). `QueueCraft` sources each ingredient hotbar-first then bench-input. No `Item` schema change. (Decided 2026-06-12.)
- **Kitchen owns meals (Phase 4):** recipes carry a bench `RoomTypeId` — Fuel Cells craft at the Workshop, Meals at the Kitchen (Kitchen terminal flips Info → Fabricator). One crafting system, two benches, rather than overloading the Workshop. (Decided 2026-06-12.)
- **RadialProgress port (Phase 4):** the channel/craft progress ring ports trail's `radial_progress` shader component to C#, upgraded with a `ProgressMode` export — **Chunked** (tweened easing toward discrete targets, the original behavior) and **Continuous** (direct per-frame set). Fed by the server `Progress` float; Chunked smooths the discrete per-tick updates. (Decided 2026-06-12.)
- **Node Activity layer (Phase 5):** the ship anchors at one node; a single-row `NodeActivity` table holds the `NodeKind`. Jump/Star Chart land in Phase 6, so Phase 5 debug-switches nodes via `SetActiveNode(kind)` (the eventual jump target). `SetActiveNode` clears **transient** node state (exterior resource nodes, creatures, threats, trade availability) and seeds the new node via a `NodeRules.SeedNode` dispatcher each node task extends; **persistent** ship state (rooms, power, pressure, vitals, stores, hotbar+cargo items) and **ship hazards** (fire, hull breaches) survive node changes. (Decided 2026-06-13.)
- **Ship-as-component / map host (Phase 5):** the ship interior becomes a reusable `Ship.tscn` placed into per-node `Map` scenes; `Main` becomes a `MapHost` swapping the active map on `NodeKind`. Exterior maps (Planetside, Wreck, Trading station) place the Ship beside an exterior grid reached by airlock; defense/maintenance are ship-side node states (no separate map). Spawners/interaction/HUDs/camera are already grid-decoupled, so the extraction is regression-only. (Decided 2026-06-13.)
- **Per-weapon ammo (Task 5.5):** weapons carry their own ammo counter and are powered via their room's breaker (vs a ship-wide pool) — faithful to GDD "load boxes into the weapon assemblies" and the power-juggling tension. `Ammo` item + `AmmoCrate` Workshop recipe + a 4th per-weapon `LoadItem` branch. (Decided 2026-06-13.)
- **Credits trading + docked station (Task 5.4):** trading posts use a `ShipStores.Credits` currency + a static `TradeRules` price catalog (`BuyItem`/`SellItem`), rather than item-for-item barter. The trade terminal lives on a `TradingStationMap` the player crosses to via the 5.2 airlock system (vs a ship-side console), so 5.4 depends on 5.2. (Decided 2026-06-13.)
- **Deterministic hazard/creature behavior (Phase 5):** reducers forbid RNG, so fire spreads to the lowest-index un-ignited adjacent floor cell and creatures chase the nearest in-range player (else patrol fixed waypoints). Predictable but DB-tunable; sufficient for the prototype, refine in playtesting. (Decided 2026-06-13.)
