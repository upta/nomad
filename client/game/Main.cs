namespace Nomad.Game;

using Godot;
using Map;

public partial class Main : Node2D
{
    public override void _Ready()
    {
        var shipGrid = GD.Load<PackedScene>("res://game/Map/ShipGrid.tscn").Instantiate();
        AddChild(shipGrid);

        var camera = new Camera2D
        {
            PositionSmoothingEnabled = true,
            PositionSmoothingSpeed = 5f,
            Zoom = new Vector2(2.0f, 2.0f),
        };
        AddChild(camera);
        camera.MakeCurrent();
    }
}
