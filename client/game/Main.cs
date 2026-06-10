namespace Nomad.Game;

using System.Collections.Generic;
using Godot;
using Map;
using Player;

[Meta(typeof(IAutoNode))]
public partial class Main : Node2D
{
    private readonly Dictionary<int, ColorRect> _remoteSprites = [];
    private Camera2D _camera = null!;

    [Dependency]
    private DbConnection Server => this.DependOn<DbConnection>();

    public override void _Notification(int what) => this.Notify(what);

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

    public void OnResolved()
    {
        var playerScene = GD.Load<PackedScene>("res://game/Player/Player.tscn");
        var localPlayer = playerScene.Instantiate<Player.Player>();
        AddChild(localPlayer);
        localPlayer.OnResolved();
    }

    public override void _Process(double delta)
    {
        if (Server?.Db?.Entities is not { } entities)
            return;

        var localEntityId = 0;
        if (Server.Identity is { } identity)
        {
            var playerRow = Server.Db.Players.Identity.Find(identity);
            if (playerRow is { } p)
                localEntityId = p.PlayerEntityId;
        }

        var seenIds = new HashSet<int>();

        foreach (var entity in entities.Iter())
        {
            if (!entity.Active || entity.EntityId == localEntityId)
                continue;

            seenIds.Add(entity.EntityId);

            if (!_remoteSprites.TryGetValue(entity.EntityId, out var sprite))
            {
                sprite = new ColorRect
                {
                    Size = new Vector2(32, 32),
                    Color = new Color(0.6f, 0.6f, 0.8f),
                };
                sprite.Position = new Vector2(-16, -16);
                AddChild(sprite);
                _remoteSprites[entity.EntityId] = sprite;
            }

            sprite.GlobalPosition = new Vector2(entity.Position.X, entity.Position.Y);
        }

        var deadIds = new List<int>();
        foreach (var (id, sprite) in _remoteSprites)
        {
            if (!seenIds.Contains(id))
            {
                sprite.QueueFree();
                deadIds.Add(id);
            }
        }
        foreach (var id in deadIds)
            _remoteSprites.Remove(id);
    }
}
