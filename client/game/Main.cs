namespace Nomad.Game;

using System.Collections.Generic;
using Godot;
using Map;

public partial class Main : Node2D
{
    private readonly Dictionary<int, ColorRect> _remoteSprites = [];
    private Camera2D _camera = null!;

    public override void _Ready()
    {
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

    public void InstantiatePlayer(Db.DbManager dbManager)
    {
        var playerScene = GD.Load<PackedScene>("res://game/Player/Player.tscn");
        var player = playerScene.Instantiate<Player.Player>();
        player.DbManagerNode = dbManager;
        AddChild(player);
    }

    public override void _Process(double delta)
    {
        // Remote rendering (placeholder — needs DbConnection reference)
    }
}
