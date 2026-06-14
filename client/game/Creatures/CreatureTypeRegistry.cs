namespace Nomad.Game.Creatures;

using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class CreatureTypeRegistry : Node
{
    private readonly Dictionary<string, CreatureType> _byId = [];

    [Export]
    public Godot.Collections.Array<CreatureType> CreatureTypes { get; set; } = [];

    public IReadOnlyList<CreatureType> All { get; private set; } = [];

    public override void _Ready()
    {
        LoadAll();
    }

    public CreatureType? Find(string creatureId) =>
        _byId.TryGetValue(creatureId, out var ct) ? ct : null;

    private void LoadAll()
    {
        var list = new List<CreatureType>();
        foreach (var ct in CreatureTypes)
        {
            _byId[ct.CreatureId] = ct;
            list.Add(ct);
        }

        All = list;

        GD.Print(
            $"[CreatureTypeRegistry] Loaded {list.Count} creature types: {string.Join(", ", list.Select(c => c.CreatureId))}"
        );
    }
}
