namespace Nomad.Game.Crafting;

using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class RecipeRegistry : Node
{
    private readonly Dictionary<string, Recipe> _byId = [];

    [Export]
    public Godot.Collections.Array<Recipe> Recipes { get; set; } = [];

    public IReadOnlyList<Recipe> All { get; private set; } = [];

    public override void _Ready()
    {
        LoadAll();
    }

    public Recipe? Find(string recipeId) => _byId.TryGetValue(recipeId, out var r) ? r : null;

    // Recipes whose bench is the given room type id — drives a bench modal's
    // recipe list.
    public IReadOnlyList<Recipe> ForBench(string benchRoomId)
    {
        var list = new List<Recipe>();
        foreach (var recipe in All)
        {
            if (recipe.BenchRoomId == benchRoomId)
                list.Add(recipe);
        }
        return list;
    }

    private void LoadAll()
    {
        var list = new List<Recipe>();
        foreach (var recipe in Recipes)
        {
            _byId[recipe.RecipeId] = recipe;
            list.Add(recipe);
        }

        All = list;

        GD.Print(
            $"[RecipeRegistry] Loaded {list.Count} recipes: {string.Join(", ", list.Select(r => r.RecipeId))}"
        );
    }
}
