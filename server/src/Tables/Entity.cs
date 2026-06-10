public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "Entities", Public = true)]
    public partial struct Entity
    {
        public Entity() { }

        [PrimaryKey]
        [AutoInc]
        public int EntityId;

        [SpacetimeDB.Index.BTree]
        public uint EntityTypeId;

        public DbVector2 Position;
        public double SenderTimestamp;
        public DbVector2 Velocity;
        public float Rotation;

        [SpacetimeDB.Index.BTree]
        public bool Active = true;
    }
}
