namespace Nomad.Game.Crafting;

using Godot;

// A craftable recipe. Mirrors the server's CraftingRules catalog: which bench
// room type crafts it, the ingredient item types it consumes, and the output.
// New recipes are new .tres files wired into the RecipeRegistry array.
[GlobalClass]
public partial class Recipe : Resource
{
    // The RoomType.RoomId of the bench that crafts this (e.g. "Workshop").
    [Export]
    public string BenchRoomId { get; set; } = "";

    // ItemType.ItemId values consumed by one craft (e.g. "FuelDeposit", "RawOre").
    [Export]
    public Godot.Collections.Array<string> IngredientItemIds { get; set; } = [];

    [Export]
    public string Label { get; set; } = "";

    // The ItemType.ItemId produced (e.g. "FuelCell").
    [Export]
    public string OutputItemId { get; set; } = "";

    // Matches the server RecipeId enum name (e.g. "FuelCell").
    [Export]
    public string RecipeId { get; set; } = "";
}
