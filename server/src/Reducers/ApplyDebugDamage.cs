public static partial class Module
{
    [SpacetimeDB.Reducer]
    public static void ApplyDebugDamage(ReducerContext ctx, float amount)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is null)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (amount <= 0f)
        {
            throw new System.ArgumentOutOfRangeException(
                nameof(amount),
                "Damage must be positive."
            );
        }

        ApplyDamage(ctx, ctx.Sender, amount, DamageType.Debug);
    }
}
