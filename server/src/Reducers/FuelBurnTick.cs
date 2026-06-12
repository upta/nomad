public static partial class Module
{
    [SpacetimeDB.Reducer]
    public static void FuelBurnTick(ReducerContext ctx, FuelBurnTimer timer)
    {
        var grid = GetPowerGrid(ctx);
        if (grid.FuelPerBurn <= 0)
        {
            return;
        }

        var reactorOnline = false;
        foreach (var ra in ctx.Db.RoomAssignments.Iter())
        {
            if (ra.RoomTypeId == RoomTypeId.Reactor && ra.BreakerOn)
            {
                reactorOnline = true;
                break;
            }
        }

        if (!reactorOnline)
        {
            return;
        }

        var stores = GetShipStores(ctx);
        if (stores.Fuel <= 0)
        {
            return;
        }

        var fuel = System.Math.Max(0, stores.Fuel - grid.FuelPerBurn);
        ctx.Db.ShipStoresRows.Id.Update(stores with { Fuel = fuel });

        if (fuel == 0)
        {
            // Dry tank: output drops to 0, sending the grid through the
            // existing overload→blackout flow.
            RecomputePowerGrid(ctx);
        }
    }
}
