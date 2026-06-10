---
applyTo: "client/game/**"
---

# Godot C# Client Instructions

These instructions apply to files under `client/game/**`.

## Important

- The `Db/` folder is generated — don't modify any files in there
- Godot generates UID values, don't try to create them from scratch
- The `addons/` folder contains external libraries, don't modify it
- Avoid `GD.Load` using resource paths (e.g. `"res://App/_Input/Actions/UICancel.tres"`). Instead, provide `[Export]` properties
- Static nodes should be created inside `.tscn` files whenever possible, only dynamic nodes should be created via code

## Scene Classes

- Child nodes that need to be referenced in code should ALWAYS be scene unique nodes
- References should be bound using the `[Node("#ChildNode")]` attribute or `GetNode<T>()`
- `[Export]` should be used to get references to other scenes, not string paths

## GUIDE Input System

Input is handled by the G.U.I.D.E addon (`client/addons/guide/`), not raw Godot `Input` calls. C# wrappers live in `client/game/Guide/`.

### Key types

| Type | Purpose |
|------|---------|
| `GuideActionBinding` | Wraps a `GUIDEAction` resource. Use typed properties, never raw `.Get()` |
| `GuideMappingContextBinding` | Wraps a `GUIDEMappingContext` (group of action mappings) |
| `GuideService` | Manages the context stack, input mode (KBM/Controller), and the `/root/GUIDE` singleton |
| `InputContext` | Resource holding global KBM/Controller contexts + mode-switch actions |
| `InputModeContext` | Resource holding a Controller/KBM context pair for a gameplay/UI mode |

### Reading input (the right way)

```csharp
// ✅ Use typed properties on GuideActionBinding
[Export] public GuideActionBinding MoveAction { get; set; } = null!;

var direction = MoveAction.ValueAxis2D;   // Vector2
var held = MoveAction.ValueBool;          // bool
var axis1d = MoveAction.ValueAxis1D;     // float
```

```csharp
// ❌ Never use raw .Get() with string keys — bypasses type safety
var direction = (Vector2)MoveAction.Get("value_axis_2d");  // BAD
```

### Setting up GUIDE contexts

1. Define actions as `.tres` GUIDEAction resources (e.g., `CharacterMove.tres`)
2. Define mapping contexts that bind inputs → actions (e.g., `CharacterContextKbm.tres`)
3. Group contexts into an `InputModeContext` with KBM + Controller pairs
4. Create an `InputContext` resource with global contexts + mode-switch actions
5. In `AppRoot._Ready()`: `new GuideService(inputContext)`, `Initialize()`, then `PushContext(gameplayMode)` after data is ready

### GUIDE + InputMap coexistence

GUIDE actions replace raw `Input.GetVector()` in gameplay code, but `InputMap` actions must still be registered for the validation runtime's `press_action` / `release_action` operations to work in test mode. Register them alongside GUIDE initialization:

```csharp
private static void EnsureInputActions()
{
    foreach (var (actionName, key) in ActionKeys)
    {
        if (!InputMap.HasAction(actionName))
            InputMap.AddAction(actionName);
        var inputEvent = new InputEventKey { PhysicalKeycode = key };
        if (!InputMap.ActionHasEvent(actionName, inputEvent))
            InputMap.ActionAddEvent(actionName, inputEvent);
    }
}
```

## Service Layer Rules

`_Service/` classes are the core game logic layer. They separate business rules and state management from Godot presentation code.

### What _Service/ classes ARE
- Plain C# classes (no Godot node inheritance)
- They own game state, rules, validation, and decision-making
- They may call SpacetimeDB reducers (server commands)
- They may read from `DbConnection.Db` tables to make decisions
- They expose C# events to notify the presentation layer of state changes
- They receive dependencies through constructor parameters

### What _Service/ classes MUST NOT reference
- Any Godot visual node types: `Sprite2D`, `AnimatedSprite2D`, `MeshInstance3D`, etc.
- Any Godot UI node types: `Control`, `Label`, `TextureRect`, `CanvasLayer`, etc.
- `SceneTree`, `SceneTreeTimer`, `GetTree()`
- `ToSignal()` or any Godot signal/await patterns
- `GD.Load<>()` or Godot resource loading

### What _Service/ classes MAY reference
- Godot math types: `Vector2`, `Vector2I`, `Mathf`, `Rect2`, etc.
- `DbConnection` and SpacetimeDB types
- `Variant` (Godot interop type) when needed for data passing
- Other `_Service/` classes
- `_Model/` types
- Standard C# types and patterns

### How _Scene/ classes interact with _Service/ classes
- `_Scene/` classes are thin Godot shells that wire services to visual nodes
- They delegate decisions to services
- They react to service events for visual updates
- They keep timer management and Godot-specific async patterns (`ToSignal`, `await`)
- They may subscribe to `DbConnection` events directly for reactive visual updates

### Example Pattern

```csharp
// _Service/ class — pure logic
public class MovementNetworkSync(DbConnection server, float sendInterval = 0.05f)
{
    private Vector2 _lastSentPosition;

    public void Initialize(Vector2 position) => _lastSentPosition = position;

    public void Update(int entityId, Vector2 position, Vector2 velocity, float rotation, double delta)
    {
        var hasMoved = !position.IsEqualApprox(_lastSentPosition);
        if (hasMoved)
        {
            server.Reducers.MoveEntity(
                entityId,
                new DbVector2(position.X, position.Y),
                new DbVector2(velocity.X, velocity.Y),
                rotation,
                Time.GetTicksMsec() / 1000.0
            );
            _lastSentPosition = position;
        }
    }
}

// _Scene/ class — thin Godot shell
public partial class Player : CharacterBody2D
{
    private MovementNetworkSync _networkSync = null!;

    [Export] public GuideActionBinding MoveAction { get; set; } = null!;

    public override void _PhysicsProcess(double delta)
    {
        var direction = MoveAction.ValueAxis2D;
        Velocity = direction * MoveSpeed;
        MoveAndSlide();
        _networkSync.Update(_entityId, GlobalPosition, Velocity, 0f, delta);
    }
}
```

## Namespaces

Classes should always be in namespaces based on their feature, not specifically their subfolder. Examples: `Nomad.Game.Player`, `Nomad.Game.Map`.
