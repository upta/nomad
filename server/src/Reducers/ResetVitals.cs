public static partial class Module
{
    // Validation/debug reset; RequestRespawn (Task 2.4) is the real revive path.
    [SpacetimeDB.Reducer]
    public static void ResetVitals(ReducerContext ctx)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is null)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (ctx.Db.VitalsRows.Identity.Find(ctx.Sender) is not { } vitals)
        {
            throw new System.InvalidOperationException("No vitals row for this identity.");
        }

        ctx.Db.VitalsRows.Identity.Update(
            vitals with
            {
                Health = vitals.Health with { Current = vitals.Health.Max },
                IsDead = false,
            }
        );
    }
}
