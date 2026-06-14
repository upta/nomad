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

        SeedWorldItem(ctx, itemTypeId, x, y);
    }
}
