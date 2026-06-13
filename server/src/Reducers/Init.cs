public static partial class Module
{
    [SpacetimeDB.Reducer(ReducerKind.Init)]
    public static void Init(ReducerContext ctx)
    {
        // Seed default room assignments for the Corvette hull (slots 0-6) plus
        // the corridor pressure unit (slot 7). Shared with ResetWorld.
        ReseedRooms(ctx);

        // Creates the PowerGrid row and settles initial powered state.
        RecomputePowerGrid(ctx);

        // Starts the repeating reactor fuel burn.
        RescheduleFuelBurn(ctx, GetPowerGrid(ctx).FuelBurnMillis);

        // Creates the VitalsConfig row and starts the repeating vitals tick.
        var vitalsConfig = GetVitalsConfig(ctx);
        RescheduleVitalsTick(ctx, vitalsConfig.TickMillis);

        // Creates the HarvestConfig row and starts the shared channel ticker
        // (harvest now, crafting in 4.3).
        RescheduleChannelTick(ctx, GetHarvestConfig(ctx).TickMillis);

        // Seeds the ship's shared stores (biomass = three respawns).
        GetShipStores(ctx);

        // Seeds hotbar/storage capacities and reach radii.
        GetInventoryConfig(ctx);

        // Seeds crafting tunables and bench input/output zone sizes.
        GetCraftingConfig(ctx);

        // Placeholder harvestable nodes on the open east corridor floor. Shared
        // with ResetWorld.
        ReseedResourceNodes(ctx);
    }
}
