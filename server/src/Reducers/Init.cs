public static partial class Module
{
    [SpacetimeDB.Reducer(ReducerKind.Init)]
    public static void Init(ReducerContext ctx)
    {
        // Seed default room assignments for Corvette hull (7 slots).
        SeedRoom(ctx, 0, RoomTypeId.Reactor);
        SeedRoom(ctx, 1, RoomTypeId.Bridge);
        SeedRoom(ctx, 2, RoomTypeId.CloningBay);
        SeedRoom(ctx, 3, RoomTypeId.Hydroponics);
        SeedRoom(ctx, 4, RoomTypeId.Workshop);
        SeedRoom(ctx, 5, RoomTypeId.Kitchen);
        SeedRoom(ctx, 6, RoomTypeId.CargoBay);

        // The corridor network is one pressure unit riding the same table;
        // slot 7 has no hull RoomSlot, so no terminal or breaker spawns.
        SeedRoom(ctx, 7, RoomTypeId.Corridor);

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

        // Dev/test convenience until real item sources (harvesting) land in
        // Phase 4: a couple of pickupable items in the corridor west of the
        // spawn point, off every validation scenario's walk path.
        SeedWorldItem(ctx, ItemTypeId.RawOre, -96f, 0f);
        SeedWorldItem(ctx, ItemTypeId.FuelCell, -160f, 0f);
        SeedWorldItem(ctx, ItemTypeId.Biomass, -224f, 0f);

        // Placeholder harvestable nodes on the open east corridor floor, clear
        // of door lanes and the dev items. Position-agnostic — Phase 5.2 moves
        // node spawning to exterior grids unchanged.
        SeedResourceNode(ctx, ResourceNodeTypeId.OreVein, 96f, 0f, 5);
        SeedResourceNode(ctx, ResourceNodeTypeId.WreckageDebris, 192f, 0f, 5);
        SeedResourceNode(ctx, ResourceNodeTypeId.FuelDepositNode, 256f, 0f, 5);
        SeedResourceNode(ctx, ResourceNodeTypeId.BiomassPatch, 384f, 0f, 5);
    }

    private static void SeedResourceNode(
        ReducerContext ctx,
        ResourceNodeTypeId nodeType,
        float x,
        float y,
        int yieldMax
    )
    {
        ctx.Db.ResourceNodes.Insert(
            new ResourceNode
            {
                NodeId = 0,
                ResourceNodeTypeId = nodeType,
                Position = new DbVector2 { X = x, Y = y },
                YieldRemaining = yieldMax,
                YieldMax = yieldMax,
            }
        );
    }

    private static void SeedWorldItem(ReducerContext ctx, ItemTypeId itemTypeId, float x, float y)
    {
        ctx.Db.Items.Insert(
            new Item
            {
                ItemId = 0,
                ItemTypeId = itemTypeId,
                LocationKind = ItemLocationKind.World,
                Position = new DbVector2 { X = x, Y = y },
                Holder = default,
                SlotIndex = 0,
                RoomSlotIndex = -1,
            }
        );
    }

    private static void SeedRoom(ReducerContext ctx, int slotIndex, RoomTypeId roomTypeId)
    {
        ctx.Db.RoomAssignments.Insert(
            new RoomAssignment
            {
                SlotIndex = slotIndex,
                RoomTypeId = roomTypeId,
                IsPowered = true,
                IsPressurized = true,
                BreakerOn = true,
                Health = 100f,
            }
        );
    }
}
