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

        // Anchors the ship at the Quiet node and starts the repeating hazard
        // tick (fire grow/spread/proximity damage). Both no-op until a node
        // switch or an ignition happens.
        GetNodeActivity(ctx);
        RescheduleHazardTick(ctx, GetHazardConfig(ctx).TickMillis);

        // Seeds the ship's shared stores (biomass = three respawns).
        GetShipStores(ctx);

        // Seeds hotbar/storage capacities and reach radii.
        GetInventoryConfig(ctx);

        // Seeds crafting tunables and bench input/output zone sizes.
        GetCraftingConfig(ctx);

        // Seeds the Quiet node's transient content — the placeholder harvest
        // nodes on the open east corridor floor. Routed through the node
        // dispatcher so Init, ResetWorld, and SetActiveNode share one path.
        SeedNode(ctx, NodeKind.Quiet);
    }
}
