public static partial class Module
{
    [SpacetimeDB.Reducer]
    public static void CreatureTick(ReducerContext ctx, CreatureTickTimer timer)
    {
        TickCreatures(ctx, GetCreatureConfig(ctx));
    }
}
