public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "PowerGrids", Public = true)]
    public partial struct PowerGrid
    {
        [PrimaryKey]
        public int Id;
        public int ReactorOutput;
        public int GraceMillis;
        public GridStatus Status;
        public Timestamp BlackoutAt;
    }
}
