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
    }

    // Seeds the arrived node's transient content. Each node task fills in its
    // case as it lands; Quiet (the ship-in-space default and maintenance home)
    // re-seeds the placeholder harvest nodes that currently sit on the ship
    // interior.
    private static void SeedNode(ReducerContext ctx, NodeKind kind)
    {
        switch (kind)
        {
            case NodeKind.Quiet:
                ReseedResourceNodes(ctx);
                break;
            // Planetside (5.2), Wreck (5.3), TradingPost (5.4), DefenseEvent
            // (5.5) seed their own surface nodes / creatures / catalog here.
            default:
                break;
        }
    }
}
