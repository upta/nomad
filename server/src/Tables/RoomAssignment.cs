public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "RoomAssignments", Public = true)]
    public partial struct RoomAssignment
    {
        [PrimaryKey]
        public int SlotIndex;
        public RoomTypeId RoomTypeId;
        public bool IsPowered;
        public bool IsPressurized;
        public bool BreakerOn;
        public float Health;
    }
}
