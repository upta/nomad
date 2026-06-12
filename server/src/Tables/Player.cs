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
    }
}
