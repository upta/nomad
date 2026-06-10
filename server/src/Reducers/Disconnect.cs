public static partial class Module
{
    [SpacetimeDB.Reducer(ReducerKind.ClientDisconnected)]
    public static void ClientDisconnected(ReducerContext ctx)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is Player player)
        {
            ctx.Db.Players.Identity.Update(player with { IsConnected = false });
        }

        foreach (var ownership in ctx.Db.EntityOwnership.Owner.Filter(ctx.Sender))
        {
            if (ctx.Db.Entities.EntityId.Find(ownership.EntityId) is Entity entity)
            {
                ctx.Db.Entities.EntityId.Update(
                    entity with
                    {
                        Active = false,
                        Velocity = new DbVector2 { X = 0, Y = 0 },
                    }
                );
            }
        }
    }
}
