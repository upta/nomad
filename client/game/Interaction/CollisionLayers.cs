namespace Nomad.Game.Interaction;

using System;

[Flags]
public enum CollisionLayers : uint
{
    None = 0,
    Player = 1 << 0,
    Interactable = 1 << 1,
}
