public static partial class Module
{
    // One channel per player (Identity PK). ChannelTick writes Progress (0→1)
    // each tick from the pinned timestamps, so the client reads it straight off
    // the row — no clock-skew math. Completion is gated on CompletesAt vs real
    // server time, which is why a client invoking the ticker gains nothing.
    [SpacetimeDB.Table(Accessor = "ActiveHarvests", Public = true)]
    public partial struct ActiveHarvest
    {
        [PrimaryKey]
        public Identity Identity;

        public int NodeId;
        public Timestamp StartedAt;
        public Timestamp CompletesAt;
        public float Progress;
    }
}
