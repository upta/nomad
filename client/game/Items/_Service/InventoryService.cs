#nullable enable

namespace Nomad.Game.Items;

using System;
using System.Collections.Generic;
using Godot;
using SpacetimeDB.Types;

// Plain-C# view of the Items table for UI/spawner consumers. Connected mode
// mirrors the server's Items rows; test mode (no connection) is seeded
// directly so pure harnesses can drive the same consumers.
public class InventoryService
{
    private const int DefaultCargoCapacity = 12;
    private const int DefaultHotbarSlots = 4;

    private readonly Dictionary<int, HotbarItemEntry> _hotbarItems = [];
    private readonly Dictionary<int, StoredItemEntry> _storedItems = [];
    private readonly SortedDictionary<int, WorldItemEntry> _worldItems = [];
    private int _cargoCapacity = DefaultCargoCapacity;
    private DbConnection? _conn;
    private int _hotbarSlotCount = DefaultHotbarSlots;
    private int _nextTestItemId = 1;

    public event Action? Changed;

    // Test-mode mirror of the LoadItem reducer — pure harnesses bump their
    // own counters (biomass/fuel) when a deposit request fires.
    public event Action<string, int>? TestLoadRequested;

    public int CargoCapacity => _cargoCapacity;

    public int HotbarSlotCount => _hotbarSlotCount;

    // Pure client UI state — the server never reads or trusts the selection.
    public int SelectedSlot { get; private set; }

    public IReadOnlyList<string?> Slots
    {
        get
        {
            var slots = new string?[_hotbarSlotCount];
            foreach (var entry in _hotbarItems.Values)
            {
                if (entry.SlotIndex >= 0 && entry.SlotIndex < slots.Length)
                    slots[entry.SlotIndex] = entry.TypeId;
            }
            return slots;
        }
    }

    public IReadOnlyList<WorldItemEntry> WorldItems => [.. _worldItems.Values];

    public void BindConnection(DbConnection conn)
    {
        _conn = conn;

        if (conn.Db.InventoryConfigs.Id.Find(0) is { } config)
        {
            _hotbarSlotCount = config.HotbarSlots;
            _cargoCapacity = config.CargoCapacity;
        }

        foreach (var item in conn.Db.Items.Iter())
            Apply(item);

        conn.Db.Items.OnInsert += OnItemInserted;
        conn.Db.Items.OnUpdate += OnItemUpdated;
        conn.Db.Items.OnDelete += OnItemDeleted;

        Changed?.Invoke();
    }

    public int CountOf(string typeId)
    {
        var count = 0;
        foreach (var entry in _hotbarItems.Values)
        {
            if (entry.TypeId == typeId)
                count++;
        }
        return count;
    }

    public int? FirstSlotOf(string typeId)
    {
        int? first = null;
        foreach (var entry in _hotbarItems.Values)
        {
            if (entry.TypeId == typeId && (first is null || entry.SlotIndex < first))
                first = entry.SlotIndex;
        }
        return first;
    }

    public void ClearTestItems()
    {
        _worldItems.Clear();
        _hotbarItems.Clear();
        _storedItems.Clear();
        Changed?.Invoke();
    }

    public void RemoveTestWorldItem(int itemId)
    {
        if (_worldItems.Remove(itemId))
            Changed?.Invoke();
    }

    public void RequestDrop(int slotIndex, Vector2 position)
    {
        if (_conn is { } conn)
        {
            // The server reads the drop position from its own Entities row;
            // the position argument only feeds the test-mode mirror.
            conn.Reducers.DropItem(slotIndex);
            return;
        }

        foreach (var (itemId, entry) in _hotbarItems)
        {
            if (entry.SlotIndex == slotIndex)
            {
                _hotbarItems.Remove(itemId);
                _worldItems[itemId] = new WorldItemEntry(itemId, entry.TypeId, position);
                Changed?.Invoke();
                return;
            }
        }
    }

    public void RequestLoad(string typeId, int roomSlotIndex)
    {
        if (FirstSlotOf(typeId) is not { } slot)
            return;

        if (_conn is { } conn)
        {
            conn.Reducers.LoadItem(slot, roomSlotIndex);
            return;
        }

        SetTestSlot(slot, null);
        TestLoadRequested?.Invoke(typeId, roomSlotIndex);
    }

    // Storage deposit — the same LoadItem verb as RequestLoad, addressed by
    // hotbar slot because the storage branch takes any item type.
    public void RequestStore(int slotIndex, int roomSlotIndex)
    {
        if (_conn is { } conn)
        {
            conn.Reducers.LoadItem(slotIndex, roomSlotIndex);
            return;
        }

        foreach (var (itemId, entry) in _hotbarItems)
        {
            if (entry.SlotIndex != slotIndex)
                continue;

            if (FindFreeTestStoreSlot(roomSlotIndex) is not { } storeSlot)
                return;

            _hotbarItems.Remove(itemId);
            _storedItems[itemId] = new StoredItemEntry(
                itemId,
                entry.TypeId,
                roomSlotIndex,
                storeSlot
            );
            Changed?.Invoke();
            return;
        }
    }

    public void RequestWithdraw(int itemId)
    {
        if (_conn is { } conn)
        {
            conn.Reducers.WithdrawItem(itemId);
            return;
        }

        if (!_storedItems.TryGetValue(itemId, out var entry))
            return;

        if (FindFreeTestSlot() is not { } slot)
            return;

        _storedItems.Remove(itemId);
        _hotbarItems[itemId] = new HotbarItemEntry(itemId, entry.TypeId, slot);
        Changed?.Invoke();
    }

    public void RequestPickUp(int itemId)
    {
        if (_conn is { } conn)
        {
            conn.Reducers.PickUpItem(itemId);
            return;
        }

        if (!_worldItems.TryGetValue(itemId, out var entry))
            return;

        if (FindFreeTestSlot() is not { } slot)
            return;

        _worldItems.Remove(itemId);
        _hotbarItems[itemId] = new HotbarItemEntry(itemId, entry.TypeId, slot);
        Changed?.Invoke();
    }

    // Test-mode harvest yield drop: places an item in the first free hotbar
    // slot, or returns null if the hotbar is full (caller falls back to a world
    // drop, mirroring the server's ChannelTick completion).
    public int? AddTestHotbarItem(string typeId)
    {
        if (FindFreeTestSlot() is not { } slot)
            return null;

        var itemId = _nextTestItemId++;
        _hotbarItems[itemId] = new HotbarItemEntry(itemId, typeId, slot);
        Changed?.Invoke();
        return slot;
    }

    public int SeedTestStoredItem(string typeId, int roomSlot)
    {
        if (FindFreeTestStoreSlot(roomSlot) is not { } storeSlot)
            return -1;

        var itemId = _nextTestItemId++;
        _storedItems[itemId] = new StoredItemEntry(itemId, typeId, roomSlot, storeSlot);
        Changed?.Invoke();
        return itemId;
    }

    // Seeds a stored item at an explicit slot — bench zones (input vs output)
    // are slot-index ranges, so test mirrors need to place items precisely.
    public int SeedTestStoredItemAt(string typeId, int roomSlot, int slotIndex)
    {
        var itemId = _nextTestItemId++;
        _storedItems[itemId] = new StoredItemEntry(itemId, typeId, roomSlot, slotIndex);
        Changed?.Invoke();
        return itemId;
    }

    public void RemoveTestStoredItem(int itemId)
    {
        if (_storedItems.Remove(itemId))
            Changed?.Invoke();
    }

    public int SeedTestWorldItem(string typeId, Vector2 position)
    {
        var itemId = _nextTestItemId++;
        _worldItems[itemId] = new WorldItemEntry(itemId, typeId, position);
        Changed?.Invoke();
        return itemId;
    }

    public IReadOnlyList<StoredItemEntry> StoredIn(int roomSlot)
    {
        var entries = new List<StoredItemEntry>();
        foreach (var entry in _storedItems.Values)
        {
            if (entry.RoomSlot == roomSlot)
                entries.Add(entry);
        }

        entries.Sort((a, b) => a.SlotIndex.CompareTo(b.SlotIndex));
        return entries;
    }

    public void SelectSlot(int index)
    {
        var clamped = Math.Clamp(index, 0, _hotbarSlotCount - 1);
        if (clamped == SelectedSlot)
            return;

        SelectedSlot = clamped;
        Changed?.Invoke();
    }

    public void SetTestSlot(int slotIndex, string? typeId)
    {
        foreach (var (itemId, entry) in _hotbarItems)
        {
            if (entry.SlotIndex == slotIndex)
            {
                _hotbarItems.Remove(itemId);
                break;
            }
        }

        if (typeId is not null)
        {
            var itemId = _nextTestItemId++;
            _hotbarItems[itemId] = new HotbarItemEntry(itemId, typeId, slotIndex);
        }

        Changed?.Invoke();
    }

    public void Unbind()
    {
        if (_conn is null)
            return;

        _conn.Db.Items.OnInsert -= OnItemInserted;
        _conn.Db.Items.OnUpdate -= OnItemUpdated;
        _conn.Db.Items.OnDelete -= OnItemDeleted;
        _conn = null;
    }

    private void Apply(Item item)
    {
        // Location moves are row UPDATEs (World → Hotbar → Stored), not
        // deletes — rows leaving a location must be evicted here, not just
        // in OnDelete.
        if (item.LocationKind == ItemLocationKind.World)
        {
            _hotbarItems.Remove(item.ItemId);
            _storedItems.Remove(item.ItemId);
            _worldItems[item.ItemId] = new WorldItemEntry(
                item.ItemId,
                item.ItemTypeId.ToString(),
                new Vector2(item.Position.X, item.Position.Y)
            );
            return;
        }

        _worldItems.Remove(item.ItemId);

        if (item.LocationKind == ItemLocationKind.Stored)
        {
            _hotbarItems.Remove(item.ItemId);
            _storedItems[item.ItemId] = new StoredItemEntry(
                item.ItemId,
                item.ItemTypeId.ToString(),
                item.RoomSlotIndex,
                item.SlotIndex
            );
            return;
        }

        _storedItems.Remove(item.ItemId);

        if (
            item.LocationKind == ItemLocationKind.Hotbar
            && _conn?.Identity is { } me
            && item.Holder == me
        )
        {
            _hotbarItems[item.ItemId] = new HotbarItemEntry(
                item.ItemId,
                item.ItemTypeId.ToString(),
                item.SlotIndex
            );
        }
        else
        {
            _hotbarItems.Remove(item.ItemId);
        }
    }

    private int? FindFreeTestStoreSlot(int roomSlot)
    {
        var occupied = new HashSet<int>();
        foreach (var entry in _storedItems.Values)
        {
            if (entry.RoomSlot == roomSlot)
                occupied.Add(entry.SlotIndex);
        }

        for (var slot = 0; slot < _cargoCapacity; slot++)
        {
            if (!occupied.Contains(slot))
                return slot;
        }

        return null;
    }

    private int? FindFreeTestSlot()
    {
        var occupied = new HashSet<int>();
        foreach (var entry in _hotbarItems.Values)
            occupied.Add(entry.SlotIndex);

        for (var slot = 0; slot < _hotbarSlotCount; slot++)
        {
            if (!occupied.Contains(slot))
                return slot;
        }

        return null;
    }

    private void OnItemDeleted(EventContext ctx, Item item)
    {
        _worldItems.Remove(item.ItemId);
        _hotbarItems.Remove(item.ItemId);
        _storedItems.Remove(item.ItemId);
        Changed?.Invoke();
    }

    private void OnItemInserted(EventContext ctx, Item item)
    {
        Apply(item);
        Changed?.Invoke();
    }

    private void OnItemUpdated(EventContext ctx, Item oldItem, Item newItem)
    {
        Apply(newItem);
        Changed?.Invoke();
    }
}

public record HotbarItemEntry(int ItemId, string TypeId, int SlotIndex);

public record StoredItemEntry(int ItemId, string TypeId, int RoomSlot, int SlotIndex);

public record WorldItemEntry(int ItemId, string TypeId, Vector2 Position);
