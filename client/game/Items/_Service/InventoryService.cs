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
    private const int DefaultHotbarSlots = 4;

    private readonly Dictionary<int, HotbarItemEntry> _hotbarItems = [];
    private readonly SortedDictionary<int, WorldItemEntry> _worldItems = [];
    private DbConnection? _conn;
    private int _hotbarSlotCount = DefaultHotbarSlots;
    private int _nextTestItemId = 1;

    public event Action? Changed;

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
            _hotbarSlotCount = config.HotbarSlots;

        foreach (var item in conn.Db.Items.Iter())
            Apply(item);

        conn.Db.Items.OnInsert += OnItemInserted;
        conn.Db.Items.OnUpdate += OnItemUpdated;
        conn.Db.Items.OnDelete += OnItemDeleted;

        Changed?.Invoke();
    }

    public void ClearTestItems()
    {
        _worldItems.Clear();
        _hotbarItems.Clear();
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

    public int SeedTestWorldItem(string typeId, Vector2 position)
    {
        var itemId = _nextTestItemId++;
        _worldItems[itemId] = new WorldItemEntry(itemId, typeId, position);
        Changed?.Invoke();
        return itemId;
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
        // Pickup is a row UPDATE (World → Hotbar), not a delete — rows that
        // leave World must be evicted here, not just in OnDelete.
        if (item.LocationKind == ItemLocationKind.World)
        {
            _hotbarItems.Remove(item.ItemId);
            _worldItems[item.ItemId] = new WorldItemEntry(
                item.ItemId,
                item.ItemTypeId.ToString(),
                new Vector2(item.Position.X, item.Position.Y)
            );
            return;
        }

        _worldItems.Remove(item.ItemId);

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

public record WorldItemEntry(int ItemId, string TypeId, Vector2 Position);
