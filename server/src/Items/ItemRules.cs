public static partial class Module
{
    // Tank deposits: the machine consumes the item into a ship-stores
    // counter. Ammo joins in 5.5; the storage branch joins in 3.5.
    private static bool AcceptsTankDeposit(RoomTypeId roomTypeId, ItemTypeId itemTypeId) =>
        (roomTypeId, itemTypeId) switch
        {
            (RoomTypeId.CloningBay, ItemTypeId.Biomass) => true,
            (RoomTypeId.Reactor, ItemTypeId.FuelCell) => true,
            _ => false,
        };

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

    private static void DropAllHotbarItems(ReducerContext ctx, Identity holder, DbVector2 position)
    {
        var held = new System.Collections.Generic.List<Item>();
        foreach (var item in ctx.Db.Items.Holder.Filter(holder))
        {
            if (item.LocationKind == ItemLocationKind.Hotbar)
            {
                held.Add(item);
            }
        }

        foreach (var item in held)
        {
            ctx.Db.Items.ItemId.Update(
                item with
                {
                    LocationKind = ItemLocationKind.World,
                    // Deterministic per-slot spread keeps stacked drops
                    // individually interactable.
                    Position = new DbVector2
                    {
                        X = position.X + item.SlotIndex * 12f,
                        Y = position.Y,
                    },
                    Holder = default,
                    SlotIndex = 0,
                }
            );
        }
    }
}
