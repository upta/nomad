public static partial class Module
{
    [SpacetimeDB.Reducer]
    public static void PickUpItem(ReducerContext ctx, int itemId)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is not { } player)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (ctx.Db.VitalsRows.Identity.Find(ctx.Sender) is not { IsDead: false })
        {
            throw new System.InvalidOperationException("Dead players cannot pick up items.");
        }

        if (ctx.Db.Items.ItemId.Find(itemId) is not { } item)
        {
            throw new System.InvalidOperationException("No such item.");
        }

        if (item.LocationKind != ItemLocationKind.World)
        {
            throw new System.InvalidOperationException("Item is not in the world.");
        }

        if (ctx.Db.Entities.EntityId.Find(player.PlayerEntityId) is not { } entity)
        {
            throw new System.InvalidOperationException("Sender has no body.");
        }

        var config = GetInventoryConfig(ctx);
        var dx = entity.Position.X - item.Position.X;
        var dy = entity.Position.Y - item.Position.Y;
        if (dx * dx + dy * dy > config.PickupRadius * config.PickupRadius)
        {
            throw new System.InvalidOperationException("Item is out of reach.");
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
            }
        );
    }
}
