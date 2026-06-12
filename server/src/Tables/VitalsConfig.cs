public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "VitalsConfigs", Public = true)]
    public partial struct VitalsConfig
    {
        [PrimaryKey]
        public int Id;
        public int TickMillis;
        public float OxygenDepletePerTick;
        public float OxygenRefillPerTick;
        public float SuffocationDamagePerTick;
        public float HungerDepletePerTick;
        public float StarvationDamagePerTick;
        public float SuitCapacityMultiplier;
        public float SuitSpeedFactor;
        public int RespawnBiomassCost;
    }
}
