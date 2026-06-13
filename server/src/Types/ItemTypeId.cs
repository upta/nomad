[SpacetimeDB.Type]
public enum ItemTypeId : uint
{
    None,
    RawOre,
    FuelDeposit,
    Biomass,
    FuelCell,
    Scrap,
    Components,

    // Appended last to keep existing client-binding ordinals stable. The meals
    // feature (recipe + eat-from-hotbar) lands whole with this type in Task 4.4.
    Meal,
}
