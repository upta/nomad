public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "EntityOwnership", Public = false)]
    public partial struct EntityOwnership
    {
        [PrimaryKey]
        public int EntityId;

        [SpacetimeDB.Index.BTree]
        public Identity Owner;
    }
}
