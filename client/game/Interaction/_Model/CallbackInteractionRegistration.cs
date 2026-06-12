namespace Nomad.Game.Interaction;

using System;
using Godot;

public class CallbackInteractionRegistration(
    Func<Vector2> positionGetter,
    Func<string> labelGetter,
    Action<ProbeData> onInteraction
) : InteractionRegistration(positionGetter, labelGetter)
{
    public override void OnInteraction(ProbeData probeData) => onInteraction(probeData);
}
