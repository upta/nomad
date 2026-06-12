public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "VitalsTickTimers", Scheduled = nameof(Module.VitalsTick))]
    public partial struct VitalsTickTimer
    {
        [AutoInc]
        [PrimaryKey]
        public ulong Id;

        public ScheduleAt ScheduledAt;
    }
}
