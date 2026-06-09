# Implementation Plan: Nomad

## Overview

Nomad is a cooperative, top-down 2D sci-fi survival game for up to 8 players. The crew pilots a modular gravity ship fleeing an encroaching galactic cataclysm (The Stellar Night). The core experience blends localized crew-management tension with high-stakes route navigation — played entirely in real-time co-op from a flat, top-down perspective. No direct character-vs-monster combat.

The current codebase is a bare Godot 4.x C# prototype with bootstrap routing (`AppRoot`) and a placeholder `Main` scene. No gameplay features exist yet.

## Architecture Decisions

- **Godot 4.x + C# client** — Godot renders the game, collects input, plays audio. All game logic lives in SpacetimeDB or is strictly client-side prediction/rendering.
- **SpacetimeDB server-authoritative** — The SpacetimeDB module owns all shared game state. Clients subscribe to tables and call reducers. Reducers are transactional and deterministic (no filesystem, network, timers, or random inside reducers). Reference: [untrailed](https://github.com/upta/untrailed) for working SpacetimeDB + Godot integration patterns.
- **Client-authoritative player position** — Each client owns their player's position (sends to server). Other entities are interpolated/extrapolated client-side. Server validates for anti-cheat.
- **Top-down 2D, tile-based grid** — Flat vector style inspired by RimWorld (§7.1). Characters are single-piece sprites (no skeletal animation). Grid uses Godot TileMap.
- **Data-driven rooms** — Hull templates and room types defined as `.tres` resources with string IDs that SpacetimeDB references for runtime state (powered, pressurized, etc.).
- **Room-centric power model** — Power/atmosphere calculated per-room, not per-tile (§6.3). Simple binary model initially (powered/unpowered), expandable later. Each room has an interactable breaker switch.
- **Walk-up modal UI** — Complex systems (Star Chart, Power Router, Fabricator) accessed by walking a character up to a physical terminal and interacting. Opens a Godot Control-based modal dialog. No persistent floating HUD menus (§7.2).
- **Pausability** — Real-time gameplay with architectural support for pausing (Stellar Night clock stoppable, reducer gating). The hybrid model means the game is real-time by default but the system can freeze state cleanly.
- **Validation-first** — Every gameplay change includes validation scenarios. Harness controllers expose semantic game state. Validation scenarios run against the SpacetimeDB server + Godot client together.

## Project Structure

```
client/                 → Godot 4.x C# project (moved from current src/)
  game/                 → Gameplay scenes and C# scripts
  bootstrap/            → App entry point with test-mode routing
  validation/           → Harnesses, scenarios, harness controllers
  addons/               → Symlinked addons (agentic_godot_validation)
server/                 → SpacetimeDB module
  src/
    Tables/             → Table definitions (schema)
    Reducers/           → Reducer functions (game logic)
    Types/              → Shared types used by server
    Views/              → Query views for client subscriptions
    Lib.cs              → Module entry point
  spacetime.json        → Local SpacetimeDB config
  spacetime.local.json  → Local deployment config
  spacetime.prod.json   → Production deployment config
shared/                 → Types and constants shared between client and server (if needed beyond generated bindings)
docs/                   → GDD, specs, ADRs
tasks/                  → Plan and task tracking
```

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
    │       │       └── Terminal Interaction Modals
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

- [ ] **Task 0.2: SpacetimeDB server scaffold** — Create `server/` directory with SpacetimeDB module structure. Set up `spacetime.json`, `.csproj`, `Lib.cs` entry point. Define initial tables (Player, GameSession). Implement connection handshake reducer. Build with `spacetime build`. Reference: untrailed `server/` structure. **Scope: M**

- [ ] **Task 0.3: SpacetimeDB client integration** — Add SpacetimeDB client SDK to Godot project. Create client-side connection manager that connects to local SpacetimeDB, subscribes to tables, and calls reducers. Verify end-to-end: client connects → reducer called → table updated → client sees update. Reference: untrailed client integration. **Scope: M**

- [ ] **Task 0.4: Tile-based grid map + 2D camera** — Create a TileMap-based grid for the ship interior. Add a top-down Camera2D. Set up rendering layers for the flat 2D vector style per GDD §7.1. Camera should follow the local player. **Scope: S**

- [ ] **Task 0.5: Player character + WASD movement** — Create a Player scene (CharacterBody2D) with WASD movement. Client-authoritative position sent to SpacetimeDB. Remote players interpolated. Crib from untrailed movement patterns. Implements the Move verb (§5). Add movement validation scenario. **Scope: M**

**Checkpoint: Foundation** — Multiple clients connect to SpacetimeDB, players move on a grid, camera follows, project is named Nomad.

### Phase 1: Ship Architecture

- [ ] **Task 1.1: Hull template data model** — Define `.tres` resource format for hull templates: grid width/height, armor rating, and a list of fixed room slots (each with position, width, height — but no type yet). Create one reference hull ("Corvette", 7 rooms). Each hull and room type has a string ID for SpacetimeDB reference. FTL-style: rooms are fixed positions on the hull, crew assigns types to them in the lobby. **Scope: M**

- [ ] **Task 1.2: Room system** — Define 7 room types as `.tres` resources: Reactor, Bridge, Cloning Bay, Hydroponics, Workshop, Kitchen, Cargo Bay. Each has `room_id`, `label`, `power_draw`, `terminal_type` (star_chart, power_router, fabricator, cloning, info), `tileset_ref`. SpacetimeDB stores runtime assignment: `slot_index → room_type_id` + live state (powered, pressurized, breaker, health). Client renders rooms by looking up the assigned type and applying its tileset. **Scope: L**

- [ ] **Task 1.3: Power grid + breaker switches** — Simple binary power model: Reactor generates power, each room consumes 1 unit. If total demand ≤ reactor output, all rooms powered. Breakers can be toggled to cut power to individual rooms. Breaker state lives in SpacetimeDB. Players interact with breaker objects to toggle (Toggle verb per §5). Walk-up modal shows room power status. **Scope: M**

- [ ] **Task 1.4: Room pressurization** — Rooms are pressurized or unpressurized. Hull breaches cause depressurization. SpacetimeDB owns pressurization state. Foundation for oxygen tether (Phase 2). Render depressurized rooms visually differently. **Scope: M**

**Checkpoint: Ship** — Hull loads, 6-8 rooms exist with breakers, rooms can be powered/unpowered, pressurization state tracked.

### Phase 2: Character Systems

- [ ] **Task 2.1: Character health + environmental damage** — Characters have a health meter. Take damage from suffocation, starvation, fire, hostile creatures (§6.4). SpacetimeDB owns health state and processes damage. Client renders health bar. **Scope: M**

- [ ] **Task 2.2: Oxygen tether** — Personal oxygen tank depletes in vacuum/unpressurized areas, refills in pressurized rooms (§6.2). SpacetimeDB owns oxygen state. Spacesuits expand capacity at movement speed cost (deferred to later task). **Scope: M**

- [ ] **Task 2.3: Food/hunger meter** — Secondary metabolic meter that depletes slowly. Replenished by consuming meals or emergency rations (§6.2). Server-authoritative. **Scope: S**

- [ ] **Task 2.4: Death, ghost state, cloning bay** — Death is non-terminal. Deceased players enter Ghost Mode (visible to all players, can float anywhere, cannot interact with physical objects). Cloning Bay respawns at Biomass cost. If no power or biomass, stay ghosted (§6.4). SpacetimeDB manages ghost/clone state. **Scope: L**

**Checkpoint: Characters** — Characters have vitals (health, oxygen, hunger), can die, ghost, and respawn.

### Phase 3: Inventory & Logistics

- [ ] **Task 3.1: Fixed-size hotbar** — 4-slot hotbar (configurable), no hidden inventory (§6.1). Every item = 1 slot regardless of type. SpacetimeDB owns inventory state. Client renders hotbar UI and sends pickup/drop reducers. **Scope: M**

- [ ] **Task 3.2: Item types** — Define item types: raw ore, fuel deposits, biomass, ammunition crates, fuel cells, probes, meals, scrap, components. Items have visual representations for world rendering. Items are SpacetimeDB entities. **Scope: M**

- [ ] **Task 3.3: Item pickup/drop** — Players can pick up items from the world into hotbar slots and drop items onto the ground. SpacetimeDB validates every pickup/drop. Items are physical entities on the grid. **Scope: M**

- [ ] **Task 3.4: Load verb** — Interact with ship system structures to deposit items directly from hotbar: ammo→weapons, fuel→reactor, biomass→cloning. Walk-up modal shows system status and accepts deposits (§5). SpacetimeDB reducer processes the deposit. **Scope: M**

**Checkpoint: Inventory** — Full pickup→carry→deposit loop works multiplayer.

### Phase 4: Resource Economy

- [ ] **Task 4.1: Resource nodes** — Harvestable resource nodes: ore veins, wreckage debris, fuel deposits, biomass patches. Finite yields with visual depletion states. SpacetimeDB owns node state. **Scope: M**

- [ ] **Task 4.2: Harvest verb** — Stand near resource node, interact to extract raw resources into hotbar (§5). Extraction takes time. SpacetimeDB reducer processes harvest. **Scope: M**

- [ ] **Task 4.3: Workshop bench + Refine/Craft verb** — Interact with workshop benches to combine raw materials into consumables (§5). Walk-up modal shows available recipes and queue. SpacetimeDB reducer processes crafting. **Scope: M**

- [ ] **Task 4.4: Crafting recipes** — Start with 2 recipes: Fuel Cells (raw fuel + metal) and Meals (biomass). Ammo Crates and Probes deferred to later. Recipes require specific resources and crafting time. **Scope: S**

**Checkpoint: Economy** — Harvest→Refine→Load→Consume loop works end-to-end in multiplayer.

### Phase 5: Node Activities

- [ ] **Task 5.1: Node type framework** — Abstract Node Activity system supporting multiple node types. Each node has a scene, hazards, resources, and events (§4). SpacetimeDB manages which node is active and its state. **Scope: L**

- [ ] **Task 5.2: Planetside resource harvesting** — Ship lands on a planetary surface grid. Players leave airlocks to mine minerals, siphon fuel, gather biomass. Exterior grid with airlock transition mechanic. SpacetimeDB manages the exterior grid state. **Scope: L**

- [ ] **Task 5.3: Abandoned wreck salvage** — Ship docks with a derelict structure. Players explore corridors for rare components and supplies. Wreck layouts (hand-crafted or procedural). SpacetimeDB manages wreck state and loot. **Scope: L**

- [ ] **Task 5.4: Trading posts** — Safe-zone nodes with neutral factions or automated depots. Exchange excess materials for scarce resources, purchase packaged food/fuel, acquire room upgrades. Walk-up terminal interface. SpacetimeDB processes trades. **Scope: M**

**Deferred:** Automated defense systems (5.5) and core maintenance node (5.6) — post-prototype.

**Checkpoint: Nodes** — 3 distinct node types playable, each with unique activities.

### Phase 6: Navigation & The Stellar Night

- [ ] **Task 6.1: Star Chart map + fog of war** — Bridge terminal shows adjacent paths clearly; distant sectors obscured (§6.5). Walk-up modal on Bridge console. SpacetimeDB owns star chart state and fog of war per-session. **Scope: L**

- [ ] **Task 6.2: Node scanning + long-range probes** — Short-range scans reveal adjacent node details. Long-range probes (crafted) clear fog of war on distant nodes. SpacetimeDB processes scan/probe results. **Scope: M**

- [ ] **Task 6.3: Jump mechanic** — Select destination on Star Chart (any player can initiate), warm up jump drive, transit with effect, anchor at new node. SpacetimeDB manages jump state and anchors. **Scope: M**

- [ ] **Task 6.4: The Stellar Night threat** — Real-time encroaching darkness on Star Chart. Hybrid: real-time by default, architecturally pausable. Staying too long doubles power consumption and inflicts structural damage. SpacetimeDB owns the clock and threat state. Drives the Escape phase (§3). **Scope: L**

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
| Scope creep from GDD ideas | Medium | Deferred defense systems (5.5), maintenance (5.6), and specializations beyond 2-3 roles to post-prototype. |
| Godot C# + SpacetimeDB SDK compatibility | Low | Both are .NET-based. Untrailed proves the integration works. Pin SDK versions. |

## Resolved Design Decisions

- **Godot project location:** Move from `src/` to `client/` to align with untrailed server/client convention.
- **Hull & room template model:** Hull defines fixed room positions (FTL-style). Each room has a position and size on the grid. Players assign room types to each slot in the pre-run lobby. No placement constraints — any room type in any slot. SpacetimeDB stores `slot_index → room_type_id` + runtime state (powered, pressurized, breaker, health).
- **Room set:** 7 rooms for the Corvette hull: Reactor, Bridge, Cloning Bay, Hydroponics, Workshop, Kitchen, Cargo Bay.
- **Terminal assignment:** Every room type has a terminal (for consistency). Core terminals: Bridge→Star Chart, Reactor→Power Router, Workshop→Fabricator, Cloning Bay→Clone/Respawn. Others show room status info initially.
- **Jump voting:** No formal vote — any player can select the next destination and launch. Crew self-organizes.
- **Stellar Night tick rate:** Configurable constant (e.g. `TICK_RATE_HZ = 1`). Tunable during playtesting.
- **Room type `.tres` fields:** `room_id` (string), `label` (display name), `power_draw` (int), `terminal_type` (enum: star_chart, power_router, fabricator, cloning, info), `tileset_ref` (visual).
