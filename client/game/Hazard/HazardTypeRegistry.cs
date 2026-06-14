namespace Nomad.Game.Hazard;

using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class HazardTypeRegistry : Node
{
    private readonly Dictionary<string, HazardType> _byId = [];

    [Export]
    public Godot.Collections.Array<HazardType> HazardTypes { get; set; } = [];

    public IReadOnlyList<HazardType> All { get; private set; } = [];

    public override void _Ready()
    {
        LoadAll();
    }

    public HazardType? Find(string hazardId) => _byId.TryGetValue(hazardId, out var ht) ? ht : null;

    private void LoadAll()
    {
        var list = new List<HazardType>();
        foreach (var ht in HazardTypes)
        {
            _byId[ht.HazardId] = ht;
            list.Add(ht);
        }

        All = list;

        GD.Print(
            $"[HazardTypeRegistry] Loaded {list.Count} hazard types: {string.Join(", ", list.Select(h => h.HazardId))}"
        );
    }
}
