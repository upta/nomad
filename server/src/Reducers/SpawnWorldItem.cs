public static partial class Module
{
    // Debug/validation spawner — real item sources (harvesting, drops)
    // land with their owning features.
    [SpacetimeDB.Reducer]
    public static void SpawnWorldItem(ReducerContext ctx, ItemTypeId itemTypeId, float x, float y)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is null)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (itemTypeId == ItemTypeId.None)
        {
            throw new System.ArgumentException("Cannot spawn an item of type None.");
        }

        ctx.Db.Items.Insert(
            new Item
            {
                ItemId = 0,
                ItemTypeId = itemTypeId,
                LocationKind = ItemLocationKind.World,
                Position = new DbVector2 { X = x, Y = y },
                Holder = default,
                SlotIndex = 0,
                RoomSlotIndex = -1,
            }
        );
    }
}
