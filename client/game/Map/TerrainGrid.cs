#nullable enable

namespace Nomad.Game.Map;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

// A flat exterior surface for the planetside/wreck/station maps: a filled
// ground rect plus faint tile lines, drawn from the node's local origin
// outward. Purely cosmetic — players, creatures, and surface resource nodes
// render at their own world positions on top of it. Position the node in the
// map scene to place the surface relative to the ship (which sits at origin).
[Meta(typeof(IAutoNode))]
public partial class TerrainGrid : Node2D
{
    public override void _Notification(int what) => this.Notify(what);

    [Export]
    public int TileSize { get; set; } = 32;

    [Export]
    public int Columns { get; set; } = 24;

    [Export]
    public int Rows { get; set; } = 19;

    [Export]
    public Color GroundColor { get; set; } = new(0.28f, 0.22f, 0.18f);

    [Export]
    public Color GridLineColor { get; set; } = new(0.34f, 0.27f, 0.22f);

    public override void _Draw()
    {
        var width = Columns * TileSize;
        var height = Rows * TileSize;

        DrawRect(new Rect2(0, 0, width, height), GroundColor);

        for (var c = 0; c <= Columns; c++)
        {
            var x = c * TileSize;
            DrawLine(new Vector2(x, 0), new Vector2(x, height), GridLineColor, 1f);
        }

        for (var r = 0; r <= Rows; r++)
        {
            var y = r * TileSize;
            DrawLine(new Vector2(0, y), new Vector2(width, y), GridLineColor, 1f);
        }
    }
}
