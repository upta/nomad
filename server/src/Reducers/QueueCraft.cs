public static partial class Module
{
    // Queue a craft at a bench. Ingredients are consumed up front (hotbar-first,
    // then bench input) so a queued job has already paid its cost — withdrawing
    // from the bench later can't strand the queue. The bench runs the job now if
    // idle, otherwise it waits its turn; ChannelTick advances and completes it.
    [SpacetimeDB.Reducer]
    public static void QueueCraft(ReducerContext ctx, int roomSlotIndex, RecipeId recipeId)
    {
        if (ctx.Db.Players.Identity.Find(ctx.Sender) is not { } player)
        {
            throw new System.UnauthorizedAccessException("Sender is not a known player.");
        }

        if (ctx.Db.VitalsRows.Identity.Find(ctx.Sender) is not { IsDead: false })
        {
            throw new System.InvalidOperationException("Dead players cannot craft.");
        }

        if (RecipeFor(recipeId) is not { } recipe)
        {
            throw new System.InvalidOperationException("No such recipe.");
        }

        if (ctx.Db.RoomAssignments.SlotIndex.Find(roomSlotIndex) is not { } room)
        {
            throw new System.InvalidOperationException("No room at that slot.");
        }

        if (room.RoomTypeId != recipe.Bench)
        {
            throw new System.InvalidOperationException("This bench cannot craft that recipe.");
        }

        if (!room.IsPowered)
        {
            throw new System.InvalidOperationException("The bench has no power.");
        }

        if (ctx.Db.Entities.EntityId.Find(player.PlayerEntityId) is not { } entity)
        {
            throw new System.InvalidOperationException("Sender has no body.");
        }

        // Never trust modal-open state — explicit server-side reach check against
        // the bench room slot's center (same radius as Load/Withdraw).
        var inventory = GetInventoryConfig(ctx);
        var center = SlotCenter(roomSlotIndex);
        var dx = entity.Position.X - center.X;
        var dy = entity.Position.Y - center.Y;
        if (dx * dx + dy * dy > inventory.LoadRadius * inventory.LoadRadius)
        {
            throw new System.InvalidOperationException("The bench is out of reach.");
        }

        // Reserve a distinct row per required ingredient, hotbar-first then the
        // bench's input zone. Validate every ingredient before deleting any, so a
        // partial match leaves the player's items untouched.
        var crafting = GetCraftingConfig(ctx);
        var consumed = new System.Collections.Generic.List<int>();
        foreach (var ingredient in recipe.Ingredients)
        {
            var found = FindIngredientRow(ctx, roomSlotIndex, ingredient, consumed, crafting);
            if (found is null)
            {
                throw new System.InvalidOperationException("Missing ingredient for that recipe.");
            }

            consumed.Add(found.Value);
        }

        foreach (var itemId in consumed)
        {
            ctx.Db.Items.ItemId.Delete(itemId);
        }

        var benchBusy = false;
        foreach (var job in ctx.Db.CraftingJobs.Iter())
        {
            if (job.RoomSlotIndex == roomSlotIndex && job.CompletesAt is not null)
            {
                benchBusy = true;
                break;
            }
        }

        var now = ctx.Timestamp;
        Timestamp? startedAt = benchBusy ? null : now;
        Timestamp? completesAt = benchBusy
            ? null
            : now + System.TimeSpan.FromMilliseconds(crafting.CraftMillis);

        ctx.Db.CraftingJobs.Insert(
            new CraftingJob
            {
                JobId = 0,
                RoomSlotIndex = roomSlotIndex,
                RecipeId = recipeId,
                QueuedBy = ctx.Sender,
                QueuedAt = now,
                StartedAt = startedAt,
                CompletesAt = completesAt,
                Progress = 0f,
            }
        );
    }

    private static int? FindIngredientRow(
        ReducerContext ctx,
        int roomSlotIndex,
        ItemTypeId ingredient,
        System.Collections.Generic.List<int> consumed,
        CraftingConfig crafting
    )
    {
        foreach (var item in ctx.Db.Items.Holder.Filter(ctx.Sender))
        {
            if (
                item.LocationKind == ItemLocationKind.Hotbar
                && item.ItemTypeId == ingredient
                && !consumed.Contains(item.ItemId)
            )
            {
                return item.ItemId;
            }
        }

        foreach (var item in ctx.Db.Items.Iter())
        {
            if (
                item.LocationKind == ItemLocationKind.Stored
                && item.RoomSlotIndex == roomSlotIndex
                && item.SlotIndex < crafting.BenchInputSlots
                && item.ItemTypeId == ingredient
                && !consumed.Contains(item.ItemId)
            )
            {
                return item.ItemId;
            }
        }

        return null;
    }
}
