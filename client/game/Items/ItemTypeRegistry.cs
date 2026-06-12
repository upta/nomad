namespace Nomad.Game.Items;

using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class ItemTypeRegistry : Node
{
    private readonly Dictionary<string, ItemType> _byId = [];

    [Export]
    public Godot.Collections.Array<ItemType> ItemTypes { get; set; } = [];

    public IReadOnlyList<ItemType> All { get; private set; } = [];

    public override void _Ready()
    {
        LoadAll();
    }

    public ItemType? Find(string itemId) => _byId.TryGetValue(itemId, out var it) ? it : null;

    public ItemType GetRequired(string itemId) =>
        Find(itemId) ?? throw new KeyNotFoundException($"ItemType '{itemId}' not registered.");

    private void LoadAll()
    {
        var list = new List<ItemType>();
        foreach (var it in ItemTypes)
        {
            _byId[it.ItemId] = it;
            list.Add(it);
        }

        All = list;

        GD.Print(
            $"[ItemTypeRegistry] Loaded {list.Count} item types: {string.Join(", ", list.Select(i => i.ItemId))}"
        );
    }
}
