public static partial class Module
{
    [SpacetimeDB.Table(
        Accessor = "GridBlackoutTimers",
        Scheduled = nameof(Module.GridBlackoutTick)
    )]
    public partial struct GridBlackoutTimer
    {
        [AutoInc]
        [PrimaryKey]
        public ulong Id;

        public ScheduleAt ScheduledAt;
    }
}
