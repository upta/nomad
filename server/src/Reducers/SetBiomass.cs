public static partial class Module
{
    // Debug/validation setter — the respawn-gating scenario needs the
    // biomass-exhausted rejection path on demand.
    [SpacetimeDB.Reducer]
    public static void SetBiomass(ReducerContext ctx, int value)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is null)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (value < 0)
        {
            throw new System.ArgumentOutOfRangeException(
                nameof(value),
                "Biomass cannot be negative."
            );
        }

        var stores = GetShipStores(ctx);
        ctx.Db.ShipStoresRows.Id.Update(stores with { Biomass = value });
    }
}
