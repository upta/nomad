public static partial class Module
{
    // Debug/validation setter — the suffocation scenario needs a near-empty
    // tank without waiting out a full real-time drain.
    [SpacetimeDB.Reducer]
    public static void SetOxygen(ReducerContext ctx, float value)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is null)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (ctx.Db.VitalsRows.Identity.Find(ctx.Sender) is not { } vitals)
        {
            throw new System.InvalidOperationException("No vitals row for this identity.");
        }

        var clamped = System.Math.Clamp(value, 0f, vitals.Oxygen.Max);
        ctx.Db.VitalsRows.Identity.Update(
            vitals with
            {
                Oxygen = vitals.Oxygen with { Current = clamped },
            }
        );
    }
}
