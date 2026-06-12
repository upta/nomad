public static partial class Module
{
    [SpacetimeDB.Reducer]
    public static void SetSuitEquipped(ReducerContext ctx, bool equipped)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is null)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (ctx.Db.VitalsRows.Identity.Find(ctx.Sender) is not { } vitals)
        {
            throw new System.InvalidOperationException("No vitals row for this identity.");
        }

        if (vitals.SuitEquipped == equipped)
        {
            return;
        }

        var config = GetVitalsConfig(ctx);
        var max = equipped ? DefaultMaxOxygen * config.SuitCapacityMultiplier : DefaultMaxOxygen;

        ctx.Db.VitalsRows.Identity.Update(
            vitals with
            {
                SuitEquipped = equipped,
                Oxygen = new Meter
                {
                    Current = System.Math.Min(vitals.Oxygen.Current, max),
                    Max = max,
                },
            }
        );
    }
}
