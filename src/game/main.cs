using Godot;

public partial class Main : Node2D
{
    public override void _Ready()
    {
        var label = new Label
        {
            Text = "Hello, Prototype!",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        label.SetAnchorsPreset(Control.LayoutPreset.Center);
        AddChild(label);
    }
}
