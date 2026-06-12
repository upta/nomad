public static partial class Module
{
    [SpacetimeDB.Reducer]
    public static void SetPressurization(ReducerContext ctx, int slotIndex, bool isPressurized)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is null)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (ctx.Db.RoomAssignments.SlotIndex.Find(slotIndex) is not { } room)
        {
            throw new System.ArgumentOutOfRangeException(
                nameof(slotIndex),
                "No room assignment at this slot."
            );
        }

        ctx.Db.RoomAssignments.SlotIndex.Update(room with { IsPressurized = isPressurized });

        // No RecomputePowerGrid: pressurization is orthogonal to power.
    }
}
