public static partial class Module
{
    // A harvestable node with a finite yield. Position-agnostic: seeded on
    // the ship interior now, relocated to exterior grids in Phase 5.2 with no
    // schema change. Harvesting (Task 4.2) decrements YieldRemaining.
    [SpacetimeDB.Table(Accessor = "ResourceNodes", Public = true)]
    public partial struct ResourceNode
    {
        [PrimaryKey]
        [AutoInc]
        public int NodeId;

        public ResourceNodeTypeId ResourceNodeTypeId;
        public DbVector2 Position;
        public int YieldRemaining;
        public int YieldMax;
    }
}
