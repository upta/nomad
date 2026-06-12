#nullable enable

namespace Nomad.Game.Ship;

using System;
using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Interaction;

[Meta(typeof(IAutoNode))]
public partial class Breaker : Node2D
{
    public override void _Notification(int what) => this.Notify(what);

    public event Action<Breaker>? Interacted;

    public bool BreakerOn { get; private set; } = true;

    [Node]
    public IColorRect Lever { get; set; } = default!;

    [Export]
    public Color LeverOffColor { get; set; } = new(0.75f, 0.25f, 0.2f);

    [Export]
    public Color LeverOnColor { get; set; } = new(0.35f, 0.85f, 0.4f);

    public string RoomLabel { get; private set; } = "";

    public int SlotIndex { get; set; }

    [Node]
    public InteractTarget Target { get; set; } = default!;

    public void OnReady()
    {
        Target.Registration = new CallbackInteractionRegistration(
            () => GlobalPosition,
            () => RoomLabel.Length > 0 ? $"{RoomLabel} Breaker" : "Breaker",
            _ => Interacted?.Invoke(this)
        );
        UpdateLever();
    }

    public void SetState(string roomLabel, bool breakerOn)
    {
        RoomLabel = roomLabel;
        BreakerOn = breakerOn;
        UpdateLever();
    }

    private void UpdateLever()
    {
        if (Lever is not null)
            Lever.Color = BreakerOn ? LeverOnColor : LeverOffColor;
    }
}
