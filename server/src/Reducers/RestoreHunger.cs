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

        RestoreHungerFor(ctx, ctx.Sender, amount);
    }
}
