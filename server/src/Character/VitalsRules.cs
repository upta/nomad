public static partial class Module
{
    private const float DefaultMaxHunger = 100f;
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
                // ~5 minutes from fed to starving.
                HungerDepletePerTick = 0.17f,
                StarvationDamagePerTick = 2f,
                SuitCapacityMultiplier = 2f,
                SuitSpeedFactor = 0.8f,
                RespawnBiomassCost = 1,
                // One meal restores half a full stomach — the eat-from-hotbar
                // amount, never a client-supplied value.
                MealHungerRestore = 50f,
            }
        );

    // Shared hunger restore: the RestoreHunger reducer and EatItem both land
    // here. Callers own auth/amount validation; this clamps and writes. Throws
    // if the identity has no vitals row (preserves RestoreHunger's contract).
    private static void RestoreHungerFor(ReducerContext ctx, Identity identity, float amount)
    {
        if (ctx.Db.VitalsRows.Identity.Find(identity) is not { } vitals)
        {
            throw new System.InvalidOperationException("No vitals row for this identity.");
        }

        var hunger = System.Math.Min(vitals.Hunger.Max, vitals.Hunger.Current + amount);
        ctx.Db.VitalsRows.Identity.Update(
            vitals with
            {
                Hunger = vitals.Hunger with { Current = hunger },
            }
        );
    }

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
