public static partial class Module
{
    // A positioned environmental hazard (fire now; breaches reuse the framework
    // in 5.6). Intensity ramps 0->1 on the HazardTick; once it crosses the
    // config SpreadThreshold the fire seeds an adjacent floor cell. RoomSlotIndex
    // records which room (or the corridor, slot 7) the cell sits in. Ship
    // hazards persist across node switches.
    [SpacetimeDB.Table(Accessor = "Hazards", Public = true)]
    public partial struct Hazard
    {
        [PrimaryKey]
        [AutoInc]
        public int HazardId;

        public HazardTypeId HazardTypeId;
        public DbVector2 Position;
        public float Intensity;
        public int RoomSlotIndex;
    }
}
