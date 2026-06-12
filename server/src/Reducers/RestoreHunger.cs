public static partial class Module
{
    // The consumption entry point: Phase 3 meal/ration items call this when
    // eaten; until then it doubles as the validation path.
    [SpacetimeDB.Reducer]
    public static void RestoreHunger(ReducerContext ctx, float amount)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is null)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (amount <= 0f)
        {
            throw new System.ArgumentOutOfRangeException(
                nameof(amount),
                "Restored amount must be positive."
            );
        }

        if (ctx.Db.VitalsRows.Identity.Find(ctx.Sender) is not { } vitals)
        {
            throw new System.InvalidOperationException("No vitals row for this identity.");
        }

        var hunger = System.Math.Min(vitals.Hunger.Max, vitals.Hunger.Current + amount);
        ctx.Db.VitalsRows.Identity.Update(
            vitals with
            {
                Hunger = vitals.Hunger with { Current = hunger },
            }
        );
    }
}
