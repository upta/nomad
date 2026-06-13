namespace Nomad.Game.Harvest;

using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class ResourceNodeTypeRegistry : Node
{
    private readonly Dictionary<string, ResourceNodeType> _byId = [];

    [Export]
    public Godot.Collections.Array<ResourceNodeType> NodeTypes { get; set; } = [];

    public IReadOnlyList<ResourceNodeType> All { get; private set; } = [];

    public override void _Ready()
    {
        LoadAll();
    }

    public ResourceNodeType? Find(string nodeId) =>
        _byId.TryGetValue(nodeId, out var nt) ? nt : null;

    public ResourceNodeType GetRequired(string nodeId) =>
        Find(nodeId)
        ?? throw new KeyNotFoundException($"ResourceNodeType '{nodeId}' not registered.");

    private void LoadAll()
    {
        var list = new List<ResourceNodeType>();
        foreach (var nt in NodeTypes)
        {
            _byId[nt.NodeId] = nt;
            list.Add(nt);
        }

        All = list;

        GD.Print(
            $"[ResourceNodeTypeRegistry] Loaded {list.Count} node types: {string.Join(", ", list.Select(n => n.NodeId))}"
        );
    }
}
