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
public class PlayerMovementService(DbConnection server, uint entityId)
{
    public event Action<Vector2>? PositionUpdated;

    public void UpdatePosition(Vector2 position)
    {
        server.Reducers.MoveEntity(entityId, position.X, position.Y);
    }
}

// _Scene/ class — thin Godot shell
public partial class Player : CharacterBody2D
{
    private PlayerMovementService _movement = null!;

    public override void _Ready()
    {
        _movement = new PlayerMovementService(Server, _entityId);
        _movement.PositionUpdated += pos => GlobalPosition = pos;
    }
}
```

## Namespaces

Classes should always be in namespaces based on their feature, not specifically their subfolder. Examples: `Nomad.Game.Player`, `Nomad.Game.Map`.
