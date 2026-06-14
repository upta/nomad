public static partial class Module
{
    // Debug/validation spawner — production creatures are seeded by
    // SeedNode(Planetside); this lets scenarios drop a creature at a chosen
    // position (e.g. on the player's own cell for the contact-damage test).
    // Mirrors SpawnResourceNode.
    [SpacetimeDB.Reducer]
    public static void SpawnCreature(ReducerContext ctx, CreatureTypeId typeId, float x, float y)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is null)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (typeId == CreatureTypeId.None)
        {
            throw new System.ArgumentException("Cannot spawn a creature of type None.");
        }

        SpawnCreatureAt(ctx, typeId, new DbVector2 { X = x, Y = y }, 0);
    }
}
