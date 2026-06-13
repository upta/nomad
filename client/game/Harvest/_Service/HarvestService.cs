#nullable enable

namespace Nomad.Game.Harvest;

using System;
using System.Collections.Generic;
using Godot;
using SpacetimeDB.Types;
using StdbResourceNode = SpacetimeDB.Types.ResourceNode;

// Plain-C# view of the ResourceNodes table for the spawner. Connected mode
// mirrors the server's rows; test mode (no connection) is seeded directly so
// pure harnesses drive the same spawner.
public class HarvestService
{
    private readonly SortedDictionary<int, ResourceNodeEntry> _nodes = [];
    private DbConnection? _conn;
    private int _nextTestNodeId = 1;

    public event Action? Changed;

    public IReadOnlyList<ResourceNodeEntry> Nodes => [.. _nodes.Values];

    public void BindConnection(DbConnection conn)
    {
        _conn = conn;

        foreach (var node in conn.Db.ResourceNodes.Iter())
            Apply(node);

        conn.Db.ResourceNodes.OnInsert += OnNodeInserted;
        conn.Db.ResourceNodes.OnUpdate += OnNodeUpdated;
        conn.Db.ResourceNodes.OnDelete += OnNodeDeleted;

        Changed?.Invoke();
    }

    public void ClearTestNodes()
    {
        _nodes.Clear();
        Changed?.Invoke();
    }

    public int SeedTestNode(string nodeTypeId, Vector2 position, int yieldMax)
    {
        var nodeId = _nextTestNodeId++;
        _nodes[nodeId] = new ResourceNodeEntry(nodeId, nodeTypeId, position, yieldMax, yieldMax);
        Changed?.Invoke();
        return nodeId;
    }

    public void SetTestYield(int nodeId, int yieldRemaining)
    {
        if (!_nodes.TryGetValue(nodeId, out var entry))
            return;

        _nodes[nodeId] = entry with { YieldRemaining = yieldRemaining };
        Changed?.Invoke();
    }

    public void Unbind()
    {
        if (_conn is null)
            return;

        _conn.Db.ResourceNodes.OnInsert -= OnNodeInserted;
        _conn.Db.ResourceNodes.OnUpdate -= OnNodeUpdated;
        _conn.Db.ResourceNodes.OnDelete -= OnNodeDeleted;
        _conn = null;
    }

    private void Apply(StdbResourceNode node)
    {
        _nodes[node.NodeId] = new ResourceNodeEntry(
            node.NodeId,
            node.ResourceNodeTypeId.ToString(),
            new Vector2(node.Position.X, node.Position.Y),
            node.YieldRemaining,
            node.YieldMax
        );
    }

    private void OnNodeDeleted(EventContext ctx, StdbResourceNode node)
    {
        _nodes.Remove(node.NodeId);
        Changed?.Invoke();
    }

    private void OnNodeInserted(EventContext ctx, StdbResourceNode node)
    {
        Apply(node);
        Changed?.Invoke();
    }

    private void OnNodeUpdated(EventContext ctx, StdbResourceNode oldNode, StdbResourceNode newNode)
    {
        Apply(newNode);
        Changed?.Invoke();
    }
}

public record ResourceNodeEntry(
    int NodeId,
    string TypeId,
    Vector2 Position,
    int YieldRemaining,
    int YieldMax
);
