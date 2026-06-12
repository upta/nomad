#nullable enable

namespace Nomad.Game.Ship;

using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Interaction;

[Meta(typeof(IAutoNode))]
public partial class Terminal : Node2D
{
    public override void _Notification(int what) => this.Notify(what);

    public event Action<Terminal>? Interacted;

    public bool IsPowered { get; private set; }

    public bool IsPressurized { get; private set; }

    public string RoomLabel { get; private set; } = "";

    public int SlotIndex { get; set; }

    [Node]
    public InteractTarget Target { get; set; } = default!;

    public TerminalType TerminalType { get; private set; } = TerminalType.Info;

    public void OnReady()
    {
        Target.Registration = new CallbackInteractionRegistration(
            () => GlobalPosition,
            () => RoomLabel.Length > 0 ? $"{RoomLabel} Terminal" : "Terminal",
            _ => Interacted?.Invoke(this)
        );
    }

    public void SetRoomState(
        string roomLabel,
        TerminalType terminalType,
        bool isPowered,
        bool isPressurized
    )
    {
        RoomLabel = roomLabel;
        TerminalType = terminalType;
        IsPowered = isPowered;
        IsPressurized = isPressurized;

        // The Cloning terminal is the one interactable ghosts may use.
        if (Target?.Registration is { } registration)
            registration.GhostAccessible = terminalType == TerminalType.Cloning;
    }
}
