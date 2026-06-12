namespace Nomad.Game.Interaction;

using Godot;

[GlobalClass]
public partial class InteractProbe : Area2D
{
    public InteractProbe()
    {
        CollisionLayer = (uint)CollisionLayers.Interactable;
        CollisionMask = (uint)CollisionLayers.Interactable;
    }
}
