public static partial class Module
{
    // Debug/validation setter — lets scenarios drive a node to a specific
    // remaining yield (e.g. 0 to prove the depleted-node rejection) without
    // running a full channel down. Mirrors SetFuel/SetBiomass.
    [SpacetimeDB.Reducer]
    public static void SetNodeYield(ReducerContext ctx, int nodeId, int yieldRemaining)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is null)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (yieldRemaining < 0)
        {
            throw new System.ArgumentException("Node yield cannot be negative.");
        }

        if (ctx.Db.ResourceNodes.NodeId.Find(nodeId) is not { } node)
        {
            throw new System.InvalidOperationException("No such node.");
        }

        ctx.Db.ResourceNodes.NodeId.Update(node with { YieldRemaining = yieldRemaining });
    }
}
