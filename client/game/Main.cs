namespace Nomad.Game;

using Db;
using Godot;

public partial class Main : Node2D
{
    public override void _Ready()
    {
        AddChild(new DbManager());

        var label = new Label
        {
            Text = "Nomad",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        label.SetAnchorsPreset(Control.LayoutPreset.Center);
        AddChild(label);
    }
}
