public static partial class Module
{
    // Mirrors the seed in GetShipStores (RequestRespawn.cs): three respawns of
    // biomass and a generous fuel tank so dev/validation runs aren't starved.
    private const int SeedBiomass = 3;
    private const int SeedFuel = 10;

    // --- Seeding (shared by Init and ResetWorld) -------------------------------

    // Delete-then-insert so this is safe both on a fresh Init (no rows yet) and
    // on a ResetWorld over a dirtied grid.
    private static void ReseedRooms(ReducerContext ctx)
    {
        var stale = new System.Collections.Generic.List<int>();
        foreach (var ra in ctx.Db.RoomAssignments.Iter())
        {
            stale.Add(ra.SlotIndex);
        }

        foreach (var slotIndex in stale)
        {
            ctx.Db.RoomAssignments.SlotIndex.Delete(slotIndex);
        }

        // Default room assignments for the Corvette hull (7 slots).
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

    // Inserts a loose World item (no holder) at a world position — the shared
    // path for scattered loot (wreck salvage) and the SpawnWorldItem debug
    // reducer.
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

    // --- Wholesale clears ------------------------------------------------------

    private static void DeleteAllItems(ReducerContext ctx)
    {
        var stale = new System.Collections.Generic.List<int>();
        foreach (var item in ctx.Db.Items.Iter())
        {
            stale.Add(item.ItemId);
        }

        foreach (var itemId in stale)
        {
            ctx.Db.Items.ItemId.Delete(itemId);
        }
    }

    // Clears loose World items that sit outside the hull — transient salvage
    // and surface drops that belong to the node, not the ship. Floor drops
    // inside a room or the corridor (SlotForCell >= 0) are on the ship and
    // survive a jump; hotbar and stored items (other LocationKinds) are never
    // touched. Used by ClearTransientNodeState on every node switch.
    private static void DeleteExteriorWorldItems(ReducerContext ctx)
    {
        var stale = new System.Collections.Generic.List<int>();
        foreach (var item in ctx.Db.Items.Iter())
        {
            if (item.LocationKind != ItemLocationKind.World)
            {
                continue;
            }

            if (SlotForCell(WorldToCell(item.Position)) < 0)
            {
                stale.Add(item.ItemId);
            }
        }

        foreach (var itemId in stale)
        {
            ctx.Db.Items.ItemId.Delete(itemId);
        }
    }

    private static void DeleteAllResourceNodes(ReducerContext ctx)
    {
        var stale = new System.Collections.Generic.List<int>();
        foreach (var node in ctx.Db.ResourceNodes.Iter())
        {
            stale.Add(node.NodeId);
        }

        foreach (var nodeId in stale)
        {
            ctx.Db.ResourceNodes.NodeId.Delete(nodeId);
        }
    }

    private static void DeleteAllCraftingJobs(ReducerContext ctx)
    {
        var stale = new System.Collections.Generic.List<int>();
        foreach (var job in ctx.Db.CraftingJobs.Iter())
        {
            stale.Add(job.JobId);
        }

        foreach (var jobId in stale)
        {
            ctx.Db.CraftingJobs.JobId.Delete(jobId);
        }
    }

    private static void DeleteAllActiveHarvests(ReducerContext ctx)
    {
        var stale = new System.Collections.Generic.List<Identity>();
        foreach (var harvest in ctx.Db.ActiveHarvests.Iter())
        {
            stale.Add(harvest.Identity);
        }

        foreach (var identity in stale)
        {
            ctx.Db.ActiveHarvests.Identity.Delete(identity);
        }
    }

    // --- Singleton resets ------------------------------------------------------

    private static void ResetShipStoresToSeed(ReducerContext ctx)
    {
        var stores = GetShipStores(ctx);
        ctx.Db.ShipStoresRows.Id.Update(stores with { Biomass = SeedBiomass, Fuel = SeedFuel });
    }

    private static void ResetPowerGridToSeed(ReducerContext ctx)
    {
        CancelBlackoutTimers(ctx);
        var grid = GetPowerGrid(ctx);
        ctx.Db.PowerGrids.Id.Update(
            grid with
            {
                ReactorOutput = DefaultReactorOutput,
                GraceMillis = DefaultGraceMillis,
                Status = GridStatus.Stable,
                FuelPerBurn = DefaultFuelPerBurn,
                FuelBurnMillis = DefaultFuelBurnMillis,
            }
        );
    }

    // Every player's vitals back to the EnsureVitals seed: full, alive, no suit.
    // Rebuilds the meters from the default maxes rather than the live ones —
    // unequipping the suit must also shrink Oxygen.Max back to base (the suit
    // doubles it), or the player keeps a suit-sized tank with no suit. Players
    // that never connected have no row and are skipped.
    private static void ResetAllVitals(ReducerContext ctx)
    {
        var rows = new System.Collections.Generic.List<Vitals>();
        foreach (var vitals in ctx.Db.VitalsRows.Iter())
        {
            rows.Add(vitals);
        }

        foreach (var vitals in rows)
        {
            ctx.Db.VitalsRows.Identity.Update(
                vitals with
                {
                    Health = new Meter { Current = DefaultMaxHealth, Max = DefaultMaxHealth },
                    Oxygen = new Meter { Current = DefaultMaxOxygen, Max = DefaultMaxOxygen },
                    Hunger = new Meter { Current = DefaultMaxHunger, Max = DefaultMaxHunger },
                    SuitEquipped = false,
                    IsDead = false,
                }
            );
        }
    }
}
