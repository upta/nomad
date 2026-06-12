namespace Nomad.Game.Interaction;

using System;
using Godot;

public abstract class InteractionRegistration(
    Func<Vector2> positionGetter,
    Func<string> labelGetter
)
{
    public string Label => labelGetter();

    public Vector2 Position => positionGetter();

    public abstract void OnInteraction(ProbeData probeData);
}
