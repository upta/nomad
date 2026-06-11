namespace Nomad.Game;

using System.Collections.Generic;
using Entities;
using Godot;
using Map;

public partial class Main : Node2D
{
    private readonly Dictionary<int, Node2D> _remoteNodes = [];
    private Camera2D _camera = null!;
    private Db.DbManager? _dbManager;
    private ShipGrid? _shipGrid;
    private Player.Player? _localPlayer;
    private int _localEntityId;

    public override void _Ready()
    {
        var roomTypeRegistry = new Ship.RoomTypeRegistry();
        AddChild(roomTypeRegistry);

        var hullTemplate = GD.Load<Ship.HullTemplate>("res://game/Ship/CorvetteHull.tres");

        _shipGrid = GD.Load<PackedScene>("res://game/Map/ShipGrid.tscn").Instantiate<ShipGrid>();
        _shipGrid.HullTemplate = hullTemplate;
        _shipGrid.RoomTypeRegistry = roomTypeRegistry;
        AddChild(_shipGrid);

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
        _dbManager = dbManager;

        if (_shipGrid is not null)
            _shipGrid.BindToServer(dbManager);

        var conn = dbManager.Connection;

        var playerScene = GD.Load<PackedScene>("res://game/Player/Player.tscn");
        _localPlayer = playerScene.Instantiate<Player.Player>();
        _localPlayer.DbManagerNode = dbManager;
        AddChild(_localPlayer);

        _localEntityId = GetLocalEntityId(conn);

        conn.Db.ActiveEntities.OnInsert += OnRemoteEntityInserted;
        conn.Db.ActiveEntities.OnDelete += OnRemoteEntityDeleted;

        foreach (var entity in conn.Db.ActiveEntities.Iter())
        {
            if (entity.EntityId != _localEntityId)
                CreateRemoteNode(conn, entity);
        }
    }

    public override void _Process(double delta)
    {
        if (_localPlayer is not null)
            _camera.GlobalPosition = _localPlayer.GlobalPosition;
    }

    public override void _ExitTree()
    {
        if (_dbManager?.Connection?.Db?.ActiveEntities is { } ae)
        {
            ae.OnInsert -= OnRemoteEntityInserted;
            ae.OnDelete -= OnRemoteEntityDeleted;
        }
    }

    private static int GetLocalEntityId(DbConnection conn)
    {
        if (conn.Identity is { } identity)
        {
            var playerRow = conn.Db.Players.Identity.Find(identity);
            if (playerRow is { } p)
                return p.PlayerEntityId;
        }
        return 0;
    }

    private void OnRemoteEntityInserted(EventContext ctx, Entity entity)
    {
        if (entity.EntityId == _localEntityId || _dbManager is null)
            return;
        if (_remoteNodes.ContainsKey(entity.EntityId))
            return;
        CreateRemoteNode(_dbManager.Connection, entity);
    }

    private void OnRemoteEntityDeleted(EventContext ctx, Entity entity)
    {
        if (_remoteNodes.TryGetValue(entity.EntityId, out var node))
        {
            _remoteNodes.Remove(entity.EntityId);
            node.QueueFree();
        }
    }

    private void CreateRemoteNode(DbConnection conn, Entity entity)
    {
        var node = new Node2D();
        var sprite = new ColorRect
        {
            Size = new Vector2(32, 32),
            Color = new Color(0.6f, 0.6f, 0.8f),
        };
        sprite.Position = new Vector2(-16, -16);
        node.AddChild(sprite);

        var mover = new EntityMover
        {
            EntityId = entity.EntityId,
            Server = conn,
            TargetNode = node,
        };
        node.AddChild(mover);
        mover.Initialize(entity);

        AddChild(node);
        _remoteNodes[entity.EntityId] = node;
    }
}
