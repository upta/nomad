namespace Nomad.Game.Items;

using Godot;

[GlobalClass]
public partial class ItemType : Resource
{
    [Export]
    public Color Color { get; set; } = Colors.Gray;

    [Export]
    public string Glyph { get; set; } = "";

    [Export]
    public string ItemId { get; set; } = "";

    [Export]
    public string Label { get; set; } = "";
}
