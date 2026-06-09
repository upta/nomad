public static partial class Module
{
    [SpacetimeDB.Reducer(ReducerKind.ClientDisconnected)]
    public static void ClientDisconnected(ReducerContext ctx)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is Player player)
        {
            ctx.Db.Players.Identity.Update(player with { IsConnected = false });
        }
    }
}
