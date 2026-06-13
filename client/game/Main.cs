namespace Nomad.Game;

using System.Collections.Generic;
using Chickensoft.GodotNodeInterfaces;
using Entities;
using Godot;
using Interaction;
using Map;
using Ui;

[Meta(
    typeof(IAutoNode),
    typeof(IProvide<InteractionService>),
    typeof(IProvide<Ship.PowerGridService>),
    typeof(IProvide<Character.VitalsService>),
    typeof(IProvide<Items.InventoryService>),
    typeof(IProvide<Items.ItemTypeRegistry>)
)]
public partial class Main
    : Node2D,
        IProvide<InteractionService>,
        IProvide<Ship.PowerGridService>,
        IProvide<Character.VitalsService>,
        IProvide<Items.InventoryService>,
        IProvide<Items.ItemTypeRegistry>
{
    private readonly InteractionService _interactionService = new();
    private readonly Items.InventoryService _inventoryService = new();
    private readonly Ship.PowerGridService _powerGridService = new();
    private readonly Character.VitalsService _vitalsService = new();
    private readonly Dictionary<int, RemoteEntity> _remoteNodes = [];
    private Db.DbManager? _dbManager;
    private Player.Player? _localPlayer;
    private int _localEntityId;
    private bool _wasDead;

    public override void _Notification(int what) => this.Notify(what);

    [Node]
    public ICamera2D Camera { get; set; } = default!;

    [Node]
    public Items.ItemSpawner ItemSpawner { get; set; } = default!;

    [Node]
    public Items.ItemTypeRegistry ItemTypeRegistry { get; set; } = default!;

    [Node]
    public HotbarHud HotbarHud { get; set; } = default!;

    [Node]
    public ModalHost ModalHost { get; set; } = default!;

    [Export]
    public PackedScene PlayerScene { get; set; } = null!;

    [Export]
    public PackedScene RemoteEntityScene { get; set; } = null!;

    [Node]
    public Ship.RoomTypeRegistry RoomTypeRegistry { get; set; } = default!;

    [Node]
    public ShipGrid ShipGrid { get; set; } = default!;

    public InteractionService Interaction => _interactionService;

    public int RemoteEntityCount => _remoteNodes.Count;

    public Node2D? GetRemoteNode(int entityId) => _remoteNodes.GetValueOrDefault(entityId);

    public void OnReady()
    {
        // Node exports don't survive the scene-instance boundary, so the
        // registries are handed over here instead of in Main.tscn.
        ShipGrid.RoomTypeRegistry = RoomTypeRegistry;
        ItemSpawner.Registry = ItemTypeRegistry;
        HotbarHud.Registry = ItemTypeRegistry;
        ShipGrid.TerminalInteracted += OnTerminalInteracted;
        ShipGrid.BreakerInteracted += OnBreakerInteracted;
        ShipGrid.SuitRackInteracted += OnSuitRackInteracted;
        ItemSpawner.Interacted += OnWorldItemInteracted;
        HotbarHud.DropRequested += OnHotbarDropRequested;
        _vitalsService.Changed += OnVitalsChanged;

        _powerGridService.SetRoomCatalog(RoomTypeRegistry.All);

        Camera.MakeCurrent();

        this.Provide();
    }

    InteractionService IProvide<InteractionService>.Value() => _interactionService;

    Ship.PowerGridService IProvide<Ship.PowerGridService>.Value() => _powerGridService;

    Character.VitalsService IProvide<Character.VitalsService>.Value() => _vitalsService;

    Items.InventoryService IProvide<Items.InventoryService>.Value() => _inventoryService;

    Items.ItemTypeRegistry IProvide<Items.ItemTypeRegistry>.Value() => ItemTypeRegistry;

    public void InstantiatePlayer(Db.DbManager dbManager)
    {
        _dbManager = dbManager;

        ShipGrid.BindToServer(dbManager);
        _powerGridService.BindConnection(dbManager.Connection);
        _vitalsService.BindConnection(dbManager.Connection);
        _inventoryService.BindConnection(dbManager.Connection);

        var conn = dbManager.Connection;

        _localPlayer = PlayerScene.Instantiate<Player.Player>();
        _localPlayer.DbManagerNode = dbManager;
        _localPlayer.Hull = ShipGrid.HullTemplate;
        AddChild(_localPlayer);

        _localEntityId = GetLocalEntityId(conn);

        // Subscribe to the base Entities table, not the ActiveEntities view:
        // computed views have no primary key, so row updates arrive as
        // insert+delete pairs and would destroy remote nodes on every move.
        conn.Db.Entities.OnInsert += OnEntityInserted;
        conn.Db.Entities.OnUpdate += OnEntityUpdated;
        conn.Db.Entities.OnDelete += OnEntityDeleted;
        conn.Db.VitalsRows.OnInsert += OnRemoteVitalsRow;
        conn.Db.VitalsRows.OnUpdate += OnRemoteVitalsUpdated;

        foreach (var entity in conn.Db.Entities.Iter())
        {
            TryCreateRemoteNode(entity);
        }
    }

    // Physics tick, not _Process: with physics interpolation enabled the camera
    // transform must only change on physics ticks or the interpolator judders.
    public override void _PhysicsProcess(double delta)
    {
        if (_localPlayer is not null)
            Camera.GlobalPosition = _localPlayer.GlobalPosition;
    }

    public override void _ExitTree()
    {
        ShipGrid.TerminalInteracted -= OnTerminalInteracted;
        ShipGrid.BreakerInteracted -= OnBreakerInteracted;
        ShipGrid.SuitRackInteracted -= OnSuitRackInteracted;
        ItemSpawner.Interacted -= OnWorldItemInteracted;
        HotbarHud.DropRequested -= OnHotbarDropRequested;
        _vitalsService.Changed -= OnVitalsChanged;
        _powerGridService.Unbind();
        _vitalsService.Unbind();
        _inventoryService.Unbind();

        if (_dbManager?.Connection?.Db?.Entities is { } entities)
        {
            entities.OnInsert -= OnEntityInserted;
            entities.OnUpdate -= OnEntityUpdated;
            entities.OnDelete -= OnEntityDeleted;
        }

        if (_dbManager?.Connection?.Db?.VitalsRows is { } vitalsRows)
        {
            vitalsRows.OnInsert -= OnRemoteVitalsRow;
            vitalsRows.OnUpdate -= OnRemoteVitalsUpdated;
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

    private void OnBreakerInteracted(Ship.Breaker breaker) =>
        _powerGridService.RequestToggleBreaker(breaker.SlotIndex);

    private void OnHotbarDropRequested() =>
        _inventoryService.RequestDrop(
            _inventoryService.SelectedSlot,
            _localPlayer?.GlobalPosition ?? Vector2.Zero
        );

    private void OnWorldItemInteracted(int itemId) => _inventoryService.RequestPickUp(itemId);

    private void OnSuitRackInteracted(Ship.SuitRack rack) =>
        _dbManager?.Connection?.Reducers.SetSuitEquipped(!_vitalsService.SuitEquipped);

    private void OnVitalsChanged()
    {
        _localPlayer?.SetSuitEquipped(_vitalsService.SuitEquipped, _vitalsService.SuitSpeedFactor);
        _localPlayer?.SetGhostMode(_vitalsService.IsDead);
        ShipGrid.SetSuitRackState(_vitalsService.SuitEquipped);

        // Position is normally client-authoritative; respawn is the one case
        // where the server places the body (at the Cloning Bay), so the local
        // node snaps to the server entity on the revive transition.
        if (_wasDead && !_vitalsService.IsDead)
            SnapLocalPlayerToServerEntity();
        _wasDead = _vitalsService.IsDead;
    }

    private void SnapLocalPlayerToServerEntity()
    {
        if (
            _localPlayer is null
            || _dbManager?.Connection is not { } conn
            || conn.Db.Entities.EntityId.Find(_localEntityId) is not { } entity
        )
        {
            return;
        }

        _localPlayer.GlobalPosition = new Vector2(entity.Position.X, entity.Position.Y);
        _localPlayer.Velocity = Vector2.Zero;
        _localPlayer.ResetPhysicsInterpolation();
    }

    private void OnTerminalInteracted(Ship.Terminal terminal) =>
        ModalHost.Open(
            new RoomModalInfo(
                terminal.RoomLabel,
                terminal.TerminalType,
                terminal.IsPowered,
                terminal.IsPressurized,
                terminal.SlotIndex
            )
        );

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

        // A remote player may already be a ghost when their node spawns.
        foreach (var player in conn.Db.Players.Iter())
        {
            if (
                player.PlayerEntityId == entity.EntityId
                && conn.Db.VitalsRows.Identity.Find(player.Identity) is { } vitals
            )
            {
                node.SetGhost(vitals.IsDead);
                break;
            }
        }
    }

    // Remote ghosts render translucent: map the vitals row's identity to its
    // player entity and tint the matching remote node.
    private void OnRemoteVitalsRow(EventContext ctx, SpacetimeDB.Types.Vitals vitals)
    {
        if (_dbManager?.Connection is not { } conn)
            return;

        if (conn.Db.Players.Identity.Find(vitals.Identity) is not { } player)
            return;

        if (_remoteNodes.TryGetValue(player.PlayerEntityId, out var node))
            node.SetGhost(vitals.IsDead);
    }

    private void OnRemoteVitalsUpdated(
        EventContext ctx,
        SpacetimeDB.Types.Vitals oldVitals,
        SpacetimeDB.Types.Vitals newVitals
    ) => OnRemoteVitalsRow(ctx, newVitals);
}
