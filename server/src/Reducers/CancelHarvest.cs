public static partial class Module
{
    // Movement cancels the channel (the client calls this). Deletes only the
    // sender's own row, so it's inherently safe — a no-op if none is active.
    [SpacetimeDB.Reducer]
    public static void CancelHarvest(ReducerContext ctx)
    {
        if (ctx.Db.ActiveHarvests.Identity.Find(ctx.Sender) is not null)
        {
            ctx.Db.ActiveHarvests.Identity.Delete(ctx.Sender);
        }
    }
}
