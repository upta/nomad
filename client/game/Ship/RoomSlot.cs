namespace Nomad.Game.Ship;

using Godot;

[GlobalClass]
public partial class RoomSlot : Resource
{
    [Export]
    public int SlotIndex { get; set; }

    [Export]
    public int PositionX { get; set; }

    [Export]
    public int PositionY { get; set; }

    [Export]
    public int Width { get; set; } = 2;

    [Export]
    public int Height { get; set; } = 2;
}
