public static partial class Module
{
    private const int DefaultCraftMillis = 5000;
    private const int DefaultBenchInputSlots = 4;
    private const int DefaultBenchOutputSlots = 4;

    // Recipe definition: which bench room type crafts it, the ingredient item
    // types it consumes (one row per entry), and the item type it produces.
    private readonly record struct RecipeDef(
        RoomTypeId Bench,
        ItemTypeId[] Ingredients,
        ItemTypeId Output
    );

    // The recipe catalog. FuelCell crafts at the Workshop; Meal crafts at the
    // Kitchen. Add new recipes here and to AllRecipes — BenchAcceptsType /
    // IsBench derive from this catalog the way PowerDrawFor derives the power
    // model.
    private static readonly RecipeId[] AllRecipes = [RecipeId.FuelCell, RecipeId.Meal];

    private static RecipeDef? RecipeFor(RecipeId recipeId) =>
        recipeId switch
        {
            RecipeId.FuelCell => new RecipeDef(
                RoomTypeId.Workshop,
                [ItemTypeId.FuelDeposit, ItemTypeId.RawOre],
                ItemTypeId.FuelCell
            ),
            RecipeId.Meal => new RecipeDef(
                RoomTypeId.Kitchen,
                [ItemTypeId.Biomass],
                ItemTypeId.Meal
            ),
            _ => null,
        };

    // A room is a bench if any catalog recipe crafts there.
    private static bool IsBench(RoomTypeId roomTypeId)
    {
        foreach (var recipeId in AllRecipes)
        {
            if (RecipeFor(recipeId) is { } def && def.Bench == roomTypeId)
            {
                return true;
            }
        }

        return false;
    }

    // A bench accepts an item type into its input zone only if some recipe at
    // that bench uses it as an ingredient.
    private static bool BenchAcceptsType(RoomTypeId roomTypeId, ItemTypeId itemTypeId)
    {
        foreach (var recipeId in AllRecipes)
        {
            if (RecipeFor(recipeId) is not { } def || def.Bench != roomTypeId)
            {
                continue;
            }

            foreach (var ingredient in def.Ingredients)
            {
                if (ingredient == itemTypeId)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static CraftingConfig GetCraftingConfig(ReducerContext ctx) =>
        ctx.Db.CraftingConfigs.Id.Find(0)
        ?? ctx.Db.CraftingConfigs.Insert(
            new CraftingConfig
            {
                Id = 0,
                CraftMillis = DefaultCraftMillis,
                BenchInputSlots = DefaultBenchInputSlots,
                BenchOutputSlots = DefaultBenchOutputSlots,
            }
        );

    private static System.Collections.Generic.HashSet<int> OccupiedBenchSlots(
        ReducerContext ctx,
        int roomSlot
    )
    {
        var occupied = new System.Collections.Generic.HashSet<int>();
        foreach (var item in ctx.Db.Items.Iter())
        {
            if (item.LocationKind == ItemLocationKind.Stored && item.RoomSlotIndex == roomSlot)
            {
                occupied.Add(item.SlotIndex);
            }
        }

        return occupied;
    }

    // Deposits land in the input zone [0, BenchInputSlots).
    private static int? FindFreeBenchInputSlot(ReducerContext ctx, int roomSlot)
    {
        var config = GetCraftingConfig(ctx);
        var occupied = OccupiedBenchSlots(ctx, roomSlot);
        for (var slot = 0; slot < config.BenchInputSlots; slot++)
        {
            if (!occupied.Contains(slot))
            {
                return slot;
            }
        }

        return null;
    }

    // Completed crafts land in the reserved output zone
    // [BenchInputSlots, BenchInputSlots + BenchOutputSlots) — reserving the
    // space up front means a finished craft always has a home, no full-store race.
    private static int? FindFreeBenchOutputSlot(ReducerContext ctx, int roomSlot)
    {
        var config = GetCraftingConfig(ctx);
        var start = config.BenchInputSlots;
        var end = config.BenchInputSlots + config.BenchOutputSlots;
        var occupied = OccupiedBenchSlots(ctx, roomSlot);
        for (var slot = start; slot < end; slot++)
        {
            if (!occupied.Contains(slot))
            {
                return slot;
            }
        }

        return null;
    }
}
