namespace Nomad.Game.Ship;

using Godot;

[GlobalClass]
public partial class HullTemplate : Resource
{
    [Export]
    public int ArmorRating { get; set; } = 1;

    [Export]
    public Godot.Collections.Array<GridRect> Corridors { get; set; } = [];

    [Export]
    public Godot.Collections.Array<Vector2I> Doors { get; set; } = [];

    [Export]
    public int GridHeight { get; set; } = 6;

    [Export]
    public int GridWidth { get; set; } = 8;

    [Export]
    public string HullId { get; set; } = "";

    [Export]
    public Godot.Collections.Array<RoomSlot> RoomSlots { get; set; } = [];
}
