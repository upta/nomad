public static partial class Module
{
    // Walk-up extinguish: a living player standing within reach of a hazard
    // clears it. Server re-checks identity, alive, and reach regardless of the
    // client's InteractTarget (bounded client trust). Ghosts (dead) cannot.
    [SpacetimeDB.Reducer]
    public static void ExtinguishHazard(ReducerContext ctx, int hazardId)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is not { } player)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (ctx.Db.VitalsRows.Identity.Find(ctx.Sender) is not { IsDead: false })
        {
            throw new System.InvalidOperationException("Dead players cannot extinguish hazards.");
        }

        if (ctx.Db.Hazards.HazardId.Find(hazardId) is not { } hazard)
        {
            throw new System.ArgumentException("No such hazard.");
        }

        if (ctx.Db.Entities.EntityId.Find(player.PlayerEntityId) is not { } entity)
        {
            throw new System.InvalidOperationException("Player has no entity.");
        }

        var dx = entity.Position.X - hazard.Position.X;
        var dy = entity.Position.Y - hazard.Position.Y;
        if (dx * dx + dy * dy > ExtinguishReach * ExtinguishReach)
        {
            throw new System.InvalidOperationException("Too far from the hazard to extinguish.");
        }

        ctx.Db.Hazards.HazardId.Delete(hazardId);
    }
}
