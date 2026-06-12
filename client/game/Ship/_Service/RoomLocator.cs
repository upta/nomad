#nullable enable

namespace Nomad.Game.Ship;

using Godot;

// Resolves which room slot a world position falls in: room-slot interiors win,
// corridor rects (and door cells, which belong to the corridor network) map to
// the corridor slot, anything else is -1 (no room / vacuum).
public class RoomLocator
{
    // Must match ShipGrid.TileSize — the grid renderer and this locator share
    // the same world-to-cell mapping (grid centered on the world origin).
    public const int TileSize = 32;

    private readonly HullTemplate _hull;

    public RoomLocator(HullTemplate hull)
    {
        _hull = hull;
    }

    public int CorridorSlotIndex => _hull.RoomSlots.Count;

    public int SlotAt(Vector2 worldPosition)
    {
        var cell = new Vector2I(
            Mathf.FloorToInt(worldPosition.X / TileSize) + _hull.GridWidth / 2,
            Mathf.FloorToInt(worldPosition.Y / TileSize) + _hull.GridHeight / 2
        );

        foreach (var slot in _hull.RoomSlots)
        {
            if (
                cell.X >= slot.PositionX
                && cell.X < slot.PositionX + slot.Width
                && cell.Y >= slot.PositionY
                && cell.Y < slot.PositionY + slot.Height
            )
            {
                return slot.SlotIndex;
            }
        }

        foreach (var corridor in _hull.Corridors)
        {
            if (
                cell.X >= corridor.PositionX
                && cell.X < corridor.PositionX + corridor.Width
                && cell.Y >= corridor.PositionY
                && cell.Y < corridor.PositionY + corridor.Height
            )
            {
                return CorridorSlotIndex;
            }
        }

        foreach (var door in _hull.Doors)
        {
            if (cell == door)
                return CorridorSlotIndex;
        }

        return -1;
    }
}
