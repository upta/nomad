public static partial class Module
{
    // Repeating ticker that moves creatures and applies contact damage — the
    // HazardTickTimer/VitalsTickTimer pattern. One row; reschedule by replace.
    [SpacetimeDB.Table(Accessor = "CreatureTickTimers", Scheduled = nameof(Module.CreatureTick))]
    public partial struct CreatureTickTimer
    {
        [AutoInc]
        [PrimaryKey]
        public ulong Id;

        public ScheduleAt ScheduledAt;
    }
}
