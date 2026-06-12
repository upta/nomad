public static partial class Module
{
    [SpacetimeDB.Reducer]
    public static void WithdrawItem(ReducerContext ctx, int itemId)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is not { } player)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (ctx.Db.VitalsRows.Identity.Find(ctx.Sender) is not { IsDead: false })
        {
            throw new System.InvalidOperationException("Dead players cannot withdraw items.");
        }

        if (ctx.Db.Items.ItemId.Find(itemId) is not { } item)
        {
            throw new System.InvalidOperationException("No such item.");
        }

        if (item.LocationKind != ItemLocationKind.Stored)
        {
            throw new System.InvalidOperationException("Item is not in storage.");
        }

        if (ctx.Db.Entities.EntityId.Find(player.PlayerEntityId) is not { } entity)
        {
            throw new System.InvalidOperationException("Sender has no body.");
        }

        // Never trust modal-open state — explicit server-side reach check
        // against the storing room slot's center.
        var config = GetInventoryConfig(ctx);
        var center = SlotCenter(item.RoomSlotIndex);
        var dx = entity.Position.X - center.X;
        var dy = entity.Position.Y - center.Y;
        if (dx * dx + dy * dy > config.LoadRadius * config.LoadRadius)
        {
            throw new System.InvalidOperationException("Storage is out of reach.");
        }

        if (FindFreeHotbarSlot(ctx, ctx.Sender) is not { } slot)
        {
            throw new System.InvalidOperationException("Hotbar is full.");
        }

        ctx.Db.Items.ItemId.Update(
            item with
            {
                LocationKind = ItemLocationKind.Hotbar,
                Position = new DbVector2 { X = 0, Y = 0 },
                Holder = ctx.Sender,
                SlotIndex = slot,
                RoomSlotIndex = -1,
            }
        );
    }
}
