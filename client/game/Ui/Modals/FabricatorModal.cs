#nullable enable

namespace Nomad.Game.Ui;

using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Crafting;
using Nomad.Game.Items;

// Bench fabricator: a recipe list with Queue buttons, the player's hotbar (click
// to deposit an ingredient into the bench input zone), the bench input/output
// zones as separate grids (click to withdraw), and a progress ring over the
// active job. Grids update in place — a rebuild would steal keyboard focus
// (PowerRouterModal/StorageModal lesson).
[Meta(typeof(IAutoNode))]
public partial class FabricatorModal : PanelContainer, IRoomModal
{
    private readonly List<int> _inputItemIds = [];
    private readonly List<int> _outputItemIds = [];
    private readonly List<RecipeRow> _recipeRows = [];
    private bool _ringActive;
    private string _roomId = "";
    private int _roomSlotIndex = -1;

    public override void _Notification(int what) => this.Notify(what);

    [Dependency]
    private CraftingService Crafting => this.DependOn<CraftingService>();

    [Dependency]
    private InventoryService Inventory => this.DependOn<InventoryService>();

    [Dependency]
    private RecipeRegistry Recipes => this.DependOn<RecipeRegistry>();

    [Dependency]
    private ItemTypeRegistry Registry => this.DependOn<ItemTypeRegistry>();

    [Node]
    public ItemSlotGrid HotbarGrid { get; set; } = default!;

    [Node]
    public ItemSlotGrid InputGrid { get; set; } = default!;

    [Node]
    public ItemSlotGrid OutputGrid { get; set; } = default!;

    [Node]
    public RadialProgress ProgressRing { get; set; } = default!;

    [Export]
    public PackedScene RecipeRowScene { get; set; } = null!;

    [Node]
    public IVBoxContainer RecipeRows { get; set; } = default!;

    [Node]
    public ILabel TitleLabel { get; set; } = default!;

    public int RecipeRowCount => _recipeRows.Count;

    // Render-side state for validation: recipe rows + their queue-enabled flags,
    // bench zone occupancy, and the active-job ring.
    public Godot.Collections.Dictionary GetObservedState()
    {
        var recipes = new Godot.Collections.Array();
        foreach (var row in _recipeRows)
        {
            recipes.Add(
                new Godot.Collections.Dictionary
                {
                    ["label"] = row.NameLabel.Text,
                    ["ingredients"] = row.IngredientsLabel.Text,
                    ["queue_enabled"] = row.QueueEnabled,
                }
            );
        }

        return new Godot.Collections.Dictionary
        {
            ["recipe_count"] = _recipeRows.Count,
            ["recipes"] = recipes,
            ["input_count"] = InputGrid.OccupiedCount,
            ["output_count"] = OutputGrid.OccupiedCount,
            ["hotbar_count"] = HotbarGrid.OccupiedCount,
            ["ring_visible"] = ProgressRing.Visible,
            ["active"] = _ringActive,
            ["active_progress"] = Crafting.ActiveJobAt(_roomSlotIndex)?.Progress ?? 0f,
        };
    }

    public override void _ExitTree()
    {
        Crafting.Changed -= Sync;
        Inventory.Changed -= Sync;
    }

    // ModalHost calls Initialize after AddChild (OnResolved), so the room context
    // and first paint belong here — OnResolved runs before the bench slot is known.
    public void Initialize(RoomModalInfo info)
    {
        _roomId = info.RoomId;
        _roomSlotIndex = info.SlotIndex;
        TitleLabel.Text = info.Label;
        ProgressRing.Visible = false;
        BuildRecipeRows();
        Sync();
        Callable.From(FocusFirst).CallDeferred();
    }

    public void OnResolved()
    {
        Crafting.Changed += Sync;
        Inventory.Changed += Sync;
        HotbarGrid.SlotPressed += OnHotbarSlotPressed;
        InputGrid.SlotPressed += OnInputSlotPressed;
        OutputGrid.SlotPressed += OnOutputSlotPressed;
    }

    // Per-recipe queue-availability summary, exposed for validation.
    public bool QueueEnabledFor(int recipeIndex) =>
        recipeIndex >= 0
        && recipeIndex < _recipeRows.Count
        && _recipeRows[recipeIndex].QueueEnabled;

    private void BuildRecipeRows()
    {
        foreach (var row in _recipeRows)
            row.QueueFree();
        _recipeRows.Clear();

        foreach (var recipe in Recipes.ForBench(_roomId))
        {
            var row = RecipeRowScene.Instantiate<RecipeRow>();
            RecipeRows.AddChildEx(row);
            var recipeId = recipe.RecipeId;
            row.Bind(() => Crafting.RequestQueueCraft(_roomSlotIndex, recipeId));
            _recipeRows.Add(row);
        }
    }

    private void FocusFirst()
    {
        if (_recipeRows.Count > 0)
            _recipeRows[0].FocusQueue();
        else
            HotbarGrid.FocusFirstOccupied();
    }

    // Available units of an ingredient = hotbar holdings + bench input holdings.
    private int AvailableCount(string itemId)
    {
        var count = Inventory.CountOf(itemId);
        foreach (var entry in Inventory.StoredIn(_roomSlotIndex))
        {
            if (entry.SlotIndex < Crafting.BenchInputSlots && entry.TypeId == itemId)
                count++;
        }
        return count;
    }

    private bool CanQueue(Recipe recipe)
    {
        foreach (var (itemId, need) in RequiredCounts(recipe))
        {
            if (AvailableCount(itemId) < need)
                return false;
        }
        return true;
    }

    // Distinct ingredients in recipe order with their required counts.
    private List<(string ItemId, int Need)> RequiredCounts(Recipe recipe)
    {
        var order = new List<string>();
        var counts = new Dictionary<string, int>();
        foreach (var ingredient in recipe.IngredientItemIds)
        {
            if (!counts.ContainsKey(ingredient))
                order.Add(ingredient);
            counts[ingredient] = counts.TryGetValue(ingredient, out var n) ? n + 1 : 1;
        }

        var result = new List<(string, int)>();
        foreach (var itemId in order)
            result.Add((itemId, counts[itemId]));
        return result;
    }

    // "FuelDeposit 0/1   RawOre 1/1" — held vs required per ingredient so a
    // disabled Queue button reads as "you're missing FuelDeposit", not a dead
    // button with no explanation.
    private string IngredientSummary(Recipe recipe)
    {
        var parts = new List<string>();
        foreach (var (itemId, need) in RequiredCounts(recipe))
            parts.Add($"{itemId} {System.Math.Min(AvailableCount(itemId), need)}/{need}");
        return string.Join("   ", parts);
    }

    private void OnHotbarSlotPressed(int index) => Inventory.RequestStore(index, _roomSlotIndex);

    private void OnInputSlotPressed(int index)
    {
        if (index >= 0 && index < _inputItemIds.Count && _inputItemIds[index] >= 0)
            Inventory.RequestWithdraw(_inputItemIds[index]);
    }

    private void OnOutputSlotPressed(int index)
    {
        if (index >= 0 && index < _outputItemIds.Count && _outputItemIds[index] >= 0)
            Inventory.RequestWithdraw(_outputItemIds[index]);
    }

    private void Sync()
    {
        SyncRecipeRows();
        SyncHotbar();
        SyncBenchZones();
        SyncRing();
    }

    private void SyncBenchZones()
    {
        var inputSlots = Crafting.BenchInputSlots;
        var outputSlots = Crafting.BenchOutputSlots;

        var input = new ItemType?[inputSlots];
        var output = new ItemType?[outputSlots];
        _inputItemIds.Clear();
        _outputItemIds.Clear();
        for (var i = 0; i < inputSlots; i++)
            _inputItemIds.Add(-1);
        for (var i = 0; i < outputSlots; i++)
            _outputItemIds.Add(-1);

        foreach (var entry in Inventory.StoredIn(_roomSlotIndex))
        {
            if (entry.SlotIndex < inputSlots)
            {
                input[entry.SlotIndex] = Registry.Find(entry.TypeId);
                _inputItemIds[entry.SlotIndex] = entry.ItemId;
            }
            else
            {
                var outIndex = entry.SlotIndex - inputSlots;
                if (outIndex >= 0 && outIndex < outputSlots)
                {
                    output[outIndex] = Registry.Find(entry.TypeId);
                    _outputItemIds[outIndex] = entry.ItemId;
                }
            }
        }

        InputGrid.SetSlots(input);
        OutputGrid.SetSlots(output);
    }

    private void SyncHotbar()
    {
        var hotbarSlots = new List<ItemType?>();
        foreach (var typeId in Inventory.Slots)
            hotbarSlots.Add(typeId is null ? null : Registry.Find(typeId));
        HotbarGrid.SetSlots(hotbarSlots);
    }

    private void SyncRecipeRows()
    {
        var recipes = Recipes.ForBench(_roomId);
        for (var i = 0; i < _recipeRows.Count && i < recipes.Count; i++)
        {
            var recipe = recipes[i];
            _recipeRows[i].Update(recipe.Label, IngredientSummary(recipe), CanQueue(recipe));
        }
    }

    private void SyncRing()
    {
        if (Crafting.ActiveJobAt(_roomSlotIndex) is { } job)
        {
            if (!_ringActive)
            {
                ProgressRing.Visible = true;
                ProgressRing.Reset(job.Progress);
                _ringActive = true;
            }
            else
            {
                ProgressRing.SetProgress(job.Progress);
            }
            return;
        }

        ProgressRing.Visible = false;
        _ringActive = false;
    }
}
