public static partial class Module
{
    // A surface creature seeded at exterior nodes (Planetside now). The
    // CreatureTick moves it deterministically — chase the nearest exterior
    // player in range, else patrol fixed waypoints — and damages players on
    // contact via DamageType.Creature. Velocity is a client facing/interpolation
    // hint; PatrolIndex is which waypoint it heads to when not chasing.
    // Creatures are transient node content: cleared when the ship leaves.
    [SpacetimeDB.Table(Accessor = "Creatures", Public = true)]
    public partial struct Creature
    {
        [PrimaryKey]
        [AutoInc]
        public int CreatureId;

        public CreatureTypeId CreatureTypeId;
        public DbVector2 Position;
        public DbVector2 Velocity;
        public float Health;
        public int PatrolIndex;
    }
}
