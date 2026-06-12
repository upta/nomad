namespace Nomad.Game.Interaction;

using Godot;

public class ProbeData(int entityId, Vector2 position)
{
    public int EntityId { get; } = entityId;

    public Vector2 Position { get; set; } = position;
}
