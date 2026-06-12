public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "FuelBurnTimers", Scheduled = nameof(Module.FuelBurnTick))]
    public partial struct FuelBurnTimer
    {
        [AutoInc]
        [PrimaryKey]
        public ulong Id;

        public ScheduleAt ScheduledAt;
    }
}
