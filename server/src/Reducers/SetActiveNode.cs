public static partial class Module
{
    // Debug/dev node switch — the eventual jump target (Phase 6). Flips the
    // active node, clears the prior node's transient content, and seeds the new
    // node's. Ship state and ship hazards (fire) persist across the switch. Any
    // known player may call it; this is a dev tool, not a gameplay verb.
    [SpacetimeDB.Reducer]
    public static void SetActiveNode(ReducerContext ctx, NodeKind kind)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is null)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        var node = GetNodeActivity(ctx);
        ctx.Db.NodeActivities.Id.Update(node with { Kind = kind, ArrivedAt = ctx.Timestamp });

        // The surface a player was standing on no longer exists after a switch —
        // pull anyone outside back into the ship before seeding the new node.
        ReturnPlayersToInterior(ctx);

        ClearTransientNodeState(ctx);
        SeedNode(ctx, kind);
    }
}
