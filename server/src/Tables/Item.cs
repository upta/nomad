public static partial class Module
{
    // One row per item; LocationKind discriminates which fields are
    // meaningful: Position (World), Holder + SlotIndex (Hotbar),
    // RoomSlotIndex + SlotIndex (Stored). No Quantity — 1 item = 1 slot.
    [SpacetimeDB.Table(Accessor = "Items", Public = true)]
    public partial struct Item
    {
        [PrimaryKey]
        [AutoInc]
        public int ItemId;

        public ItemTypeId ItemTypeId;
        public ItemLocationKind LocationKind;
        public DbVector2 Position;

        [SpacetimeDB.Index.BTree]
        public Identity Holder;

        public int SlotIndex;
        public int RoomSlotIndex;
    }
}
