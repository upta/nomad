public static partial class Module
{
    // Single-row crafting tunables (Id = 0), DB-driven so validation can shorten
    // the craft without recompiling (HarvestConfig/VitalsConfig precedent). Bench
    // storage is one Stored stack on the bench's RoomSlotIndex split into a
    // reserved input zone [0, BenchInputSlots) and output zone
    // [BenchInputSlots, BenchInputSlots + BenchOutputSlots).
    [SpacetimeDB.Table(Accessor = "CraftingConfigs", Public = true)]
    public partial struct CraftingConfig
    {
        [PrimaryKey]
        public int Id;

        public int CraftMillis;
        public int BenchInputSlots;
        public int BenchOutputSlots;
    }
}
