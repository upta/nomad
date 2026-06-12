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

        var stale = new System.Collections.Generic.List<int>();
        foreach (var item in ctx.Db.Items.Iter())
        {
            stale.Add(item.ItemId);
        }

        foreach (var itemId in stale)
        {
            ctx.Db.Items.ItemId.Delete(itemId);
        }
    }
}
