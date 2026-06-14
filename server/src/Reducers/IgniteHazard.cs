public static partial class Module
{
    // Starts a hazard at a room slot's center cell (debug now; the entry point
    // hazard events and 5.6 hull breaches will call). At-position ignition for
    // validation lives in IgniteHazardAt.
    [SpacetimeDB.Reducer]
    public static void IgniteHazard(ReducerContext ctx, HazardTypeId typeId, int roomSlot)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is null)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (typeId == HazardTypeId.None)
        {
            throw new System.ArgumentException("Cannot ignite a hazard of type None.");
        }

        if (roomSlot < 0 || roomSlot >= CorvetteSlots.Length)
        {
            throw new System.ArgumentOutOfRangeException(
                nameof(roomSlot),
                "No hull room slot at this index."
            );
        }

        IgniteHazardAtCell(
            ctx,
            typeId,
            SlotCenterCell(roomSlot),
            GetHazardConfig(ctx).IntensityPerTick
        );
    }
}
