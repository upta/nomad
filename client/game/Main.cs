namespace Nomad.Game;

using Db;
using Godot;
using Map;

public partial class Main : Node2D
{
    private Camera2D _camera;

    public override void _Ready()
    {
        AddChild(new DbManager());

        var shipGrid = GD.Load<PackedScene>("res://game/Map/ShipGrid.tscn").Instantiate();
        AddChild(shipGrid);

        _camera = new Camera2D
        {
            PositionSmoothingEnabled = true,
            PositionSmoothingSpeed = 5f,
            Zoom = new Vector2(2.0f, 2.0f),
        };
        AddChild(_camera);
        _camera.MakeCurrent();
    }
}
