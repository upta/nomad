public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "Players", Public = true)]
    public partial struct Player
    {
        [PrimaryKey]
        public Identity Identity;
        public bool IsConnected;
        public int PlayerEntityId;

        // Which room slot the player's body occupies (-1 = none/vacuum).
        // Client-reported on transitions via SetPlayerRoom; the vitals tick
        // reads it to decide oxygen refill vs depletion.
        public int CurrentSlotIndex;

        // True while the player has crossed an airlock onto an exterior grid
        // (Planetside surface, Wreck, Trading station). The surface is vacuum —
        // exterior players keep CurrentSlotIndex -1 so oxygen drains — and only
        // exterior players are chased/contacted by surface creatures.
        public bool InExterior;
    }
}
