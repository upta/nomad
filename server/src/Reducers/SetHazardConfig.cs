public static partial class Module
{
    // Tuning/validation reducer — scenarios shrink the tick and fatten damage
    // the way SetVitalsConfig/SetHarvestConfig do, so fire scenarios stay short.
    [SpacetimeDB.Reducer]
    public static void SetHazardConfig(
        ReducerContext ctx,
        int tickMillis,
        float intensityPerTick,
        float spreadThreshold,
        int maxHazards,
        float fireDamagePerTick,
        float fireDamageRadius
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

        var config = GetHazardConfig(ctx);
        ctx.Db.HazardConfigs.Id.Update(
            config with
            {
                TickMillis = tickMillis,
                IntensityPerTick = intensityPerTick,
                SpreadThreshold = spreadThreshold,
                MaxHazards = maxHazards,
                FireDamagePerTick = fireDamagePerTick,
                FireDamageRadius = fireDamageRadius,
            }
        );

        if (config.TickMillis != tickMillis)
        {
            RescheduleHazardTick(ctx, tickMillis);
        }
    }
}
