public static partial class Module
{
    [SpacetimeDB.Reducer]
    public static void SetReactorOutput(ReducerContext ctx, int output)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is null)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (output < 0 || output > 100)
        {
            throw new System.ArgumentOutOfRangeException(
                nameof(output),
                "Reactor output must be 0-100."
            );
        }

        var grid = GetPowerGrid(ctx);
        ctx.Db.PowerGrids.Id.Update(grid with { ReactorOutput = output });

        RecomputePowerGrid(ctx);
    }
}
