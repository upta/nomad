namespace Nomad.Game.Creatures;

using Godot;

[GlobalClass]
public partial class CreatureType : Resource
{
    [Export]
    public Color Color { get; set; } = new(0.7f, 0.25f, 0.3f);

    [Export]
    public string Glyph { get; set; } = "";

    [Export]
    public string Label { get; set; } = "";

    // Matches the server CreatureTypeId.ToString() (e.g. "Crawler").
    [Export]
    public string CreatureId { get; set; } = "";
}
