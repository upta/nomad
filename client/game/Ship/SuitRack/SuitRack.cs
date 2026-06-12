#nullable enable

namespace Nomad.Game.Ship;

using System;
using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Interaction;

[Meta(typeof(IAutoNode))]
public partial class SuitRack : Node2D
{
    public override void _Notification(int what) => this.Notify(what);

    public event Action<SuitRack>? Interacted;

    [Export]
    public Color EmptyColor { get; set; } = new(0.25f, 0.27f, 0.32f);

    [Node]
    public IColorRect Suit { get; set; } = default!;

    [Export]
    public Color SuitColor { get; set; } = new(0.9f, 0.65f, 0.2f);

    public bool SuitTaken { get; private set; }

    [Node]
    public InteractTarget Target { get; set; } = default!;

    public void OnReady()
    {
        Target.Registration = new CallbackInteractionRegistration(
            () => GlobalPosition,
            () => "Suit Rack",
            _ => Interacted?.Invoke(this)
        );
        UpdateSuit();
    }

    // The rack mirrors the player's suit state: an equipped suit leaves the
    // rack hanger empty.
    public void SetSuitTaken(bool taken)
    {
        SuitTaken = taken;
        UpdateSuit();
    }

    private void UpdateSuit()
    {
        if (Suit is not null)
            Suit.Color = SuitTaken ? EmptyColor : SuitColor;
    }
}
