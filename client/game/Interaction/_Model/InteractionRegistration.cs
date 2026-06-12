namespace Nomad.Game.Interaction;

using System;
using Godot;

public abstract class InteractionRegistration(Func<Vector2> positionGetter, string label)
{
    public string Label { get; } = label;

    public Vector2 Position => positionGetter();

    public abstract void OnInteraction(ProbeData probeData);
}
