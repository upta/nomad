namespace Nomad.Game.Interaction;

using System;
using Godot;

public class CallbackInteractionRegistration(
    Func<Vector2> positionGetter,
    string label,
    Action<ProbeData> onInteraction
) : InteractionRegistration(positionGetter, label)
{
    public override void OnInteraction(ProbeData probeData) => onInteraction(probeData);
}
