public static partial class Module
{
    private static NodeActivity GetNodeActivity(ReducerContext ctx) =>
        ctx.Db.NodeActivities.Id.Find(0)
        ?? ctx.Db.NodeActivities.Insert(
            new NodeActivity
            {
                Id = 0,
                Kind = NodeKind.Quiet,
                ArrivedAt = ctx.Timestamp,
            }
        );

    // Clears the transient content that belongs to the node being left —
    // resource nodes now, creatures/threats/trade availability as those land in
    // 5.2+. Persistent ship state (rooms, power, pressure, vitals, stores,
    // items) and ship hazards (fire, breaches — they don't vanish on a jump)
    // are deliberately left untouched.
    private static void ClearTransientNodeState(ReducerContext ctx)
    {
        DeleteAllResourceNodes(ctx);
        DeleteAllCreatures(ctx);
        // Uncollected salvage left on an exterior grid (wreck loot, surface
        // drops) belongs to the node, not the ship — it doesn't follow a jump.
        // Interior floor drops (inside the hull) are on the ship and persist.
        DeleteExteriorWorldItems(ctx);
    }

    // Seeds the arrived node's transient content. Each node task fills in its
    // case as it lands. Quiet (the ship-in-space default and maintenance home)
    // has nothing transient — harvestable nodes live on exterior grids now, not
    // inside the hull (the 4.1 placeholders were relocated to Planetside in 5.2).
    private static void SeedNode(ReducerContext ctx, NodeKind kind)
    {
        switch (kind)
        {
            case NodeKind.Planetside:
                SeedPlanetsideNodes(ctx);
                SeedPlanetsideCreatures(ctx);
                break;
            case NodeKind.Wreck:
                SeedWreckLoot(ctx);
                SeedWreckNodes(ctx);
                SeedWreckCreatures(ctx);
                break;
            // Quiet seeds nothing. TradingPost (5.4), DefenseEvent (5.5) seed
            // their own surface nodes / creatures / catalog here.
            default:
                break;
        }
    }

    // Harvestable nodes on the Planetside surface — the exact same
    // ResourceNode rows the Quiet ship interior uses (position-agnostic per the
    // 4.1 decision), placed out on the exterior grid to the right of the
    // landing pad (ExteriorLanding sits at x=560). Cleared like any transient
    // node content on departure.
    private static void SeedPlanetsideNodes(ReducerContext ctx)
    {
        DeleteAllResourceNodes(ctx);

        SeedResourceNode(ctx, ResourceNodeTypeId.OreVein, 720f, -64f, 5);
        SeedResourceNode(ctx, ResourceNodeTypeId.OreVein, 880f, 96f, 5);
        SeedResourceNode(ctx, ResourceNodeTypeId.FuelDepositNode, 1040f, -32f, 5);
        SeedResourceNode(ctx, ResourceNodeTypeId.BiomassPatch, 1180f, 80f, 5);
    }

    // Salvage debris fields on the wreck exterior — the WreckageDebris
    // ResourceNode rows harvested for Scrap, scattered around the derelict to
    // the right of the dock (position-agnostic per the 4.1 decision). Cleared
    // like any transient node content on departure.
    private static void SeedWreckNodes(ReducerContext ctx)
    {
        SeedResourceNode(ctx, ResourceNodeTypeId.WreckageDebris, 780f, 120f, 5);
        SeedResourceNode(ctx, ResourceNodeTypeId.WreckageDebris, 1120f, -80f, 5);
    }

    // Loose salvage scattered across the wreck — World items the crew picks up
    // and hauls back to the Cargo Bay. Mostly Scrap, with a rarer Components
    // drop. No holder (LocationKind.World); DeleteExteriorWorldItems clears any
    // uncollected loot when the ship jumps away. Shared with the SpawnSalvageLoot
    // debug reducer so a playtester can re-scatter loot without re-jumping.
    private static void SeedWreckLoot(ReducerContext ctx)
    {
        SeedWorldItem(ctx, ItemTypeId.Scrap, 700f, -96f);
        SeedWorldItem(ctx, ItemTypeId.Scrap, 1000f, 96f);
        SeedWorldItem(ctx, ItemTypeId.Components, 860f, -32f);
    }
}
