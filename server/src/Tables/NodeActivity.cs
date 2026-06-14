public static partial class Module
{
    // Single-row source of truth for where the ship is anchored. SetActiveNode
    // (debug now, the jump target in Phase 6) flips Kind, clears the prior
    // node's transient content, and seeds the new node's. Persistent ship state
    // and ship hazards survive the switch.
    [SpacetimeDB.Table(Accessor = "NodeActivities", Public = true)]
    public partial struct NodeActivity
    {
        [PrimaryKey]
        public int Id;

        public NodeKind Kind;
        public Timestamp ArrivedAt;
    }
}
