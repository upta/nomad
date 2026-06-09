# Validation Assets Instructions

These instructions apply to files under `src/validation/**`.

## Writing Harness Controllers

Harness controllers are C# scripts attached to harness scenes that expose game state for scenario assertions.

```csharp
using Godot;
using System.Collections.Generic;

public partial class MyHarnessController : Node
{
    /// <summary>
    /// Returns observed state for scenario assertions.
    /// Called each frame during scenario execution.
    /// </summary>
    public Godot.Collections.Dictionary GetObservedState()
    {
        return new Godot.Collections.Dictionary
        {
            ["nodes"] = new Godot.Collections.Dictionary
            {
                ["player"] = new Godot.Collections.Dictionary
                {
                    ["position"] = new Godot.Collections.Dictionary
                    {
                        ["x"] = player.GlobalPosition.X,
                        ["y"] = player.GlobalPosition.Y,
                    },
                    ["health"] = player.Health,
                },
            },
            ["metrics"] = new Godot.Collections.Dictionary
            {
                ["enemies_defeated"] = enemyTracker.DefeatedCount,
            },
        };
    }
}
```

## Writing Scenario Contracts

Scenario contracts are JSON files that define:
- **setup**: Godot scenes to load for the test
- **steps**: A sequence of input simulation and assertion operations
- **timeout**: Max duration in seconds

Always use `assert_value` and `assert_pipeline` operations. Avoid custom operations unless absolutely necessary.

## Harness Conventions

- Harness scenes load the game scene under test and attach a controller
- Keep harnesses minimal — they should only wire up the validation harness to the game scene
- Expose semantic state, not raw node properties
- Use deterministic seeds for any RNG
