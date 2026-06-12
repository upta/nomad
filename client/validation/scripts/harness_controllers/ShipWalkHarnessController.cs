namespace Nomad.Validation.HarnessControllers;

using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Interaction;
using Nomad.Game.Map;
using Nomad.Game.Ship;

[Meta(typeof(IAutoNode), typeof(IProvide<InteractionService>))]
public partial class ShipWalkHarnessController : Node2D, IProvide<InteractionService>
{
    private readonly InteractionService _interactionService = new();

    public override void _Notification(int what) => this.Notify(what);

    private static readonly Dictionary<string, Key> ActionKeyBridge = new()
    {
        ["move_up"] = Key.W,
        ["move_down"] = Key.S,
        ["move_left"] = Key.A,
        ["move_right"] = Key.D,
    };

    private readonly Dictionary<string, bool> _bridgeState = [];
    private Node2D _player = null!;
    private ShipGrid _shipGrid = null!;
    private Vector2 _spawnPosition;

    public override void _Process(double delta)
    {
        BridgeInputActionsToKeys();
    }

    public override void _Ready()
    {
        _player = GetNode<Node2D>("Player");
        _shipGrid = GetNode<ShipGrid>("ShipGrid");
        _shipGrid.RoomTypeRegistry = GetNode<RoomTypeRegistry>("RoomTypeRegistry");
        _spawnPosition = _player.GlobalPosition;

        // Mirror the server Init seed so rooms render with real colors/labels.
        _shipGrid.SetTestAssignment(0, "Reactor");
        _shipGrid.SetTestAssignment(1, "Bridge");
        _shipGrid.SetTestAssignment(2, "CloningBay");
        _shipGrid.SetTestAssignment(3, "Hydroponics");
        _shipGrid.SetTestAssignment(4, "Workshop");
        _shipGrid.SetTestAssignment(5, "Kitchen");
        _shipGrid.SetTestAssignment(6, "CargoBay");

        this.Provide();
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
                ["displacement_from_spawn"] = _player.GlobalPosition.DistanceTo(_spawnPosition),
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
}
