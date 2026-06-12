public static partial class Module
{
    [SpacetimeDB.Reducer(ReducerKind.ClientConnected)]
    public static void ClientConnected(ReducerContext ctx)
    {
        EnsurePlayerConnected(ctx);
        EnsureVitals(ctx, ctx.Sender);
        ActivateOrCreateOwnedEntities(ctx);
    }

    private static void EnsurePlayerConnected(ReducerContext ctx)
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
                    CurrentSlotIndex = -1,
                }
            );
        }
    }

    private static void ActivateOrCreateOwnedEntities(ReducerContext ctx)
    {
        var hasExisting = false;
        foreach (var ownership in ctx.Db.EntityOwnership.Owner.Filter(ctx.Sender))
        {
            hasExisting = true;
            ActivateEntity(ctx, ownership.EntityId);
        }

        if (!hasExisting)
        {
            CreatePlayerEntity(ctx, new DbVector2 { X = 0, Y = 0 });
        }
    }

    private static void ActivateEntity(ReducerContext ctx, int entityId)
    {
        if (ctx.Db.Entities.EntityId.Find(entityId) is Entity entity)
        {
            ctx.Db.Entities.EntityId.Update(entity with { Active = true });
        }
    }

    private static void CreatePlayerEntity(ReducerContext ctx, DbVector2 position)
    {
        var entity = ctx.Db.Entities.Insert(
            new Entity
            {
                EntityId = 0,
                EntityTypeId = (uint)EntityType.Player,
                Position = position,
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
