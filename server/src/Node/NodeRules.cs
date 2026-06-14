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
            // Quiet seeds nothing. Wreck (5.3), TradingPost (5.4), DefenseEvent
            // (5.5) seed their own surface nodes / creatures / catalog here.
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
}
