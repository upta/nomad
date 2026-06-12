public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "VitalsRows", Public = true)]
    public partial struct Vitals
    {
        [PrimaryKey]
        public Identity Identity;
        public Meter Health;
        public Meter Oxygen;
        public Meter Hunger;
        public bool SuitEquipped;
        public bool IsDead;
    }
}
