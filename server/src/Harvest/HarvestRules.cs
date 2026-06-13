public static partial class Module
{
    // Node type → harvested item mapping. The harvest channel (Task 4.2)
    // reads this at completion; lives beside the node definition the way
    // PowerDrawFor sits beside the power model.
    private static ItemTypeId YieldItemFor(ResourceNodeTypeId nodeType) =>
        nodeType switch
        {
            ResourceNodeTypeId.OreVein => ItemTypeId.RawOre,
            ResourceNodeTypeId.WreckageDebris => ItemTypeId.Scrap,
            ResourceNodeTypeId.FuelDepositNode => ItemTypeId.FuelDeposit,
            ResourceNodeTypeId.BiomassPatch => ItemTypeId.Biomass,
            _ => ItemTypeId.None,
        };
}
