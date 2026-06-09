public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "Entities", Public = true)]
    public partial struct Entity
    {
        [PrimaryKey]
        [AutoInc]
        public int EntityId;
        public uint EntityTypeId;
        public float PositionX;
        public float PositionY;
    }
}
