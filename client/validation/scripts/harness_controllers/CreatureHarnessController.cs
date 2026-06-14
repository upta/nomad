#nullable enable

namespace Nomad.Validation.HarnessControllers;

using System;
using System.Collections.Generic;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Creatures;
using Nomad.Game.Harvest;
using Nomad.Game.Interaction;

// Pure (no-server) harness for the exterior surface visuals: creatures render
// from the CreatureService and glide toward their server position, and surface
// resource nodes render on the TerrainGrid from the HarvestService. Seeds both
// directly so the same spawners run as in the connected game (where the server
// CreatureTick / ResourceNodes drive them). The chase/contact LOGIC is
// server-side and proven by the stdb suite; this proves the client rendering +
// position-following.
[Meta(
    typeof(IAutoNode),
    typeof(IProvide<InteractionService>),
    typeof(IProvide<CreatureService>),
    typeof(IProvide<HarvestService>)
)]
public partial class CreatureHarnessController
    : Node2D,
        IProvide<InteractionService>,
        IProvide<CreatureService>,
        IProvide<HarvestService>
{
    private static readonly Dictionary<string, Key> ActionKeyBridge = new()
    {
        ["move_up"] = Key.W,
        ["move_down"] = Key.S,
        ["move_left"] = Key.A,
        ["move_right"] = Key.D,
        ["interact"] = Key.E,
    };

    private readonly Dictionary<string, bool> _bridgeState = [];
    private readonly CreatureService _creatureService = new();
    private readonly HarvestService _harvestService = new();
    private readonly InteractionService _interactionService = new();
    private readonly Dictionary<string, bool> _testActionState = [];
    private CreatureSpawner _creatureSpawner = null!;
    private CreatureTypeRegistry _creatureTypeRegistry = null!;
    private Nomad.Game.Player.Player _player = null!;
    private ResourceNodeSpawner _nodeSpawner = null!;
    private int _creatureId;
    private Dictionary<string, Action> _testActions = [];

    public override void _Notification(int what) => this.Notify(what);

    public override void _PhysicsProcess(double delta)
    {
        BridgeInputActionsToKeys();

        foreach (var (action, run) in _testActions)
        {
            var pressed = Input.IsActionPressed(action);
            var wasPressed = _testActionState.TryGetValue(action, out var prior) && prior;
            _testActionState[action] = pressed;

            if (pressed && !wasPressed)
            {
                GD.Print($"[Harness] Test action '{action}' fired.");
                run();
            }
        }
    }

    public override void _Ready()
    {
        _player = GetNode<Nomad.Game.Player.Player>("Player");
        _creatureSpawner = GetNode<CreatureSpawner>("CreatureSpawner");
        _creatureTypeRegistry = GetNode<CreatureTypeRegistry>("CreatureTypeRegistry");
        _nodeSpawner = GetNode<ResourceNodeSpawner>("ResourceNodeSpawner");
        _creatureSpawner.Registry = _creatureTypeRegistry;
        _nodeSpawner.Registry = GetNode<ResourceNodeTypeRegistry>("ResourceNodeTypeRegistry");

        _testActions = new Dictionary<string, Action>
        {
            // Seed a crawler out on the surface, away from the player at origin.
            ["test_seed_creature"] = () =>
                _creatureId = _creatureService.SeedTestCreature("Crawler", new Vector2(180, 0)),
            // Retarget it onto the player — the spawner glides the node toward
            // the new server position (the connected CreatureTick chase).
            ["test_move_creature_to_player"] = () =>
                _creatureService.SetTestCreaturePosition(_creatureId, _player.GlobalPosition),
            ["test_clear_creatures"] = () => _creatureService.ClearTestCreatures(),
            // Surface harvest nodes on the exterior grid (position-agnostic
            // ResourceNode rows, as the server seeds them at Planetside).
            ["test_seed_surface_nodes"] = () =>
            {
                _harvestService.SeedTestNode("OreVein", new Vector2(220, -64), 5);
                _harvestService.SeedTestNode("FuelDepositNode", new Vector2(360, 80), 5);
            },
            ["test_clear_nodes"] = () => _harvestService.ClearTestNodes(),
        };
        foreach (var action in _testActions.Keys.Concat(ActionKeyBridge.Keys))
        {
            if (!InputMap.HasAction(action))
                InputMap.AddAction(action);
        }

        this.Provide();
    }

    InteractionService IProvide<InteractionService>.Value() => _interactionService;

    CreatureService IProvide<CreatureService>.Value() => _creatureService;

    HarvestService IProvide<HarvestService>.Value() => _harvestService;

    public Godot.Collections.Dictionary get_observed_state()
    {
        var creatures = new List<Nomad.Game.Creatures.Creature>();
        foreach (var child in _creatureSpawner.GetChildren())
        {
            if (child is Nomad.Game.Creatures.Creature creature && !creature.IsQueuedForDeletion())
                creatures.Add(creature);
        }
        creatures.Sort((a, b) => a.CreatureId.CompareTo(b.CreatureId));

        var creatureList = new Godot.Collections.Array();
        foreach (var creature in creatures)
        {
            creatureList.Add(
                new Godot.Collections.Dictionary
                {
                    ["creature_id"] = creature.CreatureId,
                    ["x"] = creature.GlobalPosition.X,
                    ["y"] = creature.GlobalPosition.Y,
                    ["color_r"] = creature.Sprite.Color.R,
                }
            );
        }

        var nodes = new List<ResourceNode>();
        foreach (var child in _nodeSpawner.GetChildren())
        {
            if (child is ResourceNode node && !node.IsQueuedForDeletion())
                nodes.Add(node);
        }
        nodes.Sort((a, b) => a.NodeId.CompareTo(b.NodeId));

        var nodeList = new Godot.Collections.Array();
        foreach (var node in nodes)
        {
            nodeList.Add(
                new Godot.Collections.Dictionary
                {
                    ["node_id"] = node.NodeId,
                    ["x"] = node.GlobalPosition.X,
                    ["y"] = node.GlobalPosition.Y,
                }
            );
        }

        return new Godot.Collections.Dictionary
        {
            ["type_count"] = _creatureTypeRegistry.All.Count,
            ["creature_count"] = _creatureSpawner.CreatureCount,
            ["creatures"] = creatureList,
            ["node_count"] = _nodeSpawner.NodeCount,
            ["nodes"] = nodeList,
            ["player"] = new Godot.Collections.Dictionary
            {
                ["x"] = _player.GlobalPosition.X,
                ["y"] = _player.GlobalPosition.Y,
            },
        };
    }

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
}
