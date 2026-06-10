namespace Nomad.Validation.HarnessControllers;

using Godot;

public partial class MovementHarnessController : Node2D
{
    [Export]
    public NodePath ActorPath { get; set; } = new("Player");

    [Export]
    public NodePath SpawnPointPath { get; set; } = new("SpawnPoint");

    private Node2D _actor = null!;
    private Marker2D _spawnPoint = null!;

    public override void _Ready()
    {
        _actor = GetNode<Node2D>(ActorPath) ?? this;
        _spawnPoint = GetNode<Marker2D>(SpawnPointPath) ?? new Marker2D();
    }

    public Godot.Collections.Dictionary get_observed_state()
    {
        return new Godot.Collections.Dictionary
        {
            ["actor_position"] = _actor.GlobalPosition,
            ["spawn_position"] = _spawnPoint.GlobalPosition,
            ["actor_displacement_from_spawn"] = _actor.GlobalPosition - _spawnPoint.GlobalPosition,
            ["nodes"] = new Godot.Collections.Dictionary
            {
                ["player"] = new Godot.Collections.Dictionary
                {
                    ["position"] = new Godot.Collections.Dictionary
                    {
                        ["x"] = _actor.GlobalPosition.X,
                        ["y"] = _actor.GlobalPosition.Y,
                    },
                },
            },
        };
    }
}
