public static partial class Module
{
    [SpacetimeDB.Reducer]
    public static void AssignRoomType(ReducerContext ctx, int slotIndex, RoomTypeId roomTypeId)
    {
        if (slotIndex < 0 || slotIndex > 6)
            throw new System.ArgumentOutOfRangeException(
                nameof(slotIndex),
                "Slot index must be 0-6 for Corvette hull."
            );

        if (roomTypeId == RoomTypeId.None)
            throw new System.ArgumentException(
                "Cannot assign None room type to a slot.",
                nameof(roomTypeId)
            );

        if (ctx.Db.RoomAssignments.SlotIndex.Find(slotIndex) is RoomAssignment existing)
        {
            ctx.Db.RoomAssignments.SlotIndex.Update(existing with { RoomTypeId = roomTypeId });
        }
        else
        {
            ctx.Db.RoomAssignments.Insert(
                new RoomAssignment
                {
                    SlotIndex = slotIndex,
                    RoomTypeId = roomTypeId,
                    IsPowered = true,
                    IsPressurized = true,
                    BreakerOn = true,
                    Health = 100f,
                }
            );
        }

        RecomputePowerGrid(ctx);
    }
}
