public static partial class Module
{
    // Debug/validation reset — stdb suite isolation between scenarios.
    [SpacetimeDB.Reducer]
    public static void ClearItems(ReducerContext ctx)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is null)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        DeleteAllItems(ctx);
    }
}
