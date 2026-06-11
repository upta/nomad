namespace Nomad.Validation.HarnessControllers;

using Godot;
using Nomad.Game.Ship;

public partial class HullHarnessController : Node
{
    private HullTemplate _hull = null!;

    public override void _Ready()
    {
        _hull = GD.Load<HullTemplate>("res://game/Ship/CorvetteHull.tres");
        if (_hull is null)
        {
            GD.PrintErr("[HullHarnessController] CorvetteHull.tres failed to load");
            return;
        }

        GD.Print(
            $"[HullHarnessController] Loaded .tres: {_hull.HullId} with {_hull.RoomSlots.Count} slots"
        );

        var gridVisual = new HullGridVisualizer { Name = "GridVisual" };
        gridVisual.SetHull(_hull);
        AddChild(gridVisual);
    }

    public Godot.Collections.Dictionary get_observed_state()
    {
        if (_hull is null)
        {
            return new Godot.Collections.Dictionary
            {
                ["error"] = "CorvetteHull.tres failed to load",
            };
        }

        var slotList = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var slot in _hull.RoomSlots)
        {
            slotList.Add(
                new Godot.Collections.Dictionary
                {
                    ["index"] = slot.SlotIndex,
                    ["x"] = slot.PositionX,
                    ["y"] = slot.PositionY,
                    ["width"] = slot.Width,
                    ["height"] = slot.Height,
                }
            );
        }

        return new Godot.Collections.Dictionary
        {
            ["hull_id"] = _hull.HullId,
            ["grid_width"] = _hull.GridWidth,
            ["grid_height"] = _hull.GridHeight,
            ["armor_rating"] = _hull.ArmorRating,
            ["room_count"] = _hull.RoomSlots.Count,
            ["room_slots"] = slotList,
            ["corridor_count"] = _hull.Corridors.Count,
            ["door_count"] = _hull.Doors.Count,
        };
    }
}
