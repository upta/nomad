public static partial class Module
{
    // Debug/validation tunable setter — any non-positive arg keeps the current
    // value (mirrors SetFuelBurn's "0 = keep" convention) so scenarios can
    // shorten only the channel duration.
    [SpacetimeDB.Reducer]
    public static void SetHarvestConfig(
        ReducerContext ctx,
        int harvestMillis,
        float harvestRadius,
        int tickMillis
    )
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is null)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        var config = GetHarvestConfig(ctx);
        var newHarvestMillis = harvestMillis > 0 ? harvestMillis : config.HarvestMillis;
        var newRadius = harvestRadius > 0 ? harvestRadius : config.HarvestRadius;
        var newTickMillis = tickMillis > 0 ? tickMillis : config.TickMillis;

        ctx.Db.HarvestConfigs.Id.Update(
            config with
            {
                HarvestMillis = newHarvestMillis,
                HarvestRadius = newRadius,
                TickMillis = newTickMillis,
            }
        );

        if (newTickMillis != config.TickMillis)
        {
            RescheduleChannelTick(ctx, newTickMillis);
        }
    }
}
