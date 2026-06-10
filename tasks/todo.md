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
