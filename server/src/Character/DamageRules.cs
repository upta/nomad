public static partial class Module
{
    private const float DefaultMaxHealth = 100f;

    // Every damage source (debug now; suffocation, starvation, fire, creatures
    // as their features land) funnels through here so death handling stays in
    // one place.
    private static void ApplyDamage(
        ReducerContext ctx,
        Identity identity,
        float amount,
        DamageType type
    )
    {
        if (ctx.Db.VitalsRows.Identity.Find(identity) is not { } vitals)
        {
            throw new System.InvalidOperationException("No vitals row for this identity.");
        }

        if (vitals.IsDead)
        {
            return;
        }

        var current = System.Math.Max(0f, vitals.Health.Current - amount);
        var died = current <= 0f;
        ctx.Db.VitalsRows.Identity.Update(
            vitals with
            {
                Health = vitals.Health with { Current = current },
                IsDead = died,
            }
        );

        if (died)
        {
            // A corpse should show no ghost harvest progress; clear any channel
            // before the body's hotbar scatters.
            if (ctx.Db.ActiveHarvests.Identity.Find(identity) is not null)
            {
                ctx.Db.ActiveHarvests.Identity.Delete(identity);
            }

            // Death scatters the hotbar at the body. Missing player/entity rows
            // skip the drop — the damage itself must still commit.
            if (
                ctx.Db.Players.Identity.Find(identity) is { } player
                && ctx.Db.Entities.EntityId.Find(player.PlayerEntityId) is { } entity
            )
            {
                DropAllHotbarItems(ctx, identity, entity.Position);
            }
        }
    }

    private static void EnsureVitals(ReducerContext ctx, Identity identity)
    {
        if (ctx.Db.VitalsRows.Identity.Find(identity) is not null)
        {
            return;
        }

        ctx.Db.VitalsRows.Insert(
            new Vitals
            {
                Identity = identity,
                Health = new Meter { Current = DefaultMaxHealth, Max = DefaultMaxHealth },
                Oxygen = new Meter { Current = DefaultMaxOxygen, Max = DefaultMaxOxygen },
                Hunger = new Meter { Current = DefaultMaxHunger, Max = DefaultMaxHunger },
                SuitEquipped = false,
                IsDead = false,
            }
        );
    }
}
