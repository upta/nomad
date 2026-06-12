public static partial class Module
{
    // Debug/validation giver — the real hotbar acquisition path (pickup)
    // lands with the pickup/drop feature.
    [SpacetimeDB.Reducer]
    public static void GiveItem(ReducerContext ctx, ItemTypeId itemTypeId, int slotIndex)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is null)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (itemTypeId == ItemTypeId.None)
        {
            throw new System.ArgumentException("Cannot give an item of type None.");
        }

        if (ctx.Db.VitalsRows.Identity.Find(ctx.Sender) is not { IsDead: false })
        {
            throw new System.InvalidOperationException("Dead players cannot receive items.");
        }

        var config = GetInventoryConfig(ctx);
        if (slotIndex < 0 || slotIndex >= config.HotbarSlots)
        {
            throw new System.ArgumentException(
                $"Hotbar slot must be in [0, {config.HotbarSlots})."
            );
        }

        foreach (var item in ctx.Db.Items.Holder.Filter(ctx.Sender))
        {
            if (item.LocationKind == ItemLocationKind.Hotbar && item.SlotIndex == slotIndex)
            {
                throw new System.InvalidOperationException($"Hotbar slot {slotIndex} is occupied.");
            }
        }

        ctx.Db.Items.Insert(
            new Item
            {
                ItemId = 0,
                ItemTypeId = itemTypeId,
                LocationKind = ItemLocationKind.Hotbar,
                Position = new DbVector2 { X = 0, Y = 0 },
                Holder = ctx.Sender,
                SlotIndex = slotIndex,
                RoomSlotIndex = -1,
            }
        );
    }
}
