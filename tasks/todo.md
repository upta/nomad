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

# Task 0.2: SpacetimeDB Server Scaffold

## Convention Migration (SDK 2.4.x)
- Lifecycle hooks: `OnConnect` / `OnDisconnect` (WITH "On" prefix per current SDK)
- Attributes: `[PrimaryKey]`, `[AutoInc]`, `[Reducer]` (shorthand, no `SpacetimeDB.` prefix)
- Tables nested inside `public static partial class Module`, split across files via `partial`
- Module path: `server/src/` (not `spacetimedb/`)
- `.csproj`: `Microsoft.NET.Sdk` + `wasi-wasm` + `SpacetimeDB.Runtime 2.4.*`

## Subtask 0.2.1: Scaffold + reorganize
- [x] Run `spacetime init` into `server/` (done)
- [ ] Delete generated artifacts: `.cursor/`, `.windsurfrules`
- [ ] Move module from `server/spacetimedb/` → `server/src/`
- [ ] Rename `StdbModule.csproj` → `NomadServer.csproj`
- [ ] Update `spacetime.json` module-path to `./src`
- [ ] Add `server/.gitignore` for wasm artifacts

## Subtask 0.2.2: Types + GlobalUsings
- [ ] Create `server/src/GlobalUsings.cs` — `global using SpacetimeDB;`
- [ ] Create `server/src/Types/EntityType.cs` — `enum EntityType : uint { Player }`

## Subtask 0.2.3: Table definitions
- [ ] Create `server/src/Tables/Player.cs` — Identity PK, IsConnected, PlayerEntityId (int)
- [ ] Create `server/src/Tables/Entity.cs` — EntityId PK+AutoInc (int), EntityTypeId (uint), PositionX/Y (float)
- [ ] Create `server/src/Tables/EntityOwnership.cs` — EntityId PK (int), Owner (Identity) with BTree index, Public=false

## Subtask 0.2.4: Reducers
- [ ] Create `server/src/Reducers/Connect.cs` — OnConnect: upsert player + spawn player entity + create ownership
- [ ] Create `server/src/Reducers/Disconnect.cs` — OnDisconnect: mark player disconnected + deactivate owned entities
- [ ] Create `server/src/Reducers/MoveEntity.cs` — ownership check + position update with `with` expression

## Subtask 0.2.5: Build + publish + docs
- [ ] `spacetime build` succeeds
- [ ] `spacetime publish nomad --module-path ./server/src` succeeds
- [ ] Generate client bindings
- [ ] Update `server.instructions.md` for SDK 2.4.x conventions
- [ ] Update `server-reducers.instructions.md` for SDK 2.4.x conventions
