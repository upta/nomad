namespace Nomad.Game.Ship;

using Godot;

[GlobalClass]
public partial class HullTemplate : Resource
{
    [Export]
    public string HullId { get; set; } = "";

    [Export]
    public int GridWidth { get; set; } = 8;

    [Export]
    public int GridHeight { get; set; } = 6;

    [Export]
    public int ArmorRating { get; set; } = 1;

    [Export]
    public Godot.Collections.Array<RoomSlot> RoomSlots { get; set; } = [];

    public static HullTemplate CreateCorvette()
    {
        var hull = new HullTemplate
        {
            HullId = "corvette",
            GridWidth = 8,
            GridHeight = 6,
            ArmorRating = 1,
        };

        hull.RoomSlots.Add(
            new RoomSlot
            {
                SlotIndex = 0,
                PositionX = 0,
                PositionY = 0,
                Width = 2,
                Height = 2,
            }
        );
        hull.RoomSlots.Add(
            new RoomSlot
            {
                SlotIndex = 1,
                PositionX = 2,
                PositionY = 0,
                Width = 4,
                Height = 2,
            }
        );
        hull.RoomSlots.Add(
            new RoomSlot
            {
                SlotIndex = 2,
                PositionX = 6,
                PositionY = 0,
                Width = 2,
                Height = 2,
            }
        );
        hull.RoomSlots.Add(
            new RoomSlot
            {
                SlotIndex = 3,
                PositionX = 0,
                PositionY = 2,
                Width = 2,
                Height = 2,
            }
        );
        hull.RoomSlots.Add(
            new RoomSlot
            {
                SlotIndex = 4,
                PositionX = 4,
                PositionY = 2,
                Width = 4,
                Height = 2,
            }
        );
        hull.RoomSlots.Add(
            new RoomSlot
            {
                SlotIndex = 5,
                PositionX = 0,
                PositionY = 4,
                Width = 3,
                Height = 2,
            }
        );
        hull.RoomSlots.Add(
            new RoomSlot
            {
                SlotIndex = 6,
                PositionX = 3,
                PositionY = 4,
                Width = 5,
                Height = 2,
            }
        );

        return hull;
    }
}
