namespace Nomad.Validation.HarnessControllers;

using Godot;

public partial class TestMovementActor : Node2D
{
    [Export]
    public float MoveSpeed { get; set; } = 400f;

    public override void _PhysicsProcess(double delta)
    {
        var direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        GlobalPosition += direction * MoveSpeed * (float)delta;
    }

    /// <summary>
    /// Called by harness controller for resetting position.
    /// </summary>
    public void ResetToSpawn(Vector2 spawnPosition)
    {
        GlobalPosition = spawnPosition;
    }

    /// <summary>
    /// Returns the current world position for harness state observation.
    /// </summary>
    public Vector2 GetWorldPosition()
    {
        return GlobalPosition;
    }
}
