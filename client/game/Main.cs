namespace Nomad.Game;

using System.Collections.Generic;
using Chickensoft.GodotNodeInterfaces;
using Entities;
using Godot;
using Map;

[Meta(typeof(IAutoNode))]
public partial class Main : Node2D
{
    private readonly Dictionary<int, RemoteEntity> _remoteNodes = [];
    private Db.DbManager? _dbManager;
    private ShipGrid? _shipGrid;
    private Player.Player? _localPlayer;
    private int _localEntityId;

    public override void _Notification(int what) => this.Notify(what);

    [Node]
    public ICamera2D Camera { get; set; } = default!;

    [Export]
    public PackedScene RemoteEntityScene { get; set; } = null!;

    public int RemoteEntityCount => _remoteNodes.Count;

    public Node2D? GetRemoteNode(int entityId) => _remoteNodes.GetValueOrDefault(entityId);

    public void OnReady()
    {
        var roomTypeRegistry = new Ship.RoomTypeRegistry();
        AddChild(roomTypeRegistry);

        var hullTemplate = GD.Load<Ship.HullTemplate>("res://game/Ship/CorvetteHull.tres");

        _shipGrid = GD.Load<PackedScene>("res://game/Map/ShipGrid.tscn").Instantiate<ShipGrid>();
        _shipGrid.HullTemplate = hullTemplate;
        _shipGrid.RoomTypeRegistry = roomTypeRegistry;
        AddChild(_shipGrid);

        Camera.MakeCurrent();
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

        // Subscribe to the base Entities table, not the ActiveEntities view:
        // computed views have no primary key, so row updates arrive as
        // insert+delete pairs and would destroy remote nodes on every move.
        conn.Db.Entities.OnInsert += OnEntityInserted;
        conn.Db.Entities.OnUpdate += OnEntityUpdated;
        conn.Db.Entities.OnDelete += OnEntityDeleted;

        foreach (var entity in conn.Db.Entities.Iter())
        {
            TryCreateRemoteNode(entity);
        }
    }

    public override void _Process(double delta)
    {
        if (_localPlayer is not null)
            Camera.GlobalPosition = _localPlayer.GlobalPosition;
    }

    public override void _ExitTree()
    {
        if (_dbManager?.Connection?.Db?.Entities is { } entities)
        {
            entities.OnInsert -= OnEntityInserted;
            entities.OnUpdate -= OnEntityUpdated;
            entities.OnDelete -= OnEntityDeleted;
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

    private void OnEntityDeleted(EventContext ctx, Entity entity)
    {
        RemoveRemoteNode(entity.EntityId);
    }

    private void OnEntityInserted(EventContext ctx, Entity entity)
    {
        TryCreateRemoteNode(entity);
    }

    private void OnEntityUpdated(EventContext ctx, Entity oldEntity, Entity newEntity)
    {
        if (!newEntity.Active)
        {
            RemoveRemoteNode(newEntity.EntityId);
            return;
        }
        TryCreateRemoteNode(newEntity);
    }

    private void RemoveRemoteNode(int entityId)
    {
        if (_remoteNodes.TryGetValue(entityId, out var node))
        {
            _remoteNodes.Remove(entityId);
            node.QueueFree();
        }
    }

    private void TryCreateRemoteNode(Entity entity)
    {
        if (_dbManager is null || !entity.Active)
            return;
        if (entity.EntityId == _localEntityId || _remoteNodes.ContainsKey(entity.EntityId))
            return;
        CreateRemoteNode(_dbManager.Connection, entity);
    }

    private void CreateRemoteNode(DbConnection conn, Entity entity)
    {
        var node = RemoteEntityScene.Instantiate<RemoteEntity>();
        AddChild(node);
        node.Initialize(conn, entity);
        _remoteNodes[entity.EntityId] = node;
    }
}
