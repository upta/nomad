public static partial class Module
{
    // Debug/validation reset — stdb suite isolation between scenarios.
    [SpacetimeDB.Reducer]
    public static void ClearResourceNodes(ReducerContext ctx)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is null)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

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
}
