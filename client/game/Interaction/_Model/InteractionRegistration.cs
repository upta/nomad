namespace Nomad.Game.Interaction;

using System;
using Godot;

public abstract class InteractionRegistration(
    Func<Vector2> positionGetter,
    Func<string> labelGetter
)
{
    // Ghosts cannot interact with physical objects; the Cloning Bay terminal
    // (their anchor back to life) opts in via this flag.
    public bool GhostAccessible { get; set; }

    public string Label => labelGetter();

    public Vector2 Position => positionGetter();

    public abstract void OnInteraction(ProbeData probeData);
}
