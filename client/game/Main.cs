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
    typeof(IProvide<Items.ItemTypeRegistry>),
    typeof(IProvide<Harvest.HarvestService>),
    typeof(IProvide<Hazard.HazardService>),
    typeof(IProvide<Creatures.CreatureService>),
    typeof(IProvide<Crafting.CraftingService>),
    typeof(IProvide<Crafting.RecipeRegistry>)
)]
public partial class Main
    : Node2D,
        IProvide<InteractionService>,
        IProvide<Ship.PowerGridService>,
        IProvide<Character.VitalsService>,
        IProvide<Items.InventoryService>,
        IProvide<Items.ItemTypeRegistry>,
        IProvide<Harvest.HarvestService>,
        IProvide<Hazard.HazardService>,
        IProvide<Creatures.CreatureService>,
        IProvide<Crafting.CraftingService>,
        IProvide<Crafting.RecipeRegistry>
{
    private readonly Crafting.CraftingService _craftingService = new();
    private readonly Creatures.CreatureService _creatureService = new();
    private readonly Harvest.HarvestService _harvestService = new();
    private readonly Hazard.HazardService _hazardService = new();
    private readonly InteractionService _interactionService = new();
    private readonly Items.InventoryService _inventoryService = new();
    private readonly Ship.PowerGridService _powerGridService = new();
    private readonly Character.VitalsService _vitalsService = new();
    private readonly Dictionary<int, RemoteEntity> _remoteNodes = [];
    private Db.DbManager? _dbManager;
    private Player.Player? _localPlayer;
    private int _localEntityId;
    private bool _wasDead;
    private GameMap _activeMap = null!;
    private ShipGrid _shipGrid = null!;
    private SpacetimeDB.Types.NodeKind _currentNodeKind = SpacetimeDB.Types.NodeKind.Quiet;
    private bool _localInExterior;

    public override void _Notification(int what) => this.Notify(what);

    [Node]
    public ICamera2D Camera { get; set; } = default!;

    [Node]
    public DebugHud DebugHud { get; set; } = default!;

    [Node]
    public Creatures.CreatureSpawner CreatureSpawner { get; set; } = default!;

    [Node]
    public Creatures.CreatureTypeRegistry CreatureTypeRegistry { get; set; } = default!;

    [Node]
    public Hazard.FireSpawner FireSpawner { get; set; } = default!;

    [Node]
    public Hazard.HazardTypeRegistry HazardTypeRegistry { get; set; } = default!;

    [Node]
    public Items.ItemSpawner ItemSpawner { get; set; } = default!;

    [Node]
    public Items.ItemTypeRegistry ItemTypeRegistry { get; set; } = default!;

    [Node]
    public HotbarHud HotbarHud { get; set; } = default!;

    [Node]
    public Node2D MapMount { get; set; } = default!;

    [Node]
    public ModalHost ModalHost { get; set; } = default!;

    [Export]
    public PackedScene PlayerScene { get; set; } = null!;

    [Export]
    public PackedScene QuietMapScene { get; set; } = null!;

    [Export]
    public PackedScene PlanetsideMapScene { get; set; } = null!;

    [Export]
    public PackedScene RemoteEntityScene { get; set; } = null!;

    [Node]
    public Crafting.RecipeRegistry RecipeRegistry { get; set; } = default!;

    [Node]
    public Harvest.ResourceNodeSpawner ResourceNodeSpawner { get; set; } = default!;

    [Node]
    public Harvest.ResourceNodeTypeRegistry ResourceNodeTypeRegistry { get; set; } = default!;

    [Node]
    public Ship.RoomTypeRegistry RoomTypeRegistry { get; set; } = default!;

    // The active map owns the Ship component (and its ShipGrid); harness
    // observation and the gameplay wiring below reach the grid through here so
    // the node can move maps without touching every call site.
    public ShipGrid ShipGrid => _shipGrid;

    // The node the client currently has a map loaded for — lets validation
    // confirm the client actually swapped maps on a node change, not just the
    // server's NodeActivity row.
    public SpacetimeDB.Types.NodeKind ActiveNodeKind => _currentNodeKind;

    public InteractionService Interaction => _interactionService;

    public int RemoteEntityCount => _remoteNodes.Count;

    public Node2D? GetRemoteNode(int entityId) => _remoteNodes.GetValueOrDefault(entityId);

    public void OnReady()
    {
        LoadMap(QuietMapScene);

        // Node exports don't survive the scene-instance boundary, so the
        // registries are handed over here instead of in Main.tscn.
        ItemSpawner.Registry = ItemTypeRegistry;
        ResourceNodeSpawner.Registry = ResourceNodeTypeRegistry;
        FireSpawner.Registry = HazardTypeRegistry;
        CreatureSpawner.Registry = CreatureTypeRegistry;
        HotbarHud.Registry = ItemTypeRegistry;
        ItemSpawner.Interacted += OnWorldItemInteracted;
        ResourceNodeSpawner.Interacted += OnResourceNodeInteracted;
        FireSpawner.Interacted += OnFireInteracted;
        HotbarHud.DropRequested += OnHotbarDropRequested;
        HotbarHud.UseRequested += OnHotbarUseRequested;
        DebugHud.ResetRequested += OnDebugResetRequested;
        DebugHud.IgniteFireRequested += OnDebugIgniteFireRequested;
        DebugHud.NodeToggleRequested += OnDebugNodeToggleRequested;
        _vitalsService.Changed += OnVitalsChanged;

        _powerGridService.SetRoomCatalog(RoomTypeRegistry.All);

        Camera.MakeCurrent();

        this.Provide();
    }

    // Instantiates the map for the current node and wires its ShipGrid + any
    // airlocks. The same Ship component rides every map, so on a live node swap
    // the fresh ShipGrid re-subscribes to the (unchanged) server ship state.
    private void LoadMap(PackedScene scene)
    {
        _activeMap = scene.Instantiate<GameMap>();
        MapMount.AddChild(_activeMap);
        _activeMap.AirlockUsed += OnAirlockUsed;

        _shipGrid = _activeMap.Ship.ShipGrid;
        _shipGrid.RoomTypeRegistry = RoomTypeRegistry;
        _shipGrid.TerminalInteracted += OnTerminalInteracted;
        _shipGrid.BreakerInteracted += OnBreakerInteracted;
        _shipGrid.SuitRackInteracted += OnSuitRackInteracted;

        // The ship's airlock prompt reflects what interacting will do, given
        // the live zone + node (the door itself is direction-agnostic).
        _activeMap.Ship.Airlock.LabelProvider = AirlockLabel;

        // The initial load runs before the connection is up (InstantiatePlayer
        // binds it); a live swap must re-bind the new grid and restore the
        // ship's current suit-rack visual itself.
        if (_dbManager is not null)
        {
            _shipGrid.BindToServer(_dbManager);
            _shipGrid.SetSuitRackState(_vitalsService.SuitEquipped);
        }
    }

    // Swaps the active map (node change). Tears down the old map's wiring,
    // frees it, and loads the new one. Spawners/HUDs/camera/services live on
    // Main, so only the map's ship-interior grid + exterior grid/airlocks swap;
    // fire, surface nodes, creatures, and items persist (they render from Main).
    private void SwitchMap(PackedScene scene)
    {
        _activeMap.AirlockUsed -= OnAirlockUsed;
        _shipGrid.TerminalInteracted -= OnTerminalInteracted;
        _shipGrid.BreakerInteracted -= OnBreakerInteracted;
        _shipGrid.SuitRackInteracted -= OnSuitRackInteracted;
        MapMount.RemoveChild(_activeMap);
        _activeMap.QueueFree();

        LoadMap(scene);
    }

    private void ApplyNodeKind(SpacetimeDB.Types.NodeKind kind)
    {
        if (kind == _currentNodeKind)
            return;
        _currentNodeKind = kind;
        SwitchMap(SceneForNode(kind));
    }

    // Wreck/TradingPost/DefenseEvent reuse the Quiet ship-in-space view until
    // their maps land in 5.3+; Planetside is the one exterior map today.
    private PackedScene SceneForNode(SpacetimeDB.Types.NodeKind kind) =>
        kind == SpacetimeDB.Types.NodeKind.Planetside ? PlanetsideMapScene : QuietMapScene;

    // Airlock walk-up → server verb, chosen from the player's current zone: a
    // single door steps you out when inside (at an exterior node) and back in
    // when outside. The server validates reach + zone and teleports the body;
    // the local node snaps when the InExterior flag flips (OnPlayerRowUpdated).
    // At a node with no exterior the door is sealed (no-op).
    private void OnAirlockUsed()
    {
        if (_dbManager?.Connection is not { } conn)
            return;
        if (_localInExterior)
            conn.Reducers.EnterInterior();
        else if (_currentNodeKind == SpacetimeDB.Types.NodeKind.Planetside)
            conn.Reducers.EnterExterior();
    }

    private string AirlockLabel() =>
        _localInExterior ? "Enter ship"
        : _currentNodeKind == SpacetimeDB.Types.NodeKind.Planetside ? "Exit to surface"
        : "Airlock";

    InteractionService IProvide<InteractionService>.Value() => _interactionService;

    Ship.PowerGridService IProvide<Ship.PowerGridService>.Value() => _powerGridService;

    Character.VitalsService IProvide<Character.VitalsService>.Value() => _vitalsService;

    Items.InventoryService IProvide<Items.InventoryService>.Value() => _inventoryService;

    Items.ItemTypeRegistry IProvide<Items.ItemTypeRegistry>.Value() => ItemTypeRegistry;

    Harvest.HarvestService IProvide<Harvest.HarvestService>.Value() => _harvestService;

    Hazard.HazardService IProvide<Hazard.HazardService>.Value() => _hazardService;

    Creatures.CreatureService IProvide<Creatures.CreatureService>.Value() => _creatureService;

    Crafting.CraftingService IProvide<Crafting.CraftingService>.Value() => _craftingService;

    Crafting.RecipeRegistry IProvide<Crafting.RecipeRegistry>.Value() => RecipeRegistry;

    public void InstantiatePlayer(Db.DbManager dbManager)
    {
        _dbManager = dbManager;

        ShipGrid.BindToServer(dbManager);
        _powerGridService.BindConnection(dbManager.Connection);
        _vitalsService.BindConnection(dbManager.Connection);
        _inventoryService.BindConnection(dbManager.Connection);
        _harvestService.BindConnection(dbManager.Connection);
        _hazardService.BindConnection(dbManager.Connection);
        _creatureService.BindConnection(dbManager.Connection);
        _craftingService.BindConnection(dbManager.Connection);

        var conn = dbManager.Connection;

        _localPlayer = PlayerScene.Instantiate<Player.Player>();
        _localPlayer.DbManagerNode = dbManager;
        _localPlayer.Hull = ShipGrid.HullTemplate;
        _localPlayer.Harvest = _harvestService;
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
        conn.Db.NodeActivities.OnInsert += OnNodeActivityRow;
        conn.Db.NodeActivities.OnUpdate += OnNodeActivityUpdated;
        conn.Db.Players.OnUpdate += OnPlayerRowUpdated;

        // Match the map to whatever node the ship is already anchored at (a
        // late-joining client may arrive mid-Planetside).
        if (conn.Db.NodeActivities.Id.Find(0) is { } node)
            ApplyNodeKind(node.Kind);

        if (conn.Identity is { } me && conn.Db.Players.Identity.Find(me) is { } selfPlayer)
            _localInExterior = selfPlayer.InExterior;

        foreach (var entity in conn.Db.Entities.Iter())
        {
            TryCreateRemoteNode(entity);
        }
    }

    private void OnNodeActivityRow(EventContext ctx, SpacetimeDB.Types.NodeActivity node) =>
        ApplyNodeKind(node.Kind);

    private void OnNodeActivityUpdated(
        EventContext ctx,
        SpacetimeDB.Types.NodeActivity oldNode,
        SpacetimeDB.Types.NodeActivity newNode
    ) => ApplyNodeKind(newNode.Kind);

    // The body is normally client-authoritative; crossing an airlock is a
    // server-placed teleport (like respawn), so snap the local node to the
    // server entity when the InExterior flag flips for us.
    private void OnPlayerRowUpdated(
        EventContext ctx,
        SpacetimeDB.Types.Player oldPlayer,
        SpacetimeDB.Types.Player newPlayer
    )
    {
        if (_dbManager?.Connection is not { } conn || conn.Identity != newPlayer.Identity)
            return;
        if (newPlayer.InExterior != oldPlayer.InExterior)
        {
            _localInExterior = newPlayer.InExterior;
            SnapLocalPlayerToServerEntity();
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
        if (_activeMap is not null)
            _activeMap.AirlockUsed -= OnAirlockUsed;
        ItemSpawner.Interacted -= OnWorldItemInteracted;
        ResourceNodeSpawner.Interacted -= OnResourceNodeInteracted;
        FireSpawner.Interacted -= OnFireInteracted;
        HotbarHud.DropRequested -= OnHotbarDropRequested;
        HotbarHud.UseRequested -= OnHotbarUseRequested;
        DebugHud.ResetRequested -= OnDebugResetRequested;
        DebugHud.IgniteFireRequested -= OnDebugIgniteFireRequested;
        DebugHud.NodeToggleRequested -= OnDebugNodeToggleRequested;
        _vitalsService.Changed -= OnVitalsChanged;
        _powerGridService.Unbind();
        _vitalsService.Unbind();
        _inventoryService.Unbind();
        _harvestService.Unbind();
        _hazardService.Unbind();
        _creatureService.Unbind();
        _craftingService.Unbind();

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

        if (_dbManager?.Connection?.Db is { } db)
        {
            db.NodeActivities.OnInsert -= OnNodeActivityRow;
            db.NodeActivities.OnUpdate -= OnNodeActivityUpdated;
            db.Players.OnUpdate -= OnPlayerRowUpdated;
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

    private void OnDebugResetRequested() => _dbManager?.Connection?.Reducers.ResetWorld();

    // Debug node switch — flip between Quiet (ship in space) and Planetside
    // (surface) so the airlock + exterior map are reachable before the Star
    // Chart / Jump verbs land in Phase 6. The map swap follows the
    // NodeActivities subscription.
    private void OnDebugNodeToggleRequested()
    {
        var next =
            _currentNodeKind == SpacetimeDB.Types.NodeKind.Planetside
                ? SpacetimeDB.Types.NodeKind.Quiet
                : SpacetimeDB.Types.NodeKind.Planetside;
        _dbManager?.Connection?.Reducers.SetActiveNode(next);
    }

    // Debug demo: ignite a fire on the local player's cell so the hazard system
    // is playable before in-game ignition sources (events, breaches) land.
    private void OnDebugIgniteFireRequested()
    {
        if (_localPlayer is { } player)
            _dbManager?.Connection?.Reducers.IgniteHazardAt(
                SpacetimeDB.Types.HazardTypeId.Fire,
                player.GlobalPosition.X,
                player.GlobalPosition.Y
            );
    }

    private void OnHotbarDropRequested() =>
        _inventoryService.RequestDrop(
            _inventoryService.SelectedSlot,
            _localPlayer?.GlobalPosition ?? Vector2.Zero
        );

    private void OnHotbarUseRequested() =>
        _inventoryService.RequestUse(_inventoryService.SelectedSlot);

    private void OnWorldItemInteracted(int itemId) => _inventoryService.RequestPickUp(itemId);

    private void OnResourceNodeInteracted(int nodeId) =>
        _harvestService.RequestStartHarvest(nodeId);

    private void OnFireInteracted(int hazardId) =>
        _dbManager?.Connection?.Reducers.ExtinguishHazard(hazardId);

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
                terminal.SlotIndex,
                terminal.RoomId
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
