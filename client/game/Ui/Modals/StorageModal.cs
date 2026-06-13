#nullable enable

namespace Nomad.Game.Ui;

using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Items;

// Dual slot grid for storage rooms: pressing an occupied hotbar slot
// deposits it (LoadItem storage branch), pressing an occupied cargo slot
// withdraws it. Grids are service-fed and update in place.
[Meta(typeof(IAutoNode))]
public partial class StorageModal : PanelContainer, IRoomModal
{
    private readonly List<int> _cargoItemIds = [];
    private int _roomSlotIndex = -1;

    public override void _Notification(int what) => this.Notify(what);

    [Dependency]
    private InventoryService Inventory => this.DependOn<InventoryService>();

    [Dependency]
    private ItemTypeRegistry Registry => this.DependOn<ItemTypeRegistry>();

    [Node]
    public ItemSlotGrid CargoGrid { get; set; } = default!;

    [Node]
    public ILabel EmptyLabel { get; set; } = default!;

    [Node]
    public ItemSlotGrid HotbarGrid { get; set; } = default!;

    [Node]
    public ILabel TitleLabel { get; set; } = default!;

    public int ShownStoredCount { get; private set; }

    public override void _ExitTree()
    {
        Inventory.Changed -= Sync;
    }

    // ModalHost calls Initialize after AddChild (which fires OnResolved), so
    // the first paint must happen here — OnResolved runs before the room slot
    // is known and would read StoredIn(-1), hiding already-stored items.
    public void Initialize(RoomModalInfo info)
    {
        _roomSlotIndex = info.SlotIndex;
        Sync();
        Callable.From(FocusFirstOccupied).CallDeferred();
    }

    public void OnResolved()
    {
        Inventory.Changed += Sync;
        HotbarGrid.SlotPressed += OnHotbarSlotPressed;
        CargoGrid.SlotPressed += OnCargoSlotPressed;
    }

    private void FocusFirstOccupied()
    {
        if (!HotbarGrid.FocusFirstOccupied())
            CargoGrid.FocusFirstOccupied();
    }

    private void OnCargoSlotPressed(int index)
    {
        if (index >= 0 && index < _cargoItemIds.Count && _cargoItemIds[index] >= 0)
            Inventory.RequestWithdraw(_cargoItemIds[index]);
    }

    private void OnHotbarSlotPressed(int index) => Inventory.RequestStore(index, _roomSlotIndex);

    private void Sync()
    {
        var hotbarSlots = new List<ItemType?>();
        foreach (var typeId in Inventory.Slots)
            hotbarSlots.Add(typeId is null ? null : Registry.Find(typeId));
        HotbarGrid.SetSlots(hotbarSlots);

        var capacity = Inventory.CargoCapacity;
        var cargoSlots = new ItemType?[capacity];
        _cargoItemIds.Clear();
        for (var i = 0; i < capacity; i++)
            _cargoItemIds.Add(-1);

        var stored = Inventory.StoredIn(_roomSlotIndex);
        foreach (var entry in stored)
        {
            if (entry.SlotIndex < 0 || entry.SlotIndex >= capacity)
                continue;

            cargoSlots[entry.SlotIndex] = Registry.Find(entry.TypeId);
            _cargoItemIds[entry.SlotIndex] = entry.ItemId;
        }
        CargoGrid.SetSlots(cargoSlots);

        ShownStoredCount = stored.Count;
        TitleLabel.Text = $"Cargo Storage ({stored.Count}/{capacity})";
        EmptyLabel.Visible = stored.Count == 0;

        // A store/withdraw empties the focused slot — re-home focus so the
        // keyboard flow survives.
        var focus = GetViewport().GuiGetFocusOwner();
        if (focus is null or ItemSlotButton { IsOccupied: false })
            Callable.From(FocusFirstOccupied).CallDeferred();
    }
}
