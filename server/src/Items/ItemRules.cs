public static partial class Module
{
    private static InventoryConfig GetInventoryConfig(ReducerContext ctx) =>
        ctx.Db.InventoryConfigs.Id.Find(0)
        ?? ctx.Db.InventoryConfigs.Insert(
            new InventoryConfig
            {
                Id = 0,
                HotbarSlots = 4,
                // Headroom over the client's ~40px interact probe plus
                // position-sync drift; reach checks ride these, DB-tunable.
                PickupRadius = 96f,
                LoadRadius = 160f,
                CargoCapacity = 12,
            }
        );

    private static int? FindFreeHotbarSlot(ReducerContext ctx, Identity holder)
    {
        var config = GetInventoryConfig(ctx);
        var occupied = new System.Collections.Generic.HashSet<int>();
        foreach (var item in ctx.Db.Items.Holder.Filter(holder))
        {
            if (item.LocationKind == ItemLocationKind.Hotbar)
            {
                occupied.Add(item.SlotIndex);
            }
        }

        for (var slot = 0; slot < config.HotbarSlots; slot++)
        {
            if (!occupied.Contains(slot))
            {
                return slot;
            }
        }

        return null;
    }
}
