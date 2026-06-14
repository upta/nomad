namespace Nomad.Game.Hazard;

using Godot;

[GlobalClass]
public partial class HazardType : Resource
{
    [Export]
    public Color Color { get; set; } = new(0.95f, 0.45f, 0.1f);

    [Export]
    public string Glyph { get; set; } = "";

    // Display name used in the "Extinguish {Label}" walk-up prompt.
    [Export]
    public string Label { get; set; } = "";

    // Matches the server HazardTypeId.ToString() (e.g. "Fire").
    [Export]
    public string HazardId { get; set; } = "";
}
