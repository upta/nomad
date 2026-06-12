namespace Nomad.Validation.HarnessControllers;

using System.Collections.Generic;
using System.Linq;
using Godot;
using Nomad.Game;
using Nomad.Game.Db;

public partial class ConnectedGameHarnessController : Node2D
{
    // modal_accept/modal_down are harness aliases bridged to real Enter/Down
    // key events so scenarios can drive Control focus navigation.
    private static readonly Dictionary<string, Key> ActionKeyBridge = new()
    {
        ["move_up"] = Key.W,
        ["move_down"] = Key.S,
        ["move_left"] = Key.A,
        ["move_right"] = Key.D,
        ["interact"] = Key.E,
        ["ui_cancel_modal"] = Key.Escape,
        ["modal_accept"] = Key.Enter,
        ["modal_down"] = Key.Down,
        ["hotbar_drop"] = Key.Q,
    };

    // Harness-only InputMap actions that let scenarios drive reducers the
    // validation contract has no other way to reach (press_action only needs
    // the action to exist in InputMap).
    private static readonly Dictionary<
        string,
        System.Action<SpacetimeDB.Types.DbConnection>
    > TestReducerActions = new()
    {
        ["test_toggle_breaker_5"] = conn => conn.Reducers.ToggleBreaker(5),
        ["test_reactor_output_low"] = conn => conn.Reducers.SetReactorOutput(3),
        ["test_reactor_output_high"] = conn => conn.Reducers.SetReactorOutput(10),
        ["test_short_grace"] = conn => conn.Reducers.SetBlackoutGrace(500),
        ["test_long_grace"] = conn => conn.Reducers.SetBlackoutGrace(3000),
        ["test_depressurize_kitchen"] = conn => conn.Reducers.SetPressurization(5, false),
        ["test_repressurize_kitchen"] = conn => conn.Reducers.SetPressurization(5, true),
        ["test_depressurize_corridor"] = conn => conn.Reducers.SetPressurization(7, false),
        ["test_repressurize_corridor"] = conn => conn.Reducers.SetPressurization(7, true),
        ["test_damage_30"] = conn => conn.Reducers.ApplyDebugDamage(30),
        ["test_damage_kill"] = conn => conn.Reducers.ApplyDebugDamage(999),
        ["test_reset_vitals"] = conn => conn.Reducers.ResetVitals(),
        // Fast vitals: 250ms tick, drain 5/tick (~5s tank), refill 25/tick,
        // suffocation 5/tick — timing-sensitive scenarios stay short without
        // racing the assertion windows.
        ["test_fast_vitals"] = conn => conn.Reducers.SetVitalsConfig(250, 5, 25, 5, 0, 0),
        ["test_set_oxygen_low"] = conn => conn.Reducers.SetOxygen(5),
        ["test_equip_suit"] = conn => conn.Reducers.SetSuitEquipped(true),
        ["test_unequip_suit"] = conn => conn.Reducers.SetSuitEquipped(false),
        // Fast hunger isolates the metabolic meter: oxygen rates zeroed so
        // only starvation moves health.
        ["test_fast_hunger"] = conn => conn.Reducers.SetVitalsConfig(250, 0, 25, 0, 5, 5),
        ["test_set_hunger_low"] = conn => conn.Reducers.SetHunger(10),
        ["test_restore_hunger"] = conn => conn.Reducers.RestoreHunger(100),
        ["test_set_biomass_zero"] = conn => conn.Reducers.SetBiomass(0),
        ["test_set_biomass_full"] = conn => conn.Reducers.SetBiomass(3),
        ["test_toggle_breaker_2"] = conn => conn.Reducers.ToggleBreaker(2),
        ["test_request_respawn"] = conn =>
        {
            if (conn.Identity is { } me)
                conn.Reducers.RequestRespawn(me);
        },
        ["test_clear_items"] = conn => conn.Reducers.ClearItems(),
        ["test_spawn_ore_at_origin"] = conn =>
            conn.Reducers.SpawnWorldItem(SpacetimeDB.Types.ItemTypeId.RawOre, 0, 0),
        // Kitchen (slot 5) center per HullGeometry — proves world items render
        // away from the origin, inside a room.
        ["test_spawn_fuelcell_in_kitchen"] = conn =>
            conn.Reducers.SpawnWorldItem(SpacetimeDB.Types.ItemTypeId.FuelCell, 16, 144),
        ["test_give_biomass_slot0"] = conn =>
            conn.Reducers.GiveItem(SpacetimeDB.Types.ItemTypeId.Biomass, 0),
        ["test_give_ore_slot2"] = conn =>
            conn.Reducers.GiveItem(SpacetimeDB.Types.ItemTypeId.RawOre, 2),
        // Occupied-slot rejection probe — fired after slot 0 is filled.
        ["test_give_ore_slot0"] = conn =>
            conn.Reducers.GiveItem(SpacetimeDB.Types.ItemTypeId.RawOre, 0),
        // Direct reducer call (no navigation) so rejection probes exercise
        // the server rule even with the hotbar full or the item far away.
        ["test_pickup_nearest_world_item"] = conn =>
        {
            if (FindNearestWorldItemId(conn) is { } itemId)
                conn.Reducers.PickUpItem(itemId);
        },
        ["test_drop_slot0"] = conn => conn.Reducers.DropItem(0),
        ["test_fill_hotbar"] = conn =>
        {
            for (var slot = 0; slot < 4; slot++)
                conn.Reducers.GiveItem(SpacetimeDB.Types.ItemTypeId.Scrap, slot);
        },
        // Well beyond PickupRadius (96) from anywhere on the hull.
        ["test_spawn_ore_far"] = conn =>
            conn.Reducers.SpawnWorldItem(SpacetimeDB.Types.ItemTypeId.RawOre, 2000, 2000),
        // Load-verb probes: CloningBay is hull slot 2, Reactor slot 0.
        ["test_load_slot0_to_cloning"] = conn => conn.Reducers.LoadItem(0, 2),
        ["test_load_slot0_to_reactor"] = conn => conn.Reducers.LoadItem(0, 0),
        ["test_give_fuelcell_slot0"] = conn =>
            conn.Reducers.GiveItem(SpacetimeDB.Types.ItemTypeId.FuelCell, 0),
        ["test_give_scrap_slot0"] = conn =>
            conn.Reducers.GiveItem(SpacetimeDB.Types.ItemTypeId.Scrap, 0),
        ["test_set_fuel_zero"] = conn => conn.Reducers.SetFuel(0),
        ["test_set_fuel_two"] = conn => conn.Reducers.SetFuel(2),
        ["test_set_fuel_full"] = conn => conn.Reducers.SetFuel(10),
        // Interval 0 keeps the current burn interval (server-side rule).
        ["test_disable_fuel_burn"] = conn => conn.Reducers.SetFuelBurn(0, 0),
        ["test_fast_fuel_burn"] = conn => conn.Reducers.SetFuelBurn(1, 500),
        ["test_slow_fuel_burn"] = conn => conn.Reducers.SetFuelBurn(1, 120000),
    };

    private readonly Dictionary<string, bool> _bridgeState = [];
    private readonly Dictionary<string, bool> _testActionState = [];
    private bool _connectionFailed;
    private bool _dataReady;
    private DbManager _dbManager = null!;
    private Vector2? _initialNodePosition;
    private Vector2? _initialRemotePosition;
    private Vector2? _initialServerPosition;
    private Main? _main;
    private Node2D? _playerNode;
    private PuppetClient? _puppet;
    private Node2D? _remoteNode;
    private int _remoteNodeRecreations;

    [Export]
    public bool EnablePuppet { get; set; }

    [Export]
    public PackedScene MainScene { get; set; } = null!;

    public override void _ExitTree()
    {
        _dbManager.OnDataReady -= OnServerDataReady;
        _dbManager.OnConnectionFailed -= OnServerConnectionFailed;
    }

    public override void _PhysicsProcess(double delta)
    {
        // The driver presses/releases across physics frames that can share a
        // single idle frame — _Process polling would miss the whole window.
        ProcessTestReducerActions();
    }

    public override void _Process(double delta)
    {
        BridgeInputActionsToKeys();
    }

    public override void _Ready()
    {
        foreach (var action in TestReducerActions.Keys.Concat(ActionKeyBridge.Keys))
        {
            if (!InputMap.HasAction(action))
                InputMap.AddAction(action);
        }

        _dbManager = new DbManager();
        _dbManager.OnDataReady += OnServerDataReady;
        _dbManager.OnConnectionFailed += OnServerConnectionFailed;
        AddChild(_dbManager);
        _dbManager.Connect();

        if (EnablePuppet)
        {
            _puppet = new PuppetClient();
            AddChild(_puppet);
        }
    }

    public Godot.Collections.Dictionary get_observed_state()
    {
        var state = new Godot.Collections.Dictionary
        {
            ["connection"] = BuildConnectionState(),
            ["game"] = BuildGameState(),
            ["power"] = BuildPowerState(),
            ["vitals"] = BuildVitalsState(),
            ["stores"] = BuildStoresState(),
            ["items"] = BuildItemsState(),
        };

        if (_puppet is { } puppet)
        {
            state["puppet"] = new Godot.Collections.Dictionary
            {
                ["data_ready"] = puppet.DataReady,
                ["entity_id"] = puppet.EntityId,
                ["displacement_from_initial"] = puppet.DisplacementFromInitial,
            };
        }

        return state;
    }

    // The validation runtime presses InputMap actions, but GUIDE only sees
    // physical key events — forward action state as synthetic key presses.
    private void BridgeInputActionsToKeys()
    {
        foreach (var (action, key) in ActionKeyBridge)
        {
            var pressed = Input.IsActionPressed(action);
            if (_bridgeState.TryGetValue(action, out var wasPressed) && wasPressed == pressed)
                continue;

            _bridgeState[action] = pressed;
            Input.ParseInputEvent(
                new InputEventKey
                {
                    Keycode = key,
                    PhysicalKeycode = key,
                    Pressed = pressed,
                }
            );
        }
    }

    // Edge-detected by hand (like BridgeInputActionsToKeys) — the driver can
    // press the action after this node's _Process ran, so IsActionJustPressed
    // would miss the press entirely.
    private void ProcessTestReducerActions()
    {
        if (_dbManager?.Connection is not { } conn || !conn.IsActive)
            return;

        foreach (var (action, call) in TestReducerActions)
        {
            var pressed = Input.IsActionPressed(action);
            var wasPressed = _testActionState.TryGetValue(action, out var prior) && prior;
            _testActionState[action] = pressed;

            if (pressed && !wasPressed)
            {
                GD.Print($"[Harness] Test action '{action}' fired — calling reducer.");
                call(conn);
            }
        }
    }

    private static int? FindNearestWorldItemId(SpacetimeDB.Types.DbConnection conn)
    {
        if (conn.Identity is not { } me || conn.Db.Players.Identity.Find(me) is not { } player)
            return null;

        if (conn.Db.Entities.EntityId.Find(player.PlayerEntityId) is not { } entity)
            return null;

        var origin = new Vector2(entity.Position.X, entity.Position.Y);
        int? nearest = null;
        var nearestDistSq = float.MaxValue;
        foreach (var item in conn.Db.Items.Iter())
        {
            if (item.LocationKind != SpacetimeDB.Types.ItemLocationKind.World)
                continue;

            var distSq = origin.DistanceSquaredTo(new Vector2(item.Position.X, item.Position.Y));
            if (distSq < nearestDistSq)
            {
                nearestDistSq = distSq;
                nearest = item.ItemId;
            }
        }

        return nearest;
    }

    private Godot.Collections.Dictionary BuildConnectionState()
    {
        var state = new Godot.Collections.Dictionary
        {
            ["is_active"] = _dbManager?.Connection?.IsActive ?? false,
            ["data_ready"] = _dataReady,
            ["connection_failed"] = _connectionFailed,
            ["local_entity_id"] = 0,
            ["player_count"] = 0,
            ["entity_count"] = 0,
        };

        if (!_dataReady || _dbManager?.Connection is not { } conn)
            return state;

        var playerCount = 0;
        foreach (var _ in conn.Db.Players.Iter())
            playerCount++;
        state["player_count"] = playerCount;

        var entityCount = 0;
        foreach (var _ in conn.Db.Entities.Iter())
            entityCount++;
        state["entity_count"] = entityCount;

        var localEntityId = 0;
        if (conn.Identity is { } identity && conn.Db.Players.Identity.Find(identity) is { } player)
            localEntityId = player.PlayerEntityId;
        state["local_entity_id"] = localEntityId;

        if (localEntityId != 0 && conn.Db.Entities.EntityId.Find(localEntityId) is { } entity)
        {
            var serverPosition = new Vector2(entity.Position.X, entity.Position.Y);
            _initialServerPosition ??= serverPosition;
            state["local_entity"] = new Godot.Collections.Dictionary
            {
                ["x"] = serverPosition.X,
                ["y"] = serverPosition.Y,
                ["displacement_from_initial"] = serverPosition.DistanceTo(
                    _initialServerPosition.Value
                ),
                ["distance_to_node"] = _playerNode is null
                    ? -1f
                    : serverPosition.DistanceTo(_playerNode.GlobalPosition),
            };
        }

        return state;
    }

    private Godot.Collections.Dictionary BuildGameState()
    {
        _playerNode ??= _main?.GetNodeOrNull<Node2D>("Player");
        if (_playerNode is not null)
            _initialNodePosition ??= _playerNode.GlobalPosition;

        var state = new Godot.Collections.Dictionary
        {
            ["main_instantiated"] = _main is not null,
            ["player_exists"] = _playerNode is not null,
        };

        if (_playerNode is not null && _initialNodePosition is { } initial)
        {
            state["player"] = new Godot.Collections.Dictionary
            {
                ["x"] = _playerNode.GlobalPosition.X,
                ["y"] = _playerNode.GlobalPosition.Y,
                ["displacement_from_start"] = _playerNode.GlobalPosition.DistanceTo(initial),
            };
        }

        state["focused_label"] = _main?.Interaction.Focused?.Label ?? "";
        state["remote_count"] = _main?.RemoteEntityCount ?? 0;

        var shipGrid = _main?.GetNodeOrNull<Nomad.Game.Map.ShipGrid>("ShipGrid");
        state["terminal_count"] = shipGrid?.TerminalCount ?? 0;

        // WorldItem instances are direct children of Main's ItemSpawner node
        // (lands in 3.1.3 — this reads 0 until then).
        state["world_item_nodes"] = _main?.GetNodeOrNull<Node>("ItemSpawner")?.GetChildCount() ?? 0;
        if (shipGrid is not null)
            state["grid"] = shipGrid.GetObservedRoomState();

        if (_main?.GetNodeOrNull<Nomad.Game.Ui.HotbarHud>("HotbarHud") is { } hotbarHud)
            state["hotbar"] = hotbarHud.GetObservedState();

        var modalHost = _main?.GetNodeOrNull<Nomad.Game.Ui.ModalHost>("ModalHost");
        state["modal"] = new Godot.Collections.Dictionary
        {
            ["open"] = modalHost?.IsOpen ?? false,
            ["title"] = modalHost?.CurrentTitle ?? "",
        };

        if (_puppet is { EntityId: > 0 } puppet && _main is not null)
        {
            // Always read the live registered node — a stale cached reference can
            // be disposed if the game recreates remote nodes. Recreations are
            // counted because a stable remote node IS part of the contract.
            var current = _main.GetRemoteNode(puppet.EntityId);
            if (current is not null)
            {
                if (_remoteNode is not null && !ReferenceEquals(current, _remoteNode))
                    _remoteNodeRecreations++;
                _remoteNode = current;
                _initialRemotePosition ??= current.GlobalPosition;
                state["remote_node"] = new Godot.Collections.Dictionary
                {
                    ["x"] = current.GlobalPosition.X,
                    ["y"] = current.GlobalPosition.Y,
                    ["displacement_from_initial"] = current.GlobalPosition.DistanceTo(
                        _initialRemotePosition.Value
                    ),
                };
            }
            state["remote_node_recreations"] = _remoteNodeRecreations;
        }

        return state;
    }

    private Godot.Collections.Dictionary BuildPowerState()
    {
        var state = new Godot.Collections.Dictionary
        {
            ["status"] = "",
            ["reactor_output"] = 0,
            ["grace_millis"] = 0,
            ["fuel_per_burn"] = -1,
            ["fuel_burn_millis"] = 0,
            ["rooms"] = new Godot.Collections.Dictionary(),
        };

        if (!_dataReady || _dbManager?.Connection is not { } conn)
            return state;

        if (conn.Db.PowerGrids.Id.Find(0) is { } grid)
        {
            state["status"] = grid.Status.ToString();
            state["reactor_output"] = grid.ReactorOutput;
            state["grace_millis"] = grid.GraceMillis;
            state["fuel_per_burn"] = grid.FuelPerBurn;
            state["fuel_burn_millis"] = grid.FuelBurnMillis;
        }

        var rooms = new Godot.Collections.Dictionary();
        foreach (var ra in conn.Db.RoomAssignments.Iter())
        {
            rooms[ra.SlotIndex.ToString()] = new Godot.Collections.Dictionary
            {
                ["breaker_on"] = ra.BreakerOn,
                ["is_powered"] = ra.IsPowered,
                ["is_pressurized"] = ra.IsPressurized,
            };
        }
        state["rooms"] = rooms;

        return state;
    }

    private Godot.Collections.Dictionary BuildVitalsState()
    {
        var state = new Godot.Collections.Dictionary();

        if (!_dataReady || _dbManager?.Connection is not { } conn || conn.Identity is not { } me)
            return state;

        if (conn.Db.Players.Identity.Find(me) is { } player)
            state["current_slot"] = player.CurrentSlotIndex;

        if (conn.Db.VitalsRows.Identity.Find(me) is not { } vitals)
            return state;

        state["health"] = vitals.Health.Current;
        state["max_health"] = vitals.Health.Max;
        state["oxygen"] = vitals.Oxygen.Current;
        state["max_oxygen"] = vitals.Oxygen.Max;
        state["hunger"] = vitals.Hunger.Current;
        state["max_hunger"] = vitals.Hunger.Max;
        state["suit_equipped"] = vitals.SuitEquipped;
        state["is_dead"] = vitals.IsDead;
        return state;
    }

    private Godot.Collections.Dictionary BuildItemsState()
    {
        var state = new Godot.Collections.Dictionary
        {
            ["world_count"] = 0,
            ["by_type"] = new Godot.Collections.Dictionary(),
            ["hotbar_count"] = 0,
            ["hotbar"] = new Godot.Collections.Dictionary(),
            ["world"] = new Godot.Collections.Array(),
            ["config"] = new Godot.Collections.Dictionary(),
        };

        if (!_dataReady || _dbManager?.Connection is not { } conn)
            return state;

        var worldCount = 0;
        var byType = new Godot.Collections.Dictionary();
        var hotbarCount = 0;
        var hotbar = new Godot.Collections.Dictionary();
        var worldRows = new List<SpacetimeDB.Types.Item>();
        foreach (var item in conn.Db.Items.Iter())
        {
            if (item.LocationKind == SpacetimeDB.Types.ItemLocationKind.World)
            {
                worldCount++;
                worldRows.Add(item);
                var key = item.ItemTypeId.ToString();
                byType[key] = byType.TryGetValue(key, out var prior) ? prior.AsInt32() + 1 : 1;
                continue;
            }

            if (
                item.LocationKind == SpacetimeDB.Types.ItemLocationKind.Hotbar
                && conn.Identity is { } me
                && item.Holder == me
            )
            {
                hotbarCount++;
                hotbar[item.SlotIndex.ToString()] = item.ItemTypeId.ToString();
            }
        }

        state["world_count"] = worldCount;
        state["by_type"] = byType;
        state["hotbar_count"] = hotbarCount;
        state["hotbar"] = hotbar;

        // Stable ItemId order so scenarios can address world.0.x etc.
        var world = new Godot.Collections.Array();
        foreach (var item in worldRows.OrderBy(i => i.ItemId))
        {
            world.Add(
                new Godot.Collections.Dictionary
                {
                    ["type"] = item.ItemTypeId.ToString(),
                    ["x"] = item.Position.X,
                    ["y"] = item.Position.Y,
                }
            );
        }
        state["world"] = world;

        if (conn.Db.InventoryConfigs.Id.Find(0) is { } config)
        {
            state["config"] = new Godot.Collections.Dictionary
            {
                ["hotbar_slots"] = config.HotbarSlots,
            };
        }

        return state;
    }

    private Godot.Collections.Dictionary BuildStoresState()
    {
        var state = new Godot.Collections.Dictionary();

        if (!_dataReady || _dbManager?.Connection is not { } conn)
            return state;

        if (conn.Db.ShipStoresRows.Id.Find(0) is { } stores)
        {
            state["biomass"] = stores.Biomass;
            state["fuel"] = stores.Fuel;
        }

        return state;
    }

    private void OnServerConnectionFailed()
    {
        _connectionFailed = true;
    }

    private void OnServerDataReady()
    {
        _dataReady = true;
        _main = MainScene.Instantiate<Main>();
        AddChild(_main);
        _main.InstantiatePlayer(_dbManager);
    }
}
