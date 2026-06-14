public static partial class Module
{
    // Debug/validation: remove every creature. Mirrors ClearResourceNodes.
    [SpacetimeDB.Reducer]
    public static void ClearCreatures(ReducerContext ctx)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is null)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        DeleteAllCreatures(ctx);
    }
}
