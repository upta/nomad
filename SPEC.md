# Spec: Nomad — Phase 0 (Multiplayer Foundation)

## Objective

Establish the multiplayer foundation for Nomad. By the end of Phase 0, multiple Godot clients can connect to a SpacetimeDB server, players move on a tile-based grid with a following camera, and the project is restructured from a bare prototype into a proper `client/` + `server/` layout, with PascalCase naming throughout.

**Target user:** The development team (us). This phase delivers the base layer that all subsequent gameplay features build on.

**Success looks like:** Two Godot clients connected to the same SpacetimeDB instance, each controlling a player character with WASD, seeing each other move in real time, camera following their own character.

## Tech Stack

| Component | Technology | Version |
|-----------|-----------|---------|
| Game client | Godot 4.x Mono (C#) | 4.3+ |
| Client runtime | .NET 8.0 | 8.0.x |
| Server | SpacetimeDB | 2.4.1 |
| Server language | C# (.NET 8.0) | 8.0.x |
| Server WASM target | wasi-wasm | — |
| Client SDK | `SpacetimeDB.ClientSDK.Godot` | latest (2.4.1 compat) |
| Validation | agentic-godot-validation kit | submodule |
| Formatting | csharpier | latest |

## Commands

```powershell
# Build client (from client/ directory)
dotnet build

# Format client (from client/ directory)
dotnet csharpier format .

# Build server (from server/ directory)
spacetime build

# Format server (from server/ directory)
dotnet csharpier format .

# Start SpacetimeDB locally
spacetime start

# Publish server module to local SpacetimeDB
spacetime publish nomad --module-path ./server/src

# Clear and republish (schema changes)
spacetime publish nomad --clear-database -y --module-path ./server/src

# Generate client bindings
spacetime generate --lang csharp --out-dir ./client/Db --module-path ./server/src

# Run a validation scenario
./tools/run_scenario.ps1 -Scenario client/validation/scenarios/<name>.json -GodotExe <path>

# Run all validation scenarios
./tools/run_all_scenarios.ps1 -GodotExe <path>

# Note: Scripts auto-detect project.godot — tries repo root first, then client/ subdirectory.
# Artifacts are written to the project directory (e.g., client/artifacts/).
```

## Project Structure (Phase 0 target)

```
client/                              ← Godot 4.x C# project (moved from src/)
  project.godot                      ← config/name="Nomad"
  Nomad.csproj                       ← renamed from MyPrototype.csproj
  Nomad.sln                          ← renamed from MyPrototype.sln
  Db/                                ← generated SpacetimeDB client bindings (auto)
  bootstrap/
    AppRoot.cs                       ← entry point (test-mode routing)
    AppRoot.tscn
  game/
    Main.cs                          ← game scene (placeholder → tile grid host)
    Main.tscn
    Player/
      Player.cs                      ← CharacterBody2D + WASD movement
      Player.tscn
    Map/
      ShipGrid.cs                    ← TileMapLayer-based ship grid
  validation/
    harnesses/
      MovementHarness.tscn           ← harness for player movement tests
    scenarios/
      player_moves_right.json        ← movement validation scenario
      player_moves_all_directions.json
    scripts/
      harness_controllers/
        MovementHarnessController.cs ← exposes player position for assertions
  addons/
    agentic_godot_validation/        ← symlink → submodule (unchanged)

server/
  spacetime.json                     ← module path + generate config
  spacetime.local.json               ← local deployment config
  NomadServer.csproj                 ← server .NET project (wasi-wasm target)
  src/
    GlobalUsings.cs                  ← global using SpacetimeDB;
    Tables/
      Player.cs                      ← partial struct Player with [SpacetimeDB.Table]
      Entity.cs                      ← partial struct Entity (position + type)
      EntityOwnership.cs             ← partial struct EntityOwnership (entity → owner)
    Reducers/
      Connect.cs                     ← ClientConnected lifecycle + spawn
      Disconnect.cs                  ← ClientDisconnected lifecycle + cleanup
      MoveEntity.cs                  ← client-authoritative entity movement
    Types/
      EntityType.cs                  ← enum EntityType { Player, ... }

.github/
  copilot-instructions.md            ← project-wide Copilot rules
  instructions/                      ← path-scoped instructions
    server.instructions.md           ← server/** SpacetimeDB rules
    server-reducers.instructions.md  ← server/src/Reducers/** rules
    client-services.instructions.md  ← client/game/** service layer rules
    validation-runtime.instructions.md
    validation-assets.instructions.md

docs/
  game-design-document.md            ← GDD (existing)
tasks/
  plan.md                            ← full implementation plan (existing)
SPEC.md                              ← this file
```

## Code Style

### Client (Godot C#)
```csharp
// File-scoped namespaces, PascalCase public, _camelCase private
namespace Nomad.Game.Player;

public partial class Player : CharacterBody2D
{
    [Export] private float _moveSpeed = 200f;

    private Vector2 _moveInput;

    public override void _PhysicsProcess(double delta)
    {
        _moveInput = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        Velocity = _moveInput * _moveSpeed;
        MoveAndSlide();
    }
}
```

### Server (SpacetimeDB C#)
```csharp
// Tables are partial struct with [SpacetimeDB.Table]
[SpacetimeDB.Table(Accessor = "Players", Public = true)]
public partial struct Player
{
    [SpacetimeDB.PrimaryKey]
    public Identity Identity;
    public bool IsConnected;
    public ulong PlayerEntityId;
}

// Reducers are static methods on partial class Module
public static partial class Module
{
    [SpacetimeDB.Reducer(ReducerKind.ClientConnected)]
    public static void ClientConnected(ReducerContext ctx)
    {
        // Lifecycle hooks: NO "On" prefix (OnClientConnected → STDB0010 error)
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is Player player)
        {
            ctx.Db.Players.Identity.Update(player with { IsConnected = true });
        }
        else
        {
            ctx.Db.Players.Insert(new Player
            {
                Identity = ctx.Sender,
                IsConnected = true,
                PlayerEntityId = 0,
            });
        }
    }

    [SpacetimeDB.Reducer]
    public static void MoveEntity(ReducerContext ctx, uint entityId, float x, float y)
    {
        // Ownership check + update with `with` expression
        var ownership = ctx.Db.EntityOwnership.EntityId.Find(entityId)
            ?? throw new Exception("Entity not owned");

        if (ownership.Owner != ctx.Sender)
            throw new Exception("Not authorized");

        var entity = ctx.Db.Entities.EntityId.Find(entityId)!.Value;
        ctx.Db.Entities.EntityId.Update(entity with { PositionX = x, PositionY = y });
    }
}
```

Key conventions:
- **All filenames PascalCase** — `.cs` and `.tscn` both match class name (e.g., `AppRoot.cs` + `AppRoot.tscn`)
- File-scoped namespaces, root namespace `Nomad`
- `PascalCase` for public members, `_camelCase` for private fields
- Prefer `var` for locals when type is obvious
- Godot node references via `[Export]` or `GetNode<T>()` in `_Ready()`
- SpacetimeDB tables: `partial struct` with `[SpacetimeDB.Table(Accessor = "...", Public = true)]`
- SpacetimeDB reducers: `public static partial class Module` across files in `Reducers/`
- Lifecycle reducers: NO `On` prefix — `ClientConnected`, not `OnClientConnected`
- Update pattern: `Find()` → `Update(row with { ... })` (never partial-update structs)
- Validation harness controllers expose semantic state via `GetObservedState()` returning `Godot.Collections.Dictionary`

## Testing Strategy

**Framework:** agentic-godot-validation kit (scenario-based, headless Godot execution)

**Test levels:**
- **Validation scenarios** (JSON contracts) for gameplay behavior: movement, camera, server sync
- Every player-visible behavior change gets a scenario
- Server-side: SpacetimeDB reducer correctness verified via end-to-end client scenarios

**Phase 0 test requirements:**
- `player_moves_right.json` — WASD right moves player, position.x increases
- `player_moves_all_directions.json` — all four cardinal directions produce correct position deltas
- (Stretch) `two_players_see_each_other.json` — two clients connected, each sees the other's player move

**Coverage expectation:** Every gameplay task in Phase 0 includes ≥1 validation scenario.

## Boundaries

### Always do:
- Build + format after each task: `dotnet build`, `dotnet csharpier format .`, `spacetime build`
- Include validation scenarios for every gameplay behavior change
- Run existing scenarios after changes to catch regressions
- Commit with conventional commit messages (`feat:`, `fix:`, `chore:`)
- Reference untrailed and SpacetimeDB docs for server patterns
- Use `with` expressions for SpacetimeDB updates (never partial struct updates)
- Check `.github/instructions/` for layer-specific rules (server, client services, reducers, validation)

### Ask first:
- Changes to `symlink-config.txt` or symlink structure
- Adding NuGet packages beyond `SpacetimeDB.ClientSDK.Godot`
- Modifications to `docs/game-design-document.md`
- Changes to validation framework submodule (`client/addons/agentic_godot_validation/`)
- Schema changes to SpacetimeDB tables after initial scaffold
- Adding new table columns that would require data migration

### Never do:
- Create `.gd` GDScript files — all game code is C#
- Commit secrets, credentials, or API keys
- Delete or disable existing validation scenarios without replacement
- Edit files under `client/addons/agentic_godot_validation/` (symlinked submodule)
- Edit files under `client/Db/` (generated SpacetimeDB bindings)
- Use `[On]` prefix on SpacetimeDB lifecycle reducers
- Use partial struct updates in SpacetimeDB (nulls fields)
- Use async/await inside SpacetimeDB reducers

## Success Criteria

Each task is complete when:

### Task 0.1: Project Restructuring
- [x] `client/` directory exists with Godot project (moved from `src/`)
- [x] `project.godot` has `config/name="Nomad"` and `project/assembly_name="Nomad"`
- [x] `.csproj` is `Nomad.csproj` with `<RootNamespace>Nomad</RootNamespace>`
- [x] `.sln` is `Nomad.sln` referencing `Nomad.csproj`
- [x] All existing files renamed to PascalCase: `AppRoot.cs/tscn`, `Main.cs/tscn`
- [x] All C# files use `namespace Nomad.*` (file-scoped)
- [x] `symlink-config.txt` updated for `client/` paths
- [x] `dotnet build` succeeds from `client/` (after symlinks re-run)
- [x] All existing validation scenarios still pass

### Task 0.2: SpacetimeDB Server Scaffold
- [x] `server/` directory with `spacetime.json`, `NomadServer.csproj`
- [x] `GlobalUsings.cs` with `global using SpacetimeDB;`
- [x] `Tables/Player.cs` — `partial struct` with `Identity`, `IsConnected`, `PlayerEntityId`
- [x] `Tables/Entity.cs` — `partial struct` with `EntityId` (auto-increment), `EntityTypeId`, `Position`, `Velocity`, `Rotation`, `Active`
- [x] `Tables/EntityOwnership.cs` — `partial struct` with `EntityId`, `Owner`
- [x] `Types/DbVector2.cs` — `partial struct` with `X`, `Y`
- [x] `Views/ActiveEntities.cs` — view filtering `Entities.Active == true`
- [x] `Reducers/Connect.cs` — `ClientConnected` lifecycle + spawn player entity
- [x] `Reducers/MoveEntity.cs` — client-authoritative `MoveEntity` with ownership check (6-param)
- [x] `Reducers/Disconnect.cs` — deactivate entities, zero velocity
- [x] `spacetime build` succeeds
- [x] `spacetime publish nomad --module-path ./server/src` succeeds

### Task 0.3: SpacetimeDB Client Integration
- [x] `SpacetimeDB.ClientSDK` NuGet package added to `Nomad.csproj`
- [x] Client connection manager (DbManager) connects to local SpacetimeDB using builder pattern
- [x] Client subscribes to Players, Entities, ActiveEntities on connect
- [x] Client calls `MoveEntity` reducer with position updates (MovementNetworkSync)
- [x] `client/Db/` generated binding directory is `.gitignore`d
- [x] End-to-end verified: client connects → Player row created → client sees update

### Task 0.4: Tile Grid + Camera
- [x] Ship grid scene with _Draw() rendering a basic ship floor plan (8x6 grid, 4 rooms)
- [x] Camera2D follows local player character
- [x] Flat 2D vector style per GDD §7.1 (minimal for now)

### Task 0.5: Player Character + Movement
- [x] `Player` scene (CharacterBody2D) with GUIDE-based WASD movement
- [x] Client-authoritative position sent to SpacetimeDB via `MoveEntity` reducer (throttled 50ms)
- [x] Remote players rendered with SnapshotInterpolator (lerp) + EntityMover
- [x] Implements Move verb per GDD §5
- [x] Validation: `player_moves_right.json` passes (393px in 60 frames)
- [x] Validation: `player_moves_all_directions.json` passes (right + left assertions)

### Checkpoint: Foundation
- [x] Two clients connect, move, and see each other
- [x] Project is named Nomad, structured as `client/` + `server/`
- [x] All validation scenarios pass
- [x] `dotnet build` and `spacetime build` both succeed

## Open Questions

1. **SpacetimeDB Godot SDK API surface:** The official `SpacetimeDB.ClientSDK.Godot` package is newer than untrailed's generic C# approach. We need to read the official docs to understand:
   - How to connect from Godot (builder pattern? singleton?)
   - How to subscribe to tables and receive updates in Godot's main loop
   - How to call reducers from Godot
   - → **Action:** Read [SpacetimeDB Godot setup docs](https://docs.spacetimedb.com/tutorials/godot/setup/) during Task 0.3
   - → **Action:** Check [spacetimedb.com/llms.txt](https://spacetimedb.com/llms.txt) for latest API reference

2. **Player position update frequency:** What tick rate for sending position to server? Every frame? Every N physics frames? Configurable constant?

3. **Remote player interpolation:** Simple (snap to server position) or smooth (lerp toward server position)? Start simple, refine later.

4. **Ship grid layout:** What shape/size for the initial Corvette hull grid? 7 rooms as defined in plan.md — need dimensions. Start with a placeholder rectangular grid.
