public static partial class Module
{
    // Targeted form so living crew can clone dead crewmates from the
    // CloningModal; a dead sender may only respawn themself (the ghost's
    // single allowed interaction).
    [SpacetimeDB.Reducer]
    public static void RequestRespawn(ReducerContext ctx, Identity target)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is null)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (ctx.Db.VitalsRows.Identity.Find(target) is not { } targetVitals)
        {
            throw new System.InvalidOperationException("No vitals row for the target.");
        }

        if (!targetVitals.IsDead)
        {
            throw new System.InvalidOperationException("Target is not dead.");
        }

        if (ctx.Sender != target)
        {
            if (ctx.Db.VitalsRows.Identity.Find(ctx.Sender) is not { IsDead: false })
            {
                throw new System.InvalidOperationException(
                    "Only living crew can clone someone else."
                );
            }
        }

        RoomAssignment? cloningBay = null;
        foreach (var ra in ctx.Db.RoomAssignments.Iter())
        {
            if (ra.RoomTypeId == RoomTypeId.CloningBay)
            {
                cloningBay = ra;
                break;
            }
        }

        if (cloningBay is not { } bay)
        {
            throw new System.InvalidOperationException("No Cloning Bay assigned.");
        }

        if (!bay.IsPowered)
        {
            throw new System.InvalidOperationException("Cloning Bay is unpowered.");
        }

        var config = GetVitalsConfig(ctx);
        var stores = GetShipStores(ctx);
        if (stores.Biomass < config.RespawnBiomassCost)
        {
            throw new System.InvalidOperationException("Not enough biomass.");
        }

        ctx.Db.ShipStoresRows.Id.Update(
            stores with
            {
                Biomass = stores.Biomass - config.RespawnBiomassCost,
            }
        );

        ctx.Db.VitalsRows.Identity.Update(
            targetVitals with
            {
                Health = targetVitals.Health with { Current = targetVitals.Health.Max },
                Oxygen = targetVitals.Oxygen with { Current = targetVitals.Oxygen.Max },
                Hunger = targetVitals.Hunger with { Current = targetVitals.Hunger.Max },
                IsDead = false,
            }
        );

        if (
            ctx.Db.Players.Identity.Find(target) is { } targetPlayer
            && ctx.Db.Entities.EntityId.Find(targetPlayer.PlayerEntityId) is { } entity
        )
        {
            ctx.Db.Entities.EntityId.Update(
                entity with
                {
                    Position = SlotCenter(bay.SlotIndex),
                    Velocity = new DbVector2 { X = 0, Y = 0 },
                }
            );
        }
    }

    private static ShipStores GetShipStores(ReducerContext ctx) =>
        ctx.Db.ShipStoresRows.Id.Find(0)
        ?? ctx.Db.ShipStoresRows.Insert(
            new ShipStores
            {
                Id = 0,
                Biomass = 3,
                // Generous tank so dev/validation runs aren't starved at the
                // default two-minute burn interval.
                Fuel = 10,
            }
        );
}
