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
}
