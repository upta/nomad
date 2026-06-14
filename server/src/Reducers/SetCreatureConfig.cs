public static partial class Module
{
    // Tuning/validation reducer — scenarios shrink the tick and fatten contact
    // damage the way SetHazardConfig/SetVitalsConfig do, so creature scenarios
    // stay short.
    [SpacetimeDB.Reducer]
    public static void SetCreatureConfig(
        ReducerContext ctx,
        int tickMillis,
        float moveSpeed,
        float chaseRange,
        float contactRadius,
        float contactDamage,
        float maxHealth
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

        var config = GetCreatureConfig(ctx);
        ctx.Db.CreatureConfigs.Id.Update(
            config with
            {
                TickMillis = tickMillis,
                MoveSpeed = moveSpeed,
                ChaseRange = chaseRange,
                ContactRadius = contactRadius,
                ContactDamage = contactDamage,
                MaxHealth = maxHealth,
            }
        );

        if (config.TickMillis != tickMillis)
        {
            RescheduleCreatureTick(ctx, tickMillis);
        }
    }
}
