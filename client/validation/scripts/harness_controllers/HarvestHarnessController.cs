#nullable enable

namespace Nomad.Validation.HarnessControllers;

using System;
using System.Collections.Generic;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Harvest;
using Nomad.Game.Interaction;
using Nomad.Game.Items;
using Nomad.Game.Map;
using Nomad.Game.Ship;
using Nomad.Game.Ui;

[Meta(typeof(IAutoNode), typeof(IProvide<InteractionService>), typeof(IProvide<HarvestService>))]
public partial class HarvestHarnessController
    : Node2D,
        IProvide<InteractionService>,
        IProvide<HarvestService>
{
    // Mirrors the server channel: ~25 frames (~0.4s) to complete, a comfortable
    // window to observe the ring climb before completion.
    private const float TestHarvestProgressPerFrame = 0.04f;

    private static readonly Dictionary<string, Key> ActionKeyBridge = new()
    {
        ["move_up"] = Key.W,
        ["move_down"] = Key.S,
        ["move_left"] = Key.A,
        ["move_right"] = Key.D,
        ["interact"] = Key.E,
    };

    private readonly Dictionary<string, bool> _bridgeState = [];
    private readonly HarvestService _harvestService = new();
    private readonly InteractionService _interactionService = new();
    private readonly InventoryService _inventoryService = new();
    private readonly Dictionary<string, bool> _testActionState = [];
    private ResourceNodeTypeRegistry _nodeTypeRegistry = null!;
    private int _oreNodeId;
    private Nomad.Game.Player.Player _player = null!;
    private InteractPrompt _prompt = null!;
    private ShipGrid _shipGrid = null!;
    private ResourceNodeSpawner _spawner = null!;
    private Dictionary<string, Action> _testActions = [];

    public override void _Notification(int what) => this.Notify(what);

    // Test actions poll in _PhysicsProcess with manual edge detection — the
    // driver's press/release window spans physics frames that can share a
    // single idle frame, so _Process polling can miss it entirely.
    public override void _PhysicsProcess(double delta)
    {
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

        // Drives the pure-mode channel forward; the real game uses the server's
        // shared ChannelTick.
        if (_harvestService.HasActiveHarvest)
            _harvestService.AdvanceTestHarvest(TestHarvestProgressPerFrame);
    }

    public override void _Process(double delta)
    {
        BridgeInputActionsToKeys();
    }

    public override void _Ready()
    {
        _player = GetNode<Nomad.Game.Player.Player>("Player");
        _shipGrid = GetNode<ShipGrid>("ShipGrid");
        _prompt = GetNode<InteractPrompt>("InteractPrompt");
        _spawner = GetNode<ResourceNodeSpawner>("ResourceNodeSpawner");
        _nodeTypeRegistry = GetNode<ResourceNodeTypeRegistry>("ResourceNodeTypeRegistry");
        _shipGrid.RoomTypeRegistry = GetNode<RoomTypeRegistry>("RoomTypeRegistry");
        _spawner.Registry = _nodeTypeRegistry;

        // The player breaks its own channel on movement; completion deposits the
        // yield into the InventoryService hotbar (the harness owns the deposit,
        // mirroring InventoryService.TestLoadRequested).
        _player.Harvest = _harvestService;
        _spawner.Interacted += OnNodeInteracted;
        _harvestService.Harvested += OnTestHarvested;

        _testActions = new Dictionary<string, Action>
        {
            // Seeds the four node types across the east corridor, mirroring the
            // server Init layout. _oreNodeId tracks the OreVein for depletion.
            ["test_seed_all_nodes"] = SeedAllNodes,
            ["test_seed_ore_node"] = () =>
                _oreNodeId = _harvestService.SeedTestNode("OreVein", new Vector2(96, 0), 5),
            // Spawns on the player's spawn cell so the InteractProbe overlaps it
            // immediately — no flaky walk-up navigation for channel scenarios.
            ["test_seed_node_at_origin"] = () =>
                _oreNodeId = _harvestService.SeedTestNode("OreVein", new Vector2(0, 0), 5),
            ["test_set_ore_yield_partial"] = () => _harvestService.SetTestYield(_oreNodeId, 1),
            ["test_deplete_ore"] = () => _harvestService.SetTestYield(_oreNodeId, 0),
            ["test_clear_nodes"] = () => _harvestService.ClearTestNodes(),
        };
        foreach (var action in _testActions.Keys.Concat(ActionKeyBridge.Keys))
        {
            if (!InputMap.HasAction(action))
                InputMap.AddAction(action);
        }

        this.Provide();

        // Mirror the server Init seed so the hull renders its rooms.
        _shipGrid.SetTestAssignment(0, "Reactor");
        _shipGrid.SetTestAssignment(1, "Bridge");
        _shipGrid.SetTestAssignment(2, "CloningBay");
        _shipGrid.SetTestAssignment(3, "Hydroponics");
        _shipGrid.SetTestAssignment(4, "Workshop");
        _shipGrid.SetTestAssignment(5, "Kitchen");
        _shipGrid.SetTestAssignment(6, "CargoBay");
        _shipGrid.SetTestAssignment(7, "Corridor");
    }

    InteractionService IProvide<InteractionService>.Value() => _interactionService;

    HarvestService IProvide<HarvestService>.Value() => _harvestService;

    public Godot.Collections.Dictionary get_observed_state()
    {
        var nodeTypes = new Godot.Collections.Array();
        foreach (var nt in _nodeTypeRegistry.All)
        {
            nodeTypes.Add(
                new Godot.Collections.Dictionary
                {
                    ["node_id"] = nt.NodeId,
                    ["label"] = nt.Label,
                    ["glyph"] = nt.Glyph,
                    ["yield_item_id"] = nt.YieldItemId,
                    ["color_r"] = nt.Color.R,
                    ["color_g"] = nt.Color.G,
                    ["color_b"] = nt.Color.B,
                }
            );
        }

        var spawned = new List<ResourceNode>();
        foreach (var child in _spawner.GetChildren())
        {
            if (child is ResourceNode node && !node.IsQueuedForDeletion())
                spawned.Add(node);
        }
        spawned.Sort((a, b) => a.NodeId.CompareTo(b.NodeId));

        var nodes = new Godot.Collections.Array();
        foreach (var node in spawned)
        {
            nodes.Add(
                new Godot.Collections.Dictionary
                {
                    ["node_id"] = node.NodeId,
                    ["glyph"] = node.Glyph.Text,
                    ["x"] = node.Position.X,
                    ["y"] = node.Position.Y,
                    ["yield_remaining"] = node.YieldRemaining,
                    ["yield_max"] = node.YieldMax,
                    ["scale"] = node.Visual.Scale.X,
                    ["ring_visible"] = node.HarvestRingVisible,
                    ["color_r"] = node.Sprite.Color.R,
                    ["color_g"] = node.Sprite.Color.G,
                    ["color_b"] = node.Sprite.Color.B,
                }
            );
        }

        var hotbar = new Godot.Collections.Dictionary();
        var slots = _inventoryService.Slots;
        for (var i = 0; i < slots.Count; i++)
        {
            if (slots[i] is { } typeId)
                hotbar[i.ToString()] = typeId;
        }

        return new Godot.Collections.Dictionary
        {
            ["type_count"] = _nodeTypeRegistry.All.Count,
            ["node_types"] = nodeTypes,
            ["node_count"] = _spawner.NodeCount,
            ["nodes"] = nodes,
            ["hotbar"] = hotbar,
            ["hotbar_count"] = hotbar.Count,
            ["harvest"] = new Godot.Collections.Dictionary
            {
                ["active_exists"] = _harvestService.HasActiveHarvest,
                ["node_id"] = _harvestService.ActiveHarvestNodeId,
                ["progress"] = _harvestService.ActiveHarvestProgress,
            },
            ["player"] = new Godot.Collections.Dictionary
            {
                ["x"] = _player.GlobalPosition.X,
                ["y"] = _player.GlobalPosition.Y,
            },
            ["interaction"] = new Godot.Collections.Dictionary
            {
                ["focused_exists"] = _interactionService.Focused is not null,
                ["focused_label"] = _interactionService.Focused?.Label ?? "",
                ["prompt_visible"] = _prompt.Visible,
            },
        };
    }

    private void OnNodeInteracted(int nodeId) => _harvestService.RequestStartHarvest(nodeId);

    // Pure-mode completion: map the harvested node to its yield item and drop it
    // into the hotbar (or nowhere if full — the world-drop path is stdb-only).
    private void OnTestHarvested(int nodeId)
    {
        foreach (var entry in _harvestService.Nodes)
        {
            if (entry.NodeId != nodeId)
                continue;

            if (_nodeTypeRegistry.Find(entry.TypeId) is { } type)
                _inventoryService.AddTestHotbarItem(type.YieldItemId);
            return;
        }
    }

    private void SeedAllNodes()
    {
        _oreNodeId = _harvestService.SeedTestNode("OreVein", new Vector2(96, 0), 5);
        _harvestService.SeedTestNode("WreckageDebris", new Vector2(192, 0), 5);
        _harvestService.SeedTestNode("FuelDepositNode", new Vector2(256, 0), 5);
        _harvestService.SeedTestNode("BiomassPatch", new Vector2(384, 0), 5);
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
}
