#nullable enable

namespace Nomad.Game.Map;

using System;
using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Interaction;

// A single walk-up airlock door, part of the ship (present on every map). It is
// used from both sides: interacting from inside steps you out onto the surface,
// from outside steps you back in. Raises Interacted (no direction); MapHost
// decides the verb from the player's current zone and the server validates
// reach + that the node actually has an exterior. The prompt reflects what the
// interaction will do via LabelProvider (set by MapHost), falling back to a
// static Label.
[Meta(typeof(IAutoNode))]
public partial class Airlock : Node2D
{
    public override void _Notification(int what) => this.Notify(what);

    public event Action? Interacted;

    [Export]
    public string Label { get; set; } = "Airlock";

    // Set by MapHost to render a context-aware prompt ("Exit to surface" /
    // "Enter ship" / sealed). Null falls back to the static Label.
    public Func<string>? LabelProvider { get; set; }

    [Node]
    public InteractTarget Target { get; set; } = default!;

    public void OnReady()
    {
        Target.Registration = new CallbackInteractionRegistration(
            () => GlobalPosition,
            () => LabelProvider?.Invoke() ?? Label,
            _ => Interacted?.Invoke()
        );
    }
}
