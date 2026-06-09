public static partial class Module
{
    [SpacetimeDB.Reducer]
    public static void MoveEntity(ReducerContext ctx, int entityId, float x, float y)
    {
        var ownership =
            ctx.Db.EntityOwnership.EntityId.Find(entityId)
            ?? throw new System.InvalidOperationException(
                $"Entity {entityId} has no owner."
            );

        if (ownership.Owner != ctx.Sender)
        {
            throw new System.UnauthorizedAccessException(
                $"User does not own entity {entityId}."
            );
        }

        var entity =
            ctx.Db.Entities.EntityId.Find(entityId)
            ?? throw new System.InvalidOperationException(
                $"Entity {entityId} does not exist."
            );

        ctx.Db.Entities.EntityId.Update(entity with { PositionX = x, PositionY = y });
    }
}
