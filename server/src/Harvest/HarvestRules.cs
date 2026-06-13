public static partial class Module
{
    private const int DefaultHarvestMillis = 2000;
    private const float DefaultHarvestRadius = 96f;
    private const int DefaultTickMillis = 150;

    // Node type → harvested item mapping. The harvest channel (Task 4.2)
    // reads this at completion; lives beside the node definition the way
    // PowerDrawFor sits beside the power model.
    private static ItemTypeId YieldItemFor(ResourceNodeTypeId nodeType) =>
        nodeType switch
        {
            ResourceNodeTypeId.OreVein => ItemTypeId.RawOre,
            ResourceNodeTypeId.WreckageDebris => ItemTypeId.Scrap,
            ResourceNodeTypeId.FuelDepositNode => ItemTypeId.FuelDeposit,
            ResourceNodeTypeId.BiomassPatch => ItemTypeId.Biomass,
            _ => ItemTypeId.None,
        };

    private static HarvestConfig GetHarvestConfig(ReducerContext ctx) =>
        ctx.Db.HarvestConfigs.Id.Find(0)
        ?? ctx.Db.HarvestConfigs.Insert(
            new HarvestConfig
            {
                Id = 0,
                HarvestMillis = DefaultHarvestMillis,
                HarvestRadius = DefaultHarvestRadius,
                TickMillis = DefaultTickMillis,
            }
        );

    // The shared channel ticker is a single row; replacing it (rather than
    // updating) is the only way to change a scheduled table's interval.
    private static void RescheduleChannelTick(ReducerContext ctx, int tickMillis)
    {
        var stale = new System.Collections.Generic.List<ulong>();
        foreach (var timer in ctx.Db.ChannelTickTimers.Iter())
        {
            stale.Add(timer.Id);
        }

        foreach (var id in stale)
        {
            ctx.Db.ChannelTickTimers.Id.Delete(id);
        }

        ctx.Db.ChannelTickTimers.Insert(
            new ChannelTickTimer
            {
                Id = 0,
                ScheduledAt = new ScheduleAt.Interval(System.TimeSpan.FromMilliseconds(tickMillis)),
            }
        );
    }
}
