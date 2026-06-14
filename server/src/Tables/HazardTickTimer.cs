public static partial class Module
{
    // Repeating ticker that grows, spreads, and applies hazard damage — the
    // VitalsTickTimer/ChannelTickTimer pattern. One row; reschedule by replace.
    [SpacetimeDB.Table(Accessor = "HazardTickTimers", Scheduled = nameof(Module.HazardTick))]
    public partial struct HazardTickTimer
    {
        [AutoInc]
        [PrimaryKey]
        public ulong Id;

        public ScheduleAt ScheduledAt;
    }
}
