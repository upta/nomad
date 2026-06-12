public static partial class Module
{
    [SpacetimeDB.Reducer]
    public static void SetBlackoutGrace(ReducerContext ctx, int graceMillis)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is null)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (graceMillis <= 0)
        {
            throw new System.ArgumentOutOfRangeException(
                nameof(graceMillis),
                "Blackout grace must be positive."
            );
        }

        var grid = GetPowerGrid(ctx);
        ctx.Db.PowerGrids.Id.Update(grid with { GraceMillis = graceMillis });
    }
}
