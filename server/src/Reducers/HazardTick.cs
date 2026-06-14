public static partial class Module
{
    [SpacetimeDB.Reducer]
    public static void HazardTick(ReducerContext ctx, HazardTickTimer timer)
    {
        TickHazards(ctx, GetHazardConfig(ctx));
    }
}
