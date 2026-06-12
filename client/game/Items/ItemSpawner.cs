#nullable enable

namespace Nomad.Game.Items;

using System;
using System.Collections.Generic;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

// Spawns one WorldItem node per World-located item row. Items are
// free-floating entities, not hull fixtures — so this lives beside
// ShipGrid rather than inside it.
[Meta(typeof(IAutoNode))]
public partial class ItemSpawner : Node2D
{
    private readonly Dictionary<int, WorldItem> _nodes = [];
    private bool _subscribed;

    public override void _Notification(int what) => this.Notify(what);

    public event Action<int>? Interacted;

    [Dependency]
    private InventoryService Inventory => this.DependOn<InventoryService>();

    // Handed by Main/harness in OnReady — node exports don't bind across
    // scene-instance boundaries.
    public ItemTypeRegistry? Registry { get; set; }

    public int WorldItemNodeCount => _nodes.Count;

    [Export]
    public PackedScene WorldItemScene { get; set; } = null!;

    public override void _ExitTree()
    {
        if (_subscribed)
            Inventory.Changed -= Sync;
    }

    public void OnResolved()
    {
        Inventory.Changed += Sync;
        _subscribed = true;
        Sync();
    }

    private void Sync()
    {
        var live = Inventory.WorldItems;
        var liveIds = live.Select(e => e.ItemId).ToHashSet();

        foreach (var staleId in _nodes.Keys.Where(id => !liveIds.Contains(id)).ToList())
        {
            _nodes[staleId].QueueFree();
            _nodes.Remove(staleId);
        }

        foreach (var entry in live)
        {
            if (_nodes.ContainsKey(entry.ItemId))
                continue;

            if (Registry?.Find(entry.TypeId) is not { } type)
            {
                GD.PushWarning($"[ItemSpawner] No ItemType registered for '{entry.TypeId}'.");
                continue;
            }

            var node = WorldItemScene.Instantiate<WorldItem>();
            node.Position = entry.Position;
            AddChild(node);
            node.SetItem(entry.ItemId, type);
            node.Interacted += OnItemInteracted;
            _nodes[entry.ItemId] = node;
        }
    }

    private void OnItemInteracted(int itemId) => Interacted?.Invoke(itemId);
}
