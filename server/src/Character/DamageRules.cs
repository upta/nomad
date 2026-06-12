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
        ctx.Db.VitalsRows.Identity.Update(
            vitals with
            {
                Health = vitals.Health with { Current = current },
                IsDead = current <= 0f,
            }
        );
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
                SuitEquipped = false,
                IsDead = false,
            }
        );
    }
}
