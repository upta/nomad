namespace Nomad.Game.Harvest;

using Godot;

[GlobalClass]
public partial class ResourceNodeType : Resource
{
    [Export]
    public Color Color { get; set; } = Colors.Gray;

    [Export]
    public string Glyph { get; set; } = "";

    [Export]
    public string Label { get; set; } = "";

    [Export]
    public string NodeId { get; set; } = "";

    // The item type a harvest pulls from this node (Task 4.2). Mirrors the
    // server's YieldItemFor mapping.
    [Export]
    public string YieldItemId { get; set; } = "";
}
