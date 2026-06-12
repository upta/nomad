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
    private readonly SortedDictionary<int, WorldItemEntry> _worldItems = [];
    private DbConnection? _conn;
    private int _nextTestItemId = 1;

    public event Action? Changed;

    public IReadOnlyList<WorldItemEntry> WorldItems => [.. _worldItems.Values];

    public void BindConnection(DbConnection conn)
    {
        _conn = conn;

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
        Changed?.Invoke();
    }

    public void RemoveTestWorldItem(int itemId)
    {
        if (_worldItems.Remove(itemId))
            Changed?.Invoke();
    }

    public int SeedTestWorldItem(string typeId, Vector2 position)
    {
        var itemId = _nextTestItemId++;
        _worldItems[itemId] = new WorldItemEntry(itemId, typeId, position);
        Changed?.Invoke();
        return itemId;
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
        if (item.LocationKind != ItemLocationKind.World)
        {
            _worldItems.Remove(item.ItemId);
            return;
        }

        _worldItems[item.ItemId] = new WorldItemEntry(
            item.ItemId,
            item.ItemTypeId.ToString(),
            new Vector2(item.Position.X, item.Position.Y)
        );
    }

    private void OnItemDeleted(EventContext ctx, Item item)
    {
        _worldItems.Remove(item.ItemId);
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

public record WorldItemEntry(int ItemId, string TypeId, Vector2 Position);
