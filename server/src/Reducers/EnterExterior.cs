public static partial class Module
{
    // Cross the ship airlock onto the exterior grid. Gated on a node that has an
    // exterior (Planetside now), the sender being a known, living player, and
    // their body standing within reach of the ship airlock. Teleports the body
    // to the surface landing pad and marks them exterior — the surface is vacuum
    // (CurrentSlotIndex -1), so the vitals tick drains oxygen until they return
    // or suit up.
    [SpacetimeDB.Reducer]
    public static void EnterExterior(ReducerContext ctx)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is not { } player)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (ctx.Db.VitalsRows.Identity.Find(ctx.Sender) is not { IsDead: false })
        {
            throw new System.InvalidOperationException("Dead crew cannot use the airlock.");
        }

        if (!NodeHasExterior(GetNodeActivity(ctx).Kind))
        {
            throw new System.InvalidOperationException("This node has no exterior to cross onto.");
        }

        if (ctx.Db.Entities.EntityId.Find(player.PlayerEntityId) is not { } entity)
        {
            throw new System.InvalidOperationException("Player has no body entity.");
        }

        if (!WithinAirlockReach(entity.Position, ShipAirlock))
        {
            throw new System.InvalidOperationException("Too far from the airlock.");
        }

        ctx.Db.Entities.EntityId.Update(
            entity with
            {
                Position = ExteriorLanding,
                Velocity = new DbVector2 { X = 0, Y = 0 },
            }
        );

        ctx.Db.Players.Identity.Update(player with { InExterior = true, CurrentSlotIndex = -1 });
    }
}
