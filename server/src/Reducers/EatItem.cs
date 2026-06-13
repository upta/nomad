public static partial class Module
{
    // Consume a Meal from a hotbar slot to restore hunger. The restore amount is
    // server config (VitalsConfig.MealHungerRestore), never a client-supplied
    // value — the client only names which slot to eat from.
    [SpacetimeDB.Reducer]
    public static void EatItem(ReducerContext ctx, int hotbarSlotIndex)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is null)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        // RestoreHunger has no alive check — eating is a living-only action.
        if (ctx.Db.VitalsRows.Identity.Find(ctx.Sender) is not { IsDead: false })
        {
            throw new System.InvalidOperationException("Dead players cannot eat.");
        }

        Item? held = null;
        foreach (var item in ctx.Db.Items.Holder.Filter(ctx.Sender))
        {
            if (item.LocationKind == ItemLocationKind.Hotbar && item.SlotIndex == hotbarSlotIndex)
            {
                held = item;
                break;
            }
        }

        if (held is not { } meal)
        {
            throw new System.InvalidOperationException("No item in that hotbar slot.");
        }

        if (meal.ItemTypeId != ItemTypeId.Meal)
        {
            throw new System.InvalidOperationException("That item is not edible.");
        }

        ctx.Db.Items.ItemId.Delete(meal.ItemId);
        RestoreHungerFor(ctx, ctx.Sender, GetVitalsConfig(ctx).MealHungerRestore);
    }
}
