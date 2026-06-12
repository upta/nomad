public static partial class Module
{
    [SpacetimeDB.Reducer]
    public static void GridBlackoutTick(ReducerContext ctx, GridBlackoutTimer timer)
    {
        var grid = GetPowerGrid(ctx);
        if (grid.Status != GridStatus.Overload)
        {
            return;
        }

        var (demand, output) = ComputeLoad(ctx, grid);
        if (demand <= output)
        {
            // Load was shed but recompute somehow didn't run — recover.
            RecomputePowerGrid(ctx);
            return;
        }

        ctx.Db.PowerGrids.Id.Update(grid with { Status = GridStatus.Blackout });
        SetRoomsPowered(ctx, gridLive: false);
    }
}
