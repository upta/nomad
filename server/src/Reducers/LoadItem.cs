public static partial class Module
{
    [SpacetimeDB.Reducer]
    public static void LoadItem(ReducerContext ctx, int hotbarSlotIndex, int roomSlotIndex)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is not { } player)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (ctx.Db.VitalsRows.Identity.Find(ctx.Sender) is not { IsDead: false })
        {
            throw new System.InvalidOperationException("Dead players cannot load items.");
        }

        Item? held = null;
        foreach (var item in ctx.Db.Items.Holder.Filter(ctx.Sender))
        {
            if (item.LocationKind == ItemLocationKind.Hotbar && item.SlotIndex == hotbarSlotIndex)
            {
                held = item;
                break;
            }
        }

        if (held is not { } loaded)
        {
            throw new System.InvalidOperationException("No item in that hotbar slot.");
        }

        if (ctx.Db.RoomAssignments.SlotIndex.Find(roomSlotIndex) is not { } room)
        {
            throw new System.InvalidOperationException("No room at that slot.");
        }

        if (!AcceptsTankDeposit(room.RoomTypeId, loaded.ItemTypeId))
        {
            throw new System.InvalidOperationException("That room does not accept this item.");
        }

        if (ctx.Db.Entities.EntityId.Find(player.PlayerEntityId) is not { } entity)
        {
            throw new System.InvalidOperationException("Sender has no body.");
        }

        // Never trust modal-open state — explicit server-side reach check
        // against the room slot's center.
        var config = GetInventoryConfig(ctx);
        var center = SlotCenter(roomSlotIndex);
        var dx = entity.Position.X - center.X;
        var dy = entity.Position.Y - center.Y;
        if (dx * dx + dy * dy > config.LoadRadius * config.LoadRadius)
        {
            throw new System.InvalidOperationException("Machine intake is out of reach.");
        }

        ctx.Db.Items.ItemId.Delete(loaded.ItemId);

        var stores = GetShipStores(ctx);
        switch (loaded.ItemTypeId)
        {
            case ItemTypeId.Biomass:
                ctx.Db.ShipStoresRows.Id.Update(stores with { Biomass = stores.Biomass + 1 });
                break;
            case ItemTypeId.FuelCell:
                ctx.Db.ShipStoresRows.Id.Update(stores with { Fuel = stores.Fuel + 1 });
                // A deposit into a dry tank brings the reactor back online.
                RecomputePowerGrid(ctx);
                break;
        }
    }
}
