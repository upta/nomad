public static partial class Module
{
    // Debug/validation setter — the starvation scenario needs a near-empty
    // stomach without waiting out a real-time drain.
    [SpacetimeDB.Reducer]
    public static void SetHunger(ReducerContext ctx, float value)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is null)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (ctx.Db.VitalsRows.Identity.Find(ctx.Sender) is not { } vitals)
        {
            throw new System.InvalidOperationException("No vitals row for this identity.");
        }

        var clamped = System.Math.Clamp(value, 0f, vitals.Hunger.Max);
        ctx.Db.VitalsRows.Identity.Update(
            vitals with
            {
                Hunger = vitals.Hunger with { Current = clamped },
            }
        );
    }
}
