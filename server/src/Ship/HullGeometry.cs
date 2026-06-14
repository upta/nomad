public static partial class Module
{
    // Corvette slot-center world coordinates, mirroring the client's
    // CorvetteHull.tres (29x16 grid, 32px tiles, world origin at grid center)
    // the same way PowerRules hard-codes per-room draws. If the hull resource
    // changes shape, these must change with it.
    private const float TileSize = 32f;
    private const float GridWidth = 29f;
    private const float GridHeight = 16f;

    private static readonly (int X, int Y, int W, int H)[] CorvetteSlots =
    [
        (1, 1, 6, 5),
        (8, 1, 7, 5),
        (16, 1, 6, 5),
        (23, 1, 5, 5),
        (1, 10, 9, 5),
        (11, 10, 8, 5),
        (20, 10, 8, 5),
    ];

    private static DbVector2 SlotCenter(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= CorvetteSlots.Length)
        {
            throw new System.ArgumentOutOfRangeException(
                nameof(slotIndex),
                "No hull slot at this index."
            );
        }

        var (x, y, w, h) = CorvetteSlots[slotIndex];
        return new DbVector2
        {
            X = (x + w / 2f - GridWidth / 2f) * TileSize,
            Y = (y + h / 2f - GridHeight / 2f) * TileSize,
        };
    }

    // --- Floor-cell geometry (fire spread) -------------------------------------
    //
    // Mirrors the client's ShipGrid.BuildMap: floor cells = the room rects + the
    // main corridor + the door cells, on the 29x16 grid. Fire spreads across
    // these cells, so the server needs the same map the client renders.
    //
    // World<->cell uses the INTEGER grid offset (GridWidth/2 = 14, GridHeight/2 =
    // 8) — the exact `HullTemplate.GridWidth / 2` integer division the client
    // renders and walks with — so an ignited cell's world position lands on the
    // tile the crew stands on. (SlotCenter above uses 14.5f, a tolerated ~16px
    // skew the 96px reach radius absorbs; fire needs pixel alignment, so it uses
    // the integer offset instead.)

    private const int GridOffsetX = 14;
    private const int GridOffsetY = 8;
    private const int CorridorSlotIndex = 7;

    private static readonly (int X, int Y, int W, int H) CorvetteCorridor = (1, 7, 27, 2);

    private static readonly (int X, int Y)[] CorvetteDoorCells =
    [
        (3, 6),
        (4, 6),
        (11, 6),
        (12, 6),
        (18, 6),
        (19, 6),
        (24, 6),
        (25, 6),
        (4, 9),
        (5, 9),
        (14, 9),
        (15, 9),
        (23, 9),
        (24, 9),
    ];

    private static readonly System.Collections.Generic.HashSet<(int X, int Y)> FloorCells =
        BuildFloorCells();

    private static System.Collections.Generic.HashSet<(int X, int Y)> BuildFloorCells()
    {
        var cells = new System.Collections.Generic.HashSet<(int X, int Y)>();

        foreach (var (x, y, w, h) in CorvetteSlots)
        {
            AddRectCells(cells, x, y, w, h);
        }

        AddRectCells(
            cells,
            CorvetteCorridor.X,
            CorvetteCorridor.Y,
            CorvetteCorridor.W,
            CorvetteCorridor.H
        );

        foreach (var door in CorvetteDoorCells)
        {
            cells.Add(door);
        }

        return cells;
    }

    private static void AddRectCells(
        System.Collections.Generic.HashSet<(int X, int Y)> cells,
        int x,
        int y,
        int w,
        int h
    )
    {
        for (var dx = 0; dx < w; dx++)
        for (var dy = 0; dy < h; dy++)
        {
            cells.Add((x + dx, y + dy));
        }
    }

    private static bool IsFloorCell((int X, int Y) cell) => FloorCells.Contains(cell);

    private static (int X, int Y) WorldToCell(DbVector2 pos) =>
        (
            (int)System.Math.Floor(pos.X / TileSize) + GridOffsetX,
            (int)System.Math.Floor(pos.Y / TileSize) + GridOffsetY
        );

    private static DbVector2 CellToWorld((int X, int Y) cell) =>
        new()
        {
            X = (cell.X - GridOffsetX + 0.5f) * TileSize,
            Y = (cell.Y - GridOffsetY + 0.5f) * TileSize,
        };

    // The floor cell at a room slot's canonical center — the SAME point
    // respawns and terminals use (SlotCenter, computed on the 14.5f float
    // center), snapped to its containing cell. Routing through SlotCenter keeps
    // room-targeted IgniteHazard aligned with where the crew actually stands;
    // a plain floor(center) would sit half a tile off in even-width rooms.
    private static (int X, int Y) SlotCenterCell(int slotIndex) =>
        WorldToCell(SlotCenter(slotIndex));

    // Which room slot a cell belongs to: a hull room (0-6), the corridor (7,
    // including door cells), or -1 if it's not floor at all.
    private static int SlotForCell((int X, int Y) cell)
    {
        for (var i = 0; i < CorvetteSlots.Length; i++)
        {
            var (x, y, w, h) = CorvetteSlots[i];
            if (cell.X >= x && cell.X < x + w && cell.Y >= y && cell.Y < y + h)
            {
                return i;
            }
        }

        return FloorCells.Contains(cell) ? CorridorSlotIndex : -1;
    }

    private static (int X, int Y)[] Neighbors4((int X, int Y) cell) =>
        [(cell.X + 1, cell.Y), (cell.X - 1, cell.Y), (cell.X, cell.Y + 1), (cell.X, cell.Y - 1)];
}
