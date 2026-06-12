public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "InventoryConfigs", Public = true)]
    public partial struct InventoryConfig
    {
        [PrimaryKey]
        public int Id;
        public int HotbarSlots;
        public float PickupRadius;
        public float LoadRadius;
        public int CargoCapacity;
    }
}
