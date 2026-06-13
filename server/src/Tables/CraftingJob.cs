public static partial class Module
{
    // One row per queued or active craft at a bench. A bench runs at most one
    // active job at a time; the rest queue. Active vs queued is CompletesAt !=
    // null (nullable timestamps; null = waiting in line). ChannelTick advances
    // Progress (0→1) on active jobs from the pinned timestamps and completes
    // them when now >= CompletesAt — the same shared ticker harvest rides.
    //
    // Order the queue by (QueuedAt, JobId tiebreak), never by the AutoInc id
    // alone (project rule: AutoInc ids are not sequential).
    [SpacetimeDB.Table(Accessor = "CraftingJobs", Public = true)]
    public partial struct CraftingJob
    {
        [PrimaryKey]
        [AutoInc]
        public int JobId;

        public int RoomSlotIndex;
        public RecipeId RecipeId;
        public Identity QueuedBy;
        public Timestamp QueuedAt;
        public Timestamp? StartedAt;
        public Timestamp? CompletesAt;
        public float Progress;
    }
}
