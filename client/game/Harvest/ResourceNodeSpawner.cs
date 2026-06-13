#nullable enable

namespace Nomad.Game.Harvest;

using System;
using System.Collections.Generic;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

// Spawns one ResourceNode per ResourceNodes row, updating depletion in place
// on yield changes. Mirrors ItemSpawner; nodes are free-floating entities, so
// this lives beside ShipGrid rather than inside it.
[Meta(typeof(IAutoNode))]
public partial class ResourceNodeSpawner : Node2D
{
    private readonly Dictionary<int, ResourceNode> _nodes = [];
    private bool _subscribed;

    public override void _Notification(int what) => this.Notify(what);

    public event Action<int>? Interacted;

    [Dependency]
    private HarvestService Harvest => this.DependOn<HarvestService>();

    // Handed by Main/harness in OnReady — node exports don't bind across
    // scene-instance boundaries.
    public ResourceNodeTypeRegistry? Registry { get; set; }

    public int NodeCount => _nodes.Count;

    [Export]
    public PackedScene ResourceNodeScene { get; set; } = null!;

    public override void _ExitTree()
    {
        if (_subscribed)
            Harvest.Changed -= Sync;
    }

    public void OnResolved()
    {
        Harvest.Changed += Sync;
        _subscribed = true;
        Sync();
    }

    private void Sync()
    {
        var live = Harvest.Nodes;
        var liveIds = live.Select(e => e.NodeId).ToHashSet();

        foreach (var staleId in _nodes.Keys.Where(id => !liveIds.Contains(id)).ToList())
        {
            _nodes[staleId].QueueFree();
            _nodes.Remove(staleId);
        }

        foreach (var entry in live)
        {
            if (_nodes.TryGetValue(entry.NodeId, out var existing))
            {
                existing.SetYield(entry.YieldRemaining, entry.YieldMax);
                continue;
            }

            if (Registry?.Find(entry.TypeId) is not { } type)
            {
                GD.PushWarning(
                    $"[ResourceNodeSpawner] No ResourceNodeType registered for '{entry.TypeId}'."
                );
                continue;
            }

            var node = ResourceNodeScene.Instantiate<ResourceNode>();
            node.Position = entry.Position;
            AddChild(node);
            node.SetNode(entry.NodeId, type, entry.YieldRemaining, entry.YieldMax);
            node.Interacted += OnNodeInteracted;
            _nodes[entry.NodeId] = node;
        }
    }

    private void OnNodeInteracted(int nodeId) => Interacted?.Invoke(nodeId);
}
