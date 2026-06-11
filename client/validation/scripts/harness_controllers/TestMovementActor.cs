namespace Nomad.Validation.HarnessControllers;

using Godot;

public partial class TestMovementActor : Node2D
{
    [Export]
    public float MoveSpeed { get; set; } = 400f;

    public override void _Ready()
    {
        var indicator = CreateVisualIndicator();
        AddChild(indicator);
    }

    public override void _PhysicsProcess(double delta)
    {
        var direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        GlobalPosition += direction * MoveSpeed * (float)delta;
    }

    public void ResetToSpawn(Vector2 spawnPosition)
    {
        GlobalPosition = spawnPosition;
    }

    public Vector2 GetWorldPosition()
    {
        return GlobalPosition;
    }

    private static Sprite2D CreateVisualIndicator()
    {
        var image = Image.CreateEmpty(20, 20, false, Image.Format.Rgba8);
        image.Fill(new Color(0.2f, 0.6f, 1.0f));
        var texture = ImageTexture.CreateFromImage(image);
        return new Sprite2D { Texture = texture, Centered = true };
    }
}
