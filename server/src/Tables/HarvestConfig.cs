public static partial class Module
{
    // Single-row harvest tunables (Id = 0), DB-driven so validation can shorten
    // the channel without recompiling (SetBlackoutGrace/VitalsConfig precedent).
    [SpacetimeDB.Table(Accessor = "HarvestConfigs", Public = true)]
    public partial struct HarvestConfig
    {
        [PrimaryKey]
        public int Id;

        public int HarvestMillis;
        public float HarvestRadius;
        public int TickMillis;
    }
}
