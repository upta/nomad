public static partial class Module
{
    // Debug/validation: ignite a hazard on the floor cell containing a world
    // position. Scenarios ignite at the player's own position (the proven
    // spawn-at-player pattern) so the proximity-damage check fires without flaky
    // navigation.
    [SpacetimeDB.Reducer]
    public static void IgniteHazardAt(ReducerContext ctx, HazardTypeId typeId, float x, float y)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is null)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (typeId == HazardTypeId.None)
        {
            throw new System.ArgumentException("Cannot ignite a hazard of type None.");
        }

        IgniteHazardAtCell(
            ctx,
            typeId,
            WorldToCell(new DbVector2 { X = x, Y = y }),
            GetHazardConfig(ctx).IntensityPerTick
        );
    }
}
