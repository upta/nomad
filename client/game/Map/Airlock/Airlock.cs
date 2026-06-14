#nullable enable

namespace Nomad.Game.Map;

using System;
using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Interaction;

// A walk-up airlock door placed on an exterior map. The ship-side airlock
// (Exits = true) reads "Exit to surface" and crosses the crew out; the
// landing-pad airlock (Exits = false) reads "Enter ship" and brings them back.
// Raises Interacted with its direction; the owning map forwards it to MapHost,
// which calls EnterExterior / EnterInterior (the server validates reach + zone).
[Meta(typeof(IAutoNode))]
public partial class Airlock : Node2D
{
    public override void _Notification(int what) => this.Notify(what);

    // true = exit to surface (EnterExterior); false = enter ship (EnterInterior).
    public event Action<bool>? Interacted;

    [Export]
    public bool Exits { get; set; } = true;

    [Export]
    public string Label { get; set; } = "Exit to surface";

    [Node]
    public InteractTarget Target { get; set; } = default!;

    public void OnReady()
    {
        Target.Registration = new CallbackInteractionRegistration(
            () => GlobalPosition,
            () => Label,
            _ => Interacted?.Invoke(Exits)
        );
    }
}
