public static partial class Module
{
    // Debug/validation tunable setter — any non-positive arg keeps the current
    // value (SetHarvestConfig convention) so scenarios can shorten only the
    // craft duration without touching the bench zone sizes.
    [SpacetimeDB.Reducer]
    public static void SetCraftingConfig(
        ReducerContext ctx,
        int craftMillis,
        int benchInputSlots,
        int benchOutputSlots
    )
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is null)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        var config = GetCraftingConfig(ctx);
        ctx.Db.CraftingConfigs.Id.Update(
            config with
            {
                CraftMillis = craftMillis > 0 ? craftMillis : config.CraftMillis,
                BenchInputSlots = benchInputSlots > 0 ? benchInputSlots : config.BenchInputSlots,
                BenchOutputSlots =
                    benchOutputSlots > 0 ? benchOutputSlots : config.BenchOutputSlots,
            }
        );
    }
}
