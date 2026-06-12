public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "ShipStoresRows", Public = true)]
    public partial struct ShipStores
    {
        [PrimaryKey]
        public int Id;
        public int Biomass;
    }
}
