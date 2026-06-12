public static partial class Module
{
    // Debug/validation setter — burn scenarios need the dry-tank and
    // full-tank states on demand.
    [SpacetimeDB.Reducer]
    public static void SetFuel(ReducerContext ctx, int value)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is null)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (value < 0)
        {
            throw new System.ArgumentOutOfRangeException(nameof(value), "Fuel cannot be negative.");
        }

        var stores = GetShipStores(ctx);
        ctx.Db.ShipStoresRows.Id.Update(stores with { Fuel = value });
        RecomputePowerGrid(ctx);
    }
}
