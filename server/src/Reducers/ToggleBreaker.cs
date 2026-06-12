public static partial class Module
{
    [SpacetimeDB.Reducer]
    public static void ToggleBreaker(ReducerContext ctx, int slotIndex)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is null)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (slotIndex < 0 || slotIndex > 6)
        {
            throw new System.ArgumentOutOfRangeException(
                nameof(slotIndex),
                "Slot index must be 0-6 for Corvette hull."
            );
        }

        var assignment =
            ctx.Db.RoomAssignments.SlotIndex.Find(slotIndex)
            ?? throw new System.InvalidOperationException($"Slot {slotIndex} has no assignment.");

        ctx.Db.RoomAssignments.SlotIndex.Update(
            assignment with
            {
                BreakerOn = !assignment.BreakerOn,
            }
        );

        RecomputePowerGrid(ctx);
    }
}
