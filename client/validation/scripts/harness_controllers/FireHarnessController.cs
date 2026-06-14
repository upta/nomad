#nullable enable

namespace Nomad.Validation.HarnessControllers;

using System;
using System.Collections.Generic;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Hazard;
using Nomad.Game.Interaction;
using Nomad.Game.Map;
using Nomad.Game.Ship;
using Nomad.Game.Ui;

// Pure (no-server) harness for fire rendering + walk-up extinguish. Seeds
// hazards directly into the HazardService so the FireSpawner renders Fire scenes
// exactly as the connected game does, and routes the extinguish interact back to
// a test-mode removal (the server's ExtinguishHazard in the real game).
[Meta(typeof(IAutoNode), typeof(IProvide<InteractionService>), typeof(IProvide<HazardService>))]
public partial class FireHarnessController
    : Node2D,
        IProvide<InteractionService>,
        IProvide<HazardService>
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
    private readonly HazardService _hazardService = new();
    private readonly InteractionService _interactionService = new();
    private readonly Dictionary<string, bool> _testActionState = [];
    private HazardTypeRegistry _hazardTypeRegistry = null!;
    private Nomad.Game.Player.Player _player = null!;
    private InteractPrompt _prompt = null!;
    private ShipGrid _shipGrid = null!;
    private FireSpawner _spawner = null!;
    private int _fireId;
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
        _shipGrid = GetNode<ShipGrid>("ShipGrid");
        _prompt = GetNode<InteractPrompt>("InteractPrompt");
        _spawner = GetNode<FireSpawner>("FireSpawner");
        _hazardTypeRegistry = GetNode<HazardTypeRegistry>("HazardTypeRegistry");
        _shipGrid.RoomTypeRegistry = GetNode<RoomTypeRegistry>("RoomTypeRegistry");
        _spawner.Registry = _hazardTypeRegistry;
        _spawner.Interacted += OnFireInteracted;

        _testActions = new Dictionary<string, Action>
        {
            // Ignite on the player's spawn cell so the InteractProbe overlaps it
            // immediately — no flaky walk-up for the rendering/extinguish tests.
            ["test_ignite_at_origin"] = () =>
                _fireId = _hazardService.SeedTestHazard("Fire", new Vector2(0, 0), 0.3f),
            ["test_ramp_intensity"] = () => _hazardService.SetTestIntensity(_fireId, 1.0f),
            // Pure-mode "spread": a second fire one tile over, mirroring the
            // server seeding an adjacent cell (real spread is stdb-validated).
            ["test_spread_adjacent"] = () =>
                _hazardService.SeedTestHazard("Fire", new Vector2(32, 0), 0.3f),
            ["test_clear_fires"] = () => _hazardService.ClearTestHazards(),
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

    HazardService IProvide<HazardService>.Value() => _hazardService;

    public Godot.Collections.Dictionary get_observed_state()
    {
        var spawned = new List<Fire>();
        foreach (var child in _spawner.GetChildren())
        {
            if (child is Fire fire && !fire.IsQueuedForDeletion())
                spawned.Add(fire);
        }
        spawned.Sort((a, b) => a.HazardId.CompareTo(b.HazardId));

        var fires = new Godot.Collections.Array();
        foreach (var fire in spawned)
        {
            fires.Add(
                new Godot.Collections.Dictionary
                {
                    ["hazard_id"] = fire.HazardId,
                    ["intensity"] = fire.Intensity,
                    ["scale"] = fire.Visual.Scale.X,
                    ["x"] = fire.Position.X,
                    ["y"] = fire.Position.Y,
                    ["color_r"] = fire.Sprite.Color.R,
                    ["color_b"] = fire.Sprite.Color.B,
                }
            );
        }

        return new Godot.Collections.Dictionary
        {
            ["type_count"] = _hazardTypeRegistry.All.Count,
            ["fire_count"] = _spawner.FireCount,
            ["fires"] = fires,
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

    private void OnFireInteracted(int hazardId) => _hazardService.RemoveTestHazard(hazardId);

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
