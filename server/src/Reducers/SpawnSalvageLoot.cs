public static partial class Module
{
    // Debug/dev: re-scatter the wreck's salvage loot so a playtester can
    // replenish it without re-jumping. Seeds the same loose World items
    // SeedNode(Wreck) drops; uncollected loot is cleared when the ship leaves
    // the node. Any known player may call it — a dev tool, not a gameplay verb.
    [SpacetimeDB.Reducer]
    public static void SpawnSalvageLoot(ReducerContext ctx)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is null)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        SeedWreckLoot(ctx);
    }
}
