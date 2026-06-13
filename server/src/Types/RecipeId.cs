[SpacetimeDB.Type]
public enum RecipeId : uint
{
    None,
    FuelCell,

    // Meal's recipe rules land whole with the meals feature in Task 4.4; the
    // variant is declared now so the enum (and client bindings) are stable.
    Meal,
}
