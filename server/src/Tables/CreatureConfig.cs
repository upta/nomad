public static partial class Module
{
    // Single-row creature tunables — scenarios shrink the tick and fatten the
    // contact damage the way HazardConfig/VitalsConfig do, so creature
    // scenarios stay short.
    [SpacetimeDB.Table(Accessor = "CreatureConfigs", Public = true)]
    public partial struct CreatureConfig
    {
        [PrimaryKey]
        public int Id;

        public int TickMillis;
        public float MoveSpeed;
        public float ChaseRange;
        public float ContactRadius;
        public float ContactDamage;
        public float MaxHealth;
    }
}
