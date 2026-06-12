public static partial class Module
{
    // Tuning/validation reducer — scenarios shrink timings the same way
    // SetBlackoutGrace does for the power grid.
    [SpacetimeDB.Reducer]
    public static void SetVitalsConfig(
        ReducerContext ctx,
        int tickMillis,
        float oxygenDepletePerTick,
        float oxygenRefillPerTick,
        float suffocationDamagePerTick,
        float hungerDepletePerTick,
        float starvationDamagePerTick
    )
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is null)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (tickMillis <= 0)
        {
            throw new System.ArgumentOutOfRangeException(
                nameof(tickMillis),
                "Tick interval must be positive."
            );
        }

        var config = GetVitalsConfig(ctx);
        ctx.Db.VitalsConfigs.Id.Update(
            config with
            {
                TickMillis = tickMillis,
                OxygenDepletePerTick = oxygenDepletePerTick,
                OxygenRefillPerTick = oxygenRefillPerTick,
                SuffocationDamagePerTick = suffocationDamagePerTick,
                HungerDepletePerTick = hungerDepletePerTick,
                StarvationDamagePerTick = starvationDamagePerTick,
            }
        );

        if (config.TickMillis != tickMillis)
        {
            RescheduleVitalsTick(ctx, tickMillis);
        }
    }
}
