#nullable enable

namespace Nomad.Validation.HarnessControllers;

using System;
using System.Collections.Generic;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Character;
using Nomad.Game.Interaction;
using Nomad.Game.Map;
using Nomad.Game.Ship;
using Nomad.Game.Ui;

[Meta(
    typeof(IAutoNode),
    typeof(IProvide<InteractionService>),
    typeof(IProvide<VitalsService>),
    typeof(IProvide<Nomad.Game.Items.InventoryService>)
)]
public partial class GhostHarnessController
    : Node2D,
        IProvide<InteractionService>,
        IProvide<VitalsService>,
        IProvide<Nomad.Game.Items.InventoryService>
{
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
    };

    private readonly Dictionary<string, bool> _bridgeState = [];
    private readonly InteractionService _interactionService = new();
    private readonly Nomad.Game.Items.InventoryService _inventoryService = new();
    private readonly Dictionary<string, bool> _testActionState = [];
    private readonly VitalsService _vitalsService = new();
    private ModalHost _modalHost = null!;
    private Nomad.Game.Player.Player _player = null!;
    private InteractPrompt _prompt = null!;
    private ShipGrid _shipGrid = null!;
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
    }

    public override void _Process(double delta)
    {
        BridgeInputActionsToKeys();
    }

    public override void _Ready()
    {
        _player = GetNode<Nomad.Game.Player.Player>("Player");
        _shipGrid = GetNode<ShipGrid>("ShipGrid");
        _modalHost = GetNode<ModalHost>("ModalHost");
        _prompt = GetNode<InteractPrompt>("InteractPrompt");
        _shipGrid.RoomTypeRegistry = GetNode<RoomTypeRegistry>("RoomTypeRegistry");
        _shipGrid.TerminalInteracted += OnTerminalInteracted;

        // Mirror Main's wiring: vitals drive ghost mode on the player.
        _vitalsService.Changed += OnVitalsChanged;

        _testActions = new Dictionary<string, Action>
        {
            ["test_kill"] = () => _vitalsService.SetTestVitals(0, 100, true),
            ["test_revive"] = () => _vitalsService.SetTestVitals(100, 100, false),
            ["test_seed_dead_crew"] = () =>
            {
                _vitalsService.SetTestBiomass(3);
                _vitalsService.SeedTestCrewMember("Crew Alpha", isDead: true);
            },
        };
        foreach (var action in _testActions.Keys.Concat(ActionKeyBridge.Keys))
        {
            if (!InputMap.HasAction(action))
                InputMap.AddAction(action);
        }

        this.Provide();

        // Mirror the server Init seed so terminals spawn for every room.
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

    VitalsService IProvide<VitalsService>.Value() => _vitalsService;

    Nomad.Game.Items.InventoryService IProvide<Nomad.Game.Items.InventoryService>.Value() =>
        _inventoryService;

    public Godot.Collections.Dictionary get_observed_state()
    {
        return new Godot.Collections.Dictionary
        {
            ["player"] = new Godot.Collections.Dictionary
            {
                ["x"] = _player.GlobalPosition.X,
                ["y"] = _player.GlobalPosition.Y,
                ["is_ghost"] = _player.IsGhostMode,
            },
            ["vitals"] = new Godot.Collections.Dictionary { ["is_dead"] = _vitalsService.IsDead },
            ["interaction"] = new Godot.Collections.Dictionary
            {
                ["focused_exists"] = _interactionService.Focused is not null,
                ["focused_label"] = _interactionService.Focused?.Label ?? "",
                ["prompt_visible"] = _prompt.Visible,
            },
            ["modal"] = BuildModalState(),
        };
    }

    private Godot.Collections.Dictionary BuildModalState()
    {
        var state = new Godot.Collections.Dictionary
        {
            ["open"] = _modalHost.IsOpen,
            ["title"] = _modalHost.CurrentTitle,
        };

        if (_modalHost.CurrentModal is CloningModal cloning)
        {
            state["dead_rows"] = cloning.DeadRowCount;
            state["biomass"] = cloning.ShownBiomass;
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

    private void OnTerminalInteracted(Terminal terminal) =>
        _modalHost.Open(
            new RoomModalInfo(
                terminal.RoomLabel,
                terminal.TerminalType,
                terminal.IsPowered,
                terminal.IsPressurized,
                terminal.SlotIndex
            )
        );

    private void OnVitalsChanged() => _player.SetGhostMode(_vitalsService.IsDead);
}
