#nullable enable

namespace Nomad.Validation.HarnessControllers;

using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Interaction;
using Nomad.Game.Map;
using Nomad.Game.Ship;
using Nomad.Game.Ui;

[Meta(typeof(IAutoNode), typeof(IProvide<InteractionService>))]
public partial class InteractionHarnessController : Node2D, IProvide<InteractionService>
{
    private static readonly Dictionary<string, Key> ActionKeyBridge = new()
    {
        ["move_up"] = Key.W,
        ["move_down"] = Key.S,
        ["move_left"] = Key.A,
        ["move_right"] = Key.D,
        ["interact"] = Key.E,
        ["ui_cancel_modal"] = Key.Escape,
    };

    private readonly Dictionary<string, bool> _bridgeState = [];
    private readonly InteractionService _interactionService = new();
    private ModalHost _modalHost = null!;
    private Node2D _player = null!;
    private InteractPrompt _prompt = null!;
    private ShipGrid _shipGrid = null!;

    public override void _Notification(int what) => this.Notify(what);

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

        this.Provide();

        // Mirror the server Init seed so terminals spawn for every room.
        _shipGrid.SetTestAssignment(0, "Reactor");
        _shipGrid.SetTestAssignment(1, "Bridge");
        _shipGrid.SetTestAssignment(2, "CloningBay");
        _shipGrid.SetTestAssignment(3, "Hydroponics");
        _shipGrid.SetTestAssignment(4, "Workshop");
        _shipGrid.SetTestAssignment(5, "Kitchen");
        _shipGrid.SetTestAssignment(6, "CargoBay");
    }

    InteractionService IProvide<InteractionService>.Value() => _interactionService;

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
            ["grid"] = new Godot.Collections.Dictionary
            {
                ["terminal_count"] = _shipGrid.TerminalCount,
            },
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
}
