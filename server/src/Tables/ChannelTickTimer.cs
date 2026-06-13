public static partial class Module
{
    // One shared repeating ticker advances every active channel (harvest now,
    // crafting in 4.3). A single ticker — not per-channel one-shot timers —
    // removes the stale-timer double-fire hazard and the TimerId bookkeeping.
    [SpacetimeDB.Table(Accessor = "ChannelTickTimers", Scheduled = nameof(Module.ChannelTick))]
    public partial struct ChannelTickTimer
    {
        [AutoInc]
        [PrimaryKey]
        public ulong Id;

        public ScheduleAt ScheduledAt;
    }
}
