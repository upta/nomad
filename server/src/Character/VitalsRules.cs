public static partial class Module
{
    private const float DefaultMaxOxygen = 100f;

    private static VitalsConfig GetVitalsConfig(ReducerContext ctx) =>
        ctx.Db.VitalsConfigs.Id.Find(0)
        ?? ctx.Db.VitalsConfigs.Insert(
            new VitalsConfig
            {
                Id = 0,
                TickMillis = 500,
                // ~60s to drain a full tank in vacuum; refills much faster.
                OxygenDepletePerTick = 0.85f,
                OxygenRefillPerTick = 5f,
                // ~25s from full health to death once the tank is empty.
                SuffocationDamagePerTick = 2f,
                // Hunger arrives with Task 2.3 — zero keeps the meter inert.
                HungerDepletePerTick = 0f,
                StarvationDamagePerTick = 2f,
                SuitCapacityMultiplier = 2f,
                SuitSpeedFactor = 0.8f,
                RespawnBiomassCost = 1,
            }
        );

    // The repeating tick is a single row; replacing it (rather than updating)
    // is the only way to change the interval of a scheduled table.
    private static void RescheduleVitalsTick(ReducerContext ctx, int tickMillis)
    {
        var stale = new System.Collections.Generic.List<ulong>();
        foreach (var timer in ctx.Db.VitalsTickTimers.Iter())
        {
            stale.Add(timer.Id);
        }

        foreach (var id in stale)
        {
            ctx.Db.VitalsTickTimers.Id.Delete(id);
        }

        ctx.Db.VitalsTickTimers.Insert(
            new VitalsTickTimer
            {
                Id = 0,
                ScheduledAt = new ScheduleAt.Interval(System.TimeSpan.FromMilliseconds(tickMillis)),
            }
        );
    }
}
