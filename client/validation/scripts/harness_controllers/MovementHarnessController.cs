namespace Nomad.Validation.HarnessControllers;

using Game.Player;
using Godot;

public partial class MovementHarnessController : Node
{
    private Player _player = null!;

    public override void _Ready()
    {
        _player = GetNode<Player>("Player");
    }

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
                        ["x"] = _player.GlobalPosition.X,
                        ["y"] = _player.GlobalPosition.Y,
                    },
                },
            },
        };
    }
}
