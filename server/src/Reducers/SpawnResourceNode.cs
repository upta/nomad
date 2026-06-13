public static partial class Module
{
    // Debug/validation spawner — production nodes are Init-seeded; this lets
    // scenarios add nodes at runtime. Mirrors SpawnWorldItem.
    [SpacetimeDB.Reducer]
    public static void SpawnResourceNode(
        ReducerContext ctx,
        ResourceNodeTypeId nodeType,
        float x,
        float y,
        int yieldMax
    )
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is null)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (nodeType == ResourceNodeTypeId.None)
        {
            throw new System.ArgumentException("Cannot spawn a node of type None.");
        }

        if (yieldMax <= 0)
        {
            throw new System.ArgumentException("Node yield must be positive.");
        }

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
}
