#nullable enable

namespace Nomad.Validation.HarnessControllers;

using System;
using System.Collections.Generic;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Interaction;
using Nomad.Game.Map;
using Nomad.Game.Ship;
using Nomad.Game.Ui;

[Meta(typeof(IAutoNode), typeof(IProvide<InteractionService>), typeof(IProvide<PowerGridService>))]
public partial class PowerHarnessController
    : Node2D,
        IProvide<InteractionService>,
        IProvide<PowerGridService>
{
    // modal_accept/modal_down are harness-registered aliases bridged to real
    // Enter/Down key events so scenarios can drive Control focus navigation —
    // a single deterministic path instead of relying on InputEventAction
    // reaching the GUI.
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
    private readonly PowerGridService _powerGridService = new();
    private readonly Dictionary<string, bool> _testActionState = [];
    private ModalHost _modalHost = null!;
    private Node2D _player = null!;
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
        _player = GetNode<Node2D>("Player");
        _shipGrid = GetNode<ShipGrid>("ShipGrid");
        _modalHost = GetNode<ModalHost>("ModalHost");
        _prompt = GetNode<InteractPrompt>("InteractPrompt");
        _shipGrid.RoomTypeRegistry = GetNode<RoomTypeRegistry>("RoomTypeRegistry");
        _shipGrid.TerminalInteracted += OnTerminalInteracted;
        _shipGrid.BreakerInteracted += OnBreakerInteracted;

        _testActions = new Dictionary<string, Action>
        {
            ["test_cut_power_kitchen"] = () => _shipGrid.SetTestPower(5, false, false),
            ["test_restore_power_kitchen"] = () => _shipGrid.SetTestPower(5, true, true),
            ["test_grid_overload"] = () => _shipGrid.SetTestGridStatus("Overload"),
            ["test_grid_stable"] = () => _shipGrid.SetTestGridStatus("Stable"),
            ["test_assign_kitchen_reactor"] = () => AssignTestRoom(5, "Reactor"),
        };
        foreach (var action in _testActions.Keys.Concat(ActionKeyBridge.Keys))
        {
            if (!InputMap.HasAction(action))
                InputMap.AddAction(action);
        }

        _powerGridService.SetRoomCatalog(GetNode<RoomTypeRegistry>("RoomTypeRegistry").All);
        _powerGridService.Changed += SyncServiceToGrid;
        _powerGridService.SetTestGrid(10, "Stable");

        this.Provide();

        // Mirror the server Init seed so terminals spawn for every room.
        AssignTestRoom(0, "Reactor");
        AssignTestRoom(1, "Bridge");
        AssignTestRoom(2, "CloningBay");
        AssignTestRoom(3, "Hydroponics");
        AssignTestRoom(4, "Workshop");
        AssignTestRoom(5, "Kitchen");
        AssignTestRoom(6, "CargoBay");
    }

    InteractionService IProvide<InteractionService>.Value() => _interactionService;

    PowerGridService IProvide<PowerGridService>.Value() => _powerGridService;

    public Godot.Collections.Dictionary get_observed_state()
    {
        return new Godot.Collections.Dictionary
        {
            ["player"] = new Godot.Collections.Dictionary
            {
                ["x"] = _player.GlobalPosition.X,
                ["y"] = _player.GlobalPosition.Y,
            },
            ["interaction"] = new Godot.Collections.Dictionary
            {
                ["focused_exists"] = _interactionService.Focused is not null,
                ["focused_label"] = _interactionService.Focused?.Label ?? "",
                ["registration_count"] = _interactionService.Registrations.Count,
                ["prompt_visible"] = _prompt.Visible,
            },
            ["modal"] = new Godot.Collections.Dictionary
            {
                ["open"] = _modalHost.IsOpen,
                ["title"] = _modalHost.CurrentTitle,
            },
            ["grid"] = _shipGrid.GetObservedRoomState(),
        };
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

    private void AssignTestRoom(int slotIndex, string roomTypeId)
    {
        _shipGrid.SetTestAssignment(slotIndex, roomTypeId);
        _powerGridService.SeedTestRoom(slotIndex, roomTypeId);
    }

    // Pure harness has no server: toggles flip local service state the way
    // the ToggleBreaker reducer would, and SyncServiceToGrid keeps ShipGrid
    // rendering in agreement.
    private void OnBreakerInteracted(Breaker breaker) =>
        _powerGridService.RequestToggleBreaker(breaker.SlotIndex);

    private void SyncServiceToGrid()
    {
        foreach (var room in _powerGridService.Rooms)
            _shipGrid.SetTestPower(room.Slot, room.BreakerOn, room.IsPowered);
    }

    private void OnTerminalInteracted(Terminal terminal) =>
        _modalHost.Open(
            new RoomModalInfo(
                terminal.RoomLabel,
                terminal.TerminalType,
                terminal.IsPowered,
                terminal.IsPressurized
            )
        );
}
