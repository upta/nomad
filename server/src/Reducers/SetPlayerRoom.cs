public static partial class Module
{
    [SpacetimeDB.Reducer]
    public static void SetPlayerRoom(ReducerContext ctx, int slotIndex)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is not { } player)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (slotIndex != -1 && ctx.Db.RoomAssignments.SlotIndex.Find(slotIndex) is null)
        {
            throw new System.ArgumentOutOfRangeException(
                nameof(slotIndex),
                "No room assignment at this slot."
            );
        }

        ctx.Db.Players.Identity.Update(player with { CurrentSlotIndex = slotIndex });
    }
}
