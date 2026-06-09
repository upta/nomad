public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "Players", Public = true)]
    public partial struct Player
    {
        [PrimaryKey]
        public Identity Identity;
        public bool IsConnected;
        public int PlayerEntityId;
    }
}
