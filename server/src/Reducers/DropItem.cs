public static partial class Module
{
    [SpacetimeDB.Reducer]
    public static void DropItem(ReducerContext ctx, int slotIndex)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is not { } player)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (ctx.Db.VitalsRows.Identity.Find(ctx.Sender) is not { IsDead: false })
        {
            throw new System.InvalidOperationException("Dead players cannot drop items.");
        }

        Item? held = null;
        foreach (var item in ctx.Db.Items.Holder.Filter(ctx.Sender))
        {
            if (item.LocationKind == ItemLocationKind.Hotbar && item.SlotIndex == slotIndex)
            {
                held = item;
                break;
            }
        }

        if (held is not { } dropped)
        {
            throw new System.InvalidOperationException($"No item in hotbar slot {slotIndex}.");
        }

        if (ctx.Db.Entities.EntityId.Find(player.PlayerEntityId) is not { } entity)
        {
            throw new System.InvalidOperationException("Sender has no body.");
        }

        // Drop position comes from the server's own Entities row — no client
        // coordinate parameter to trust or clamp.
        ctx.Db.Items.ItemId.Update(
            dropped with
            {
                LocationKind = ItemLocationKind.World,
                Position = entity.Position,
                Holder = default,
                SlotIndex = 0,
            }
        );
    }
}
