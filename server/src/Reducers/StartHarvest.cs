public static partial class Module
{
    [SpacetimeDB.Reducer]
    public static void StartHarvest(ReducerContext ctx, int nodeId)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is not { } player)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (ctx.Db.VitalsRows.Identity.Find(ctx.Sender) is not { IsDead: false })
        {
            throw new System.InvalidOperationException("Dead players cannot harvest.");
        }

        if (ctx.Db.ResourceNodes.NodeId.Find(nodeId) is not { } node)
        {
            throw new System.InvalidOperationException("No such node.");
        }

        if (node.YieldRemaining <= 0)
        {
            throw new System.InvalidOperationException("Node is depleted.");
        }

        if (ctx.Db.Entities.EntityId.Find(player.PlayerEntityId) is not { } entity)
        {
            throw new System.InvalidOperationException("Sender has no body.");
        }

        var config = GetHarvestConfig(ctx);
        var dx = entity.Position.X - node.Position.X;
        var dy = entity.Position.Y - node.Position.Y;
        if (dx * dx + dy * dy > config.HarvestRadius * config.HarvestRadius)
        {
            throw new System.InvalidOperationException("Node is out of reach.");
        }

        // Precheck a slot so the channel doesn't run to completion only to find
        // nowhere to put the yield. Completion re-checks regardless and falls
        // back to a world drop if the hotbar filled mid-channel.
        if (FindFreeHotbarSlot(ctx, ctx.Sender) is null)
        {
            throw new System.InvalidOperationException("Hotbar is full.");
        }

        var startedAt = ctx.Timestamp;
        var completesAt = startedAt + System.TimeSpan.FromMilliseconds(config.HarvestMillis);
        var row = new ActiveHarvest
        {
            Identity = ctx.Sender,
            NodeId = nodeId,
            StartedAt = startedAt,
            CompletesAt = completesAt,
            Progress = 0f,
        };

        // Re-interacting restarts the channel — one row per player.
        if (ctx.Db.ActiveHarvests.Identity.Find(ctx.Sender) is not null)
        {
            ctx.Db.ActiveHarvests.Identity.Update(row);
        }
        else
        {
            ctx.Db.ActiveHarvests.Insert(row);
        }
    }
}
