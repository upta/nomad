public static partial class Module
{
    // Cross back through the exterior airlock into the ship. Sender must be a
    // known, living player standing within reach of the landing-pad airlock.
    // Teleports the body to the interior landing cell and clears the exterior
    // flag; CurrentSlotIndex picks up the corridor slot the landing sits in so
    // the vitals tick refills oxygen again immediately (the client's RoomLocator
    // keeps it accurate as they walk on).
    [SpacetimeDB.Reducer]
    public static void EnterInterior(ReducerContext ctx)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is not { } player)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (ctx.Db.VitalsRows.Identity.Find(ctx.Sender) is not { IsDead: false })
        {
            throw new System.InvalidOperationException("Dead crew cannot use the airlock.");
        }

        if (ctx.Db.Entities.EntityId.Find(player.PlayerEntityId) is not { } entity)
        {
            throw new System.InvalidOperationException("Player has no body entity.");
        }

        if (!WithinAirlockReach(entity.Position, ExteriorLanding))
        {
            throw new System.InvalidOperationException("Too far from the airlock.");
        }

        ctx.Db.Entities.EntityId.Update(
            entity with
            {
                Position = InteriorLanding,
                Velocity = new DbVector2 { X = 0, Y = 0 },
            }
        );

        ctx.Db.Players.Identity.Update(
            player with
            {
                InExterior = false,
                CurrentSlotIndex = SlotForCell(WorldToCell(InteriorLanding)),
            }
        );
    }
}
