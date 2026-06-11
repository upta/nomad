namespace Nomad.Game.Ship;

using Godot;

[GlobalClass]
public partial class GridRect : Resource
{
    [Export]
    public int Height { get; set; } = 1;

    [Export]
    public int PositionX { get; set; }

    [Export]
    public int PositionY { get; set; }

    [Export]
    public int Width { get; set; } = 1;
}
