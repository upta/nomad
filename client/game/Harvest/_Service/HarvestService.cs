#nullable enable

namespace Nomad.Game.Harvest;

using System;
using System.Collections.Generic;
using Godot;
using SpacetimeDB.Types;
using StdbActiveHarvest = SpacetimeDB.Types.ActiveHarvest;
using StdbResourceNode = SpacetimeDB.Types.ResourceNode;

// Plain-C# view of the ResourceNodes table for the spawner plus the local
// player's active harvest channel. Connected mode mirrors the server's rows
// (Progress read straight off the ActiveHarvest row — no clock-skew math); test
// mode (no connection) is seeded and ticked directly so pure harnesses drive the
// same spawner and ring.
public class HarvestService
{
    private readonly SortedDictionary<int, ResourceNodeEntry> _nodes = [];
    private DbConnection? _conn;
    private int? _connectedActiveNodeId;
    private float _connectedProgress;
    private int _nextTestNodeId = 1;
    private int? _testActiveNodeId;
    private float _testProgress;

    public event Action? Changed;

    // Test-mode completion hook — the harness maps the node to its yield item
    // and drops it into the InventoryService hotbar (the LoadRequested pattern).
    public event Action<int>? Harvested;

    public float ActiveHarvestProgress => _conn is not null ? _connectedProgress : _testProgress;

    public int ActiveHarvestNodeId =>
        _conn is not null ? _connectedActiveNodeId ?? -1 : _testActiveNodeId ?? -1;

    public bool HasActiveHarvest =>
        _conn is not null ? _connectedActiveNodeId is not null : _testActiveNodeId is not null;

    public IReadOnlyList<ResourceNodeEntry> Nodes => [.. _nodes.Values];

    public void BindConnection(DbConnection conn)
    {
        _conn = conn;

        foreach (var node in conn.Db.ResourceNodes.Iter())
            Apply(node);

        foreach (var harvest in conn.Db.ActiveHarvests.Iter())
            ApplyHarvest(harvest);

        conn.Db.ResourceNodes.OnInsert += OnNodeInserted;
        conn.Db.ResourceNodes.OnUpdate += OnNodeUpdated;
        conn.Db.ResourceNodes.OnDelete += OnNodeDeleted;
        conn.Db.ActiveHarvests.OnInsert += OnHarvestInserted;
        conn.Db.ActiveHarvests.OnUpdate += OnHarvestUpdated;
        conn.Db.ActiveHarvests.OnDelete += OnHarvestDeleted;

        Changed?.Invoke();
    }

    public void ClearTestNodes()
    {
        _nodes.Clear();
        _testActiveNodeId = null;
        _testProgress = 0f;
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

    public void RequestStartHarvest(int nodeId)
    {
        if (_conn is { } conn)
        {
            conn.Reducers.StartHarvest(nodeId);
            return;
        }

        // Test mirror: only start on a node that exists and still has yield.
        if (_nodes.TryGetValue(nodeId, out var entry) && entry.YieldRemaining > 0)
        {
            _testActiveNodeId = nodeId;
            _testProgress = 0f;
            Changed?.Invoke();
        }
    }

    public void RequestCancelHarvest()
    {
        if (_conn is { } conn)
        {
            conn.Reducers.CancelHarvest();

            // End the channel locally now so a moving player doesn't re-fire
            // CancelHarvest every physics frame until the server's delete echoes
            // back (the per-frame-reducer-spam guard — cf. Player.TrackCurrentRoom).
            // OnHarvestDeleted later confirms; CancelHarvest is idempotent.
            if (_connectedActiveNodeId is not null)
            {
                _connectedActiveNodeId = null;
                _connectedProgress = 0f;
                Changed?.Invoke();
            }
            return;
        }

        if (_testActiveNodeId is null)
            return;

        _testActiveNodeId = null;
        _testProgress = 0f;
        Changed?.Invoke();
    }

    // Drives the test-mode channel forward; the harness calls this each physics
    // frame while a harvest is active. On completion it decrements the node and
    // signals the harness to deposit the yield.
    public void AdvanceTestHarvest(float progressDelta)
    {
        if (_conn is not null || _testActiveNodeId is not { } nodeId)
            return;

        _testProgress += progressDelta;
        if (_testProgress < 1f)
        {
            Changed?.Invoke();
            return;
        }

        if (_nodes.TryGetValue(nodeId, out var entry) && entry.YieldRemaining > 0)
            _nodes[nodeId] = entry with { YieldRemaining = entry.YieldRemaining - 1 };

        _testActiveNodeId = null;
        _testProgress = 0f;
        Changed?.Invoke();
        Harvested?.Invoke(nodeId);
    }

    public void Unbind()
    {
        if (_conn is null)
            return;

        _conn.Db.ResourceNodes.OnInsert -= OnNodeInserted;
        _conn.Db.ResourceNodes.OnUpdate -= OnNodeUpdated;
        _conn.Db.ResourceNodes.OnDelete -= OnNodeDeleted;
        _conn.Db.ActiveHarvests.OnInsert -= OnHarvestInserted;
        _conn.Db.ActiveHarvests.OnUpdate -= OnHarvestUpdated;
        _conn.Db.ActiveHarvests.OnDelete -= OnHarvestDeleted;
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

    // Returns true only when the row belongs to the local player, so callers
    // can skip a Changed broadcast (and its full spawner ring+sync pass) for
    // every remote harvester's per-tick Progress update.
    private bool ApplyHarvest(StdbActiveHarvest harvest)
    {
        if (_conn?.Identity is not { } me || harvest.Identity != me)
            return false;

        _connectedActiveNodeId = harvest.NodeId;
        _connectedProgress = harvest.Progress;
        return true;
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

    private void OnHarvestInserted(EventContext ctx, StdbActiveHarvest harvest)
    {
        if (ApplyHarvest(harvest))
            Changed?.Invoke();
    }

    private void OnHarvestUpdated(
        EventContext ctx,
        StdbActiveHarvest oldHarvest,
        StdbActiveHarvest newHarvest
    )
    {
        if (ApplyHarvest(newHarvest))
            Changed?.Invoke();
    }

    private void OnHarvestDeleted(EventContext ctx, StdbActiveHarvest harvest)
    {
        if (_conn?.Identity is not { } me || harvest.Identity != me)
            return;

        _connectedActiveNodeId = null;
        _connectedProgress = 0f;
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
