public static partial class Module
{
    // Single-row hazard tunables — scenarios shrink the tick and fatten the
    // damage the way VitalsConfig/HarvestConfig do, so timing-sensitive fire
    // scenarios stay short.
    [SpacetimeDB.Table(Accessor = "HazardConfigs", Public = true)]
    public partial struct HazardConfig
    {
        [PrimaryKey]
        public int Id;

        public int TickMillis;
        public float IntensityPerTick;
        public float SpreadThreshold;
        public int MaxHazards;
        public float FireDamagePerTick;
        public float FireDamageRadius;
    }
}
