# Godot Client Conventions

Applies to everything under `client/`. All game code is C# — never create `.gd` files.

## Never edit

- `Db/` — generated SpacetimeDB bindings (regenerate with `spacetime generate`)
- `addons/` — external libraries, including the symlinked validation runtime

## Scene-first development (mandatory)

Build this game the way a human Godot developer would: **scenes and resources are the primary authoring surface, code is for behavior.** `.tscn` and `.tres` files are plain text — author and edit them directly.

### Rules

1. **If a node exists for the whole lifetime of its parent, it is declared in the `.tscn`.** `_Ready()` must not assemble static structure. A camera, a UI panel, a grid container, a background — scene, not code.
2. **Code only instantiates what is genuinely dynamic** (spawned per-entity, data-driven counts) — and even then it instantiates a `PackedScene` provided via `[Export]`, never assembles raw nodes with `new Node2D()` + `AddChild` chains. If a dynamic thing has visual structure, that structure is its own scene.
3. **Tunable values live in the scene/resource, not in C# literals.** The test: "could a designer tweak this in the editor without recompiling?" Positions, sizes, colors, zoom, smoothing speeds, speeds — `[Export]` properties with values set in the `.tscn`/`.tres`.
4. **Game data lives in `.tres` resources** (e.g. `game/Ship/CorvetteHull.tres` is a `HullTemplate` resource). New content of an existing type = new `.tres`, not new code.
5. **Acceptable procedural work:** painting `TileMapLayer` cells from data, `_Draw()` overlays, spawning entities from server state. The *nodes* doing that work (the `TileMapLayer`, the container) still belong in the scene, and their `TileSet` belongs in a `.tres`.

Anti-patterns currently in the codebase — do not copy them; refactor toward scenes when touching these files: `game/Ship/RoomTypeRegistry.cs` still `GD.Load`s the seven `RoomTypes/*.tres` files via string paths in `LoadAll()` (should be an `[Export]` array of `RoomType` resources wired in the scene). (`game/Main.cs` was refactored to exports + scene-declared nodes in `Main.tscn` — note that `ShipGrid.RoomTypeRegistry` is still assigned in `Main.OnReady()` because Node exports don't bind across scene-instance boundaries.)

### Node and resource references

Node binding uses **Chickensoft AutoInject** (already referenced in `Nomad.csproj` together with `Chickensoft.Introspection` and `Chickensoft.GodotNodeInterfaces`):

```csharp
using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoNode))]
public partial class Player : CharacterBody2D
{
    public override void _Notification(int what) => this.Notify(what);

    [Node]                  // binds the scene-unique node %Sprite (property name)
    public IColorRect Sprite { get; set; } = default!;

    [Node("%HealthLabel")]  // explicit unique-node path; plain paths like "Body/Shape" also work
    public ILabel HealthLabel { get; set; } = default!;
}
```

- The class needs all three pieces: `[Meta(typeof(IAutoNode))]`, the `_Notification` → `this.Notify(what)` forwarder, and `[Node]` properties. Bound children should be **scene-unique** (`unique_name_in_owner = true` in the `.tscn`). Unique-node syntax is `%Name` — there is no `#` syntax.
- Prefer the `GodotNodeInterfaces` interface types (`INode2D`, `IColorRect`, …) for bound properties — they make scene scripts unit-testable.
- AutoInject also provides tree-scoped DI (`IProvider`/`IDependent` with `[Dependency]`) — prefer it over singletons or passing references through long constructor chains when descendants need something an ancestor owns.
- References to *other* scenes/resources: `[Export] PackedScene` / `[Export] SomeResource` properties, wired up in the `.tscn`. **Never `GD.Load("res://...")` string paths in gameplay code** — string paths break silently on rename.
- Static signal wiring can live in the scene (`[connection]` sections); cross-layer notifications use C# events on services.

### Authoring `.tscn` files as text

Godot 4 text format, `format=3`:

```
[gd_scene load_steps=3 format=3]

[ext_resource type="Script" path="res://game/Player/Player.cs" id="1"]
[ext_resource type="PackedScene" path="res://game/SomeChild/SomeChild.tscn" id="2"]

[sub_resource type="RectangleShape2D" id="RectangleShape2D_body"]
size = Vector2(20, 20)

[node name="Player" type="CharacterBody2D"]
script = ExtResource("1")
MoveSpeed = 200.0

[node name="Sprite" type="ColorRect" parent="."]
unique_name_in_owner = true
offset_left = -10.0
offset_top = -10.0
offset_right = 10.0
offset_bottom = 10.0

[node name="Collision" type="CollisionShape2D" parent="."]
shape = SubResource("RectangleShape2D_body")

[node name="Child" parent="." instance=ExtResource("2")]
```

- `load_steps` = number of ext_resources + sub_resources + 1.
- **Never invent `uid="uid://..."` values.** Omit the `uid` attribute on files you create; Godot assigns one on next editor open. Preserve existing uids when editing.
- `[Export]` properties are set on the node entry by their C# property name (as in `MoveSpeed` above).
- Scene errors only surface at runtime: after structural `.tscn` changes, `dotnet build` and then actually load the scene (validation run or `run-game` skill) before calling it done.

## GUIDE input system

Input goes through the G.U.I.D.E addon (`addons/guide/`), never raw `Input` calls in gameplay code. C# wrappers live in `game/Guide/`.

| Type | Purpose |
|---|---|
| `GuideActionBinding` | Wraps a `GUIDEAction` resource — use typed properties (`ValueAxis2D`, `ValueBool`, `ValueAxis1D`), never raw `.Get()` |
| `GuideMappingContextBinding` | Wraps a `GUIDEMappingContext` (group of action mappings) |
| `GuideService` | Context stack, input mode (KBM/Controller), `/root/GUIDE` singleton |
| `InputContext` / `InputModeContext` | Resources holding global + per-mode KBM/Controller context pairs |

```csharp
[Export] public GuideActionBinding MoveAction { get; set; } = null!;
var direction = MoveAction.ValueAxis2D;   // ✅ typed
// ❌ never: (Vector2)MoveAction.Get("value_axis_2d")
```

New actions: author `.tres` GUIDEAction + mapping-context resources (like `game/Player/_Input/`), group into an `InputModeContext`, push via `GuideService`.

**Validation coexistence:** the validation runtime's `press_action`/`release_action` ops drive Godot `InputMap` actions, not GUIDE. Any action a scenario needs must also be registered in `InputMap` — see `EnsureInputActions()` in `bootstrap/AppRoot.cs` and keep it in sync when adding actions.

## Service layer (`_Service/` vs `_Scene/`)

- `_Service/` classes are **plain C#** (no `Node` inheritance): game state, rules, decisions. They may use Godot math types (`Vector2`, `Mathf`), `DbConnection` tables/reducers, other services, and expose C# events. They must **not** touch visual/UI node types, `SceneTree`, `GetTree()`, `ToSignal()`, or `GD.Load`.
- `_Scene/` classes (and scene scripts generally) are thin Godot shells: read input, delegate to services, react to service/DbConnection events for visuals, own timers and Godot async.

## Namespaces

Feature-based, not folder-based: `Nomad.Game.Player`, `Nomad.Game.Map`, `Nomad.Validation.HarnessControllers`.
