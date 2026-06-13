public static partial class Module
{
    // Dev/debug: wipe the shared world simulation back to the Init seed so a
    // playtester can restart a run without republishing the module. Clears all
    // items (world, hotbar, bench, cargo), crafting jobs, and harvest channels;
    // re-seeds rooms and resource nodes; restores ship stores, the power grid,
    // and every player's vitals to their seed values.
    //
    // Out of scope on purpose: player body positions (client-authoritative — the
    // client would just re-sync them back) and the *Config tunable rows /
    // scheduled ticks (infrastructure, not run state). Any known player may call
    // it — this is a dev tool, not a gameplay verb.
    [SpacetimeDB.Reducer]
    public static void ResetWorld(ReducerContext ctx)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is null)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        DeleteAllItems(ctx);
        DeleteAllCraftingJobs(ctx);
        DeleteAllActiveHarvests(ctx);

        ReseedRooms(ctx);
        ResetShipStoresToSeed(ctx);
        ResetPowerGridToSeed(ctx);
        // Re-settle powered state against the freshly reseeded rooms + grid.
        RecomputePowerGrid(ctx);
        ReseedResourceNodes(ctx);
        ResetAllVitals(ctx);
    }
}
