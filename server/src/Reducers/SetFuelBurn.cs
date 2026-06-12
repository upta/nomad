public static partial class Module
{
    // Tuning/validation reducer — perBurn 0 disables burn entirely;
    // intervalMillis 0 keeps the current interval.
    [SpacetimeDB.Reducer]
    public static void SetFuelBurn(ReducerContext ctx, int perBurn, ulong intervalMillis)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is null)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (perBurn < 0)
        {
            throw new System.ArgumentOutOfRangeException(
                nameof(perBurn),
                "Fuel per burn cannot be negative."
            );
        }

        var grid = GetPowerGrid(ctx);
        var newInterval = intervalMillis > 0 ? intervalMillis : grid.FuelBurnMillis;
        ctx.Db.PowerGrids.Id.Update(
            grid with
            {
                FuelPerBurn = perBurn,
                FuelBurnMillis = newInterval,
            }
        );

        if (grid.FuelBurnMillis != newInterval)
        {
            RescheduleFuelBurn(ctx, newInterval);
        }

        // Toggling burn on/off changes whether a dry tank silences the
        // reactor — settle the grid against the new rule immediately.
        RecomputePowerGrid(ctx);
    }
}
