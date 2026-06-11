namespace Nomad.Validation.HarnessControllers;

using System.Collections.Generic;
using Godot;
using Nomad.Game;
using Nomad.Game.Db;

public partial class ConnectedGameHarnessController : Node2D
{
    private static readonly Dictionary<string, Key> ActionKeyBridge = new()
    {
        ["move_up"] = Key.W,
        ["move_down"] = Key.S,
        ["move_left"] = Key.A,
        ["move_right"] = Key.D,
    };

    private readonly Dictionary<string, bool> _bridgeState = [];
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

    public override void _Process(double delta)
    {
        BridgeInputActionsToKeys();
    }

    public override void _Ready()
    {
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

        state["remote_count"] = _main?.RemoteEntityCount ?? 0;

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
