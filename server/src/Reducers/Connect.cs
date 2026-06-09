public static partial class Module
{
    [SpacetimeDB.Reducer(ReducerKind.ClientConnected)]
    public static void ClientConnected(ReducerContext ctx)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is Player player)
        {
            ctx.Db.Players.Identity.Update(player with { IsConnected = true });
        }
        else
        {
            ctx.Db.Players.Insert(
                new Player
                {
                    Identity = ctx.Sender,
                    IsConnected = true,
                    PlayerEntityId = 0,
                }
            );
        }

        var hasEntity = false;
        foreach (var ownership in ctx.Db.EntityOwnership.Owner.Filter(ctx.Sender))
        {
            hasEntity = true;
        }

        if (!hasEntity)
        {
            var entity = ctx.Db.Entities.Insert(
                new Entity
                {
                    EntityId = 0,
                    EntityTypeId = (uint)EntityType.Player,
                    PositionX = 0,
                    PositionY = 0,
                }
            );

            ctx.Db.EntityOwnership.Insert(
                new EntityOwnership { EntityId = entity.EntityId, Owner = ctx.Sender }
            );

            if (ctx.Db.Players.Identity.Find(ctx.Sender) is not Player current)
                throw new System.InvalidOperationException("Player should exist after insert");
            ctx.Db.Players.Identity.Update(current with { PlayerEntityId = entity.EntityId });
        }
    }
}
