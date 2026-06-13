public static partial class Module
{
    // The single shared ticker: advances every active channel's Progress from
    // its pinned timestamps and completes any whose CompletesAt has passed.
    // Completion is gated on real server time, so there is no module-identity
    // guard — a client invoking this gains nothing.
    [SpacetimeDB.Reducer]
    public static void ChannelTick(ReducerContext ctx, ChannelTickTimer timer)
    {
        var now = ctx.Timestamp;

        // Collect first — completing a channel deletes its row, and mutating a
        // table mid-iteration is unsafe.
        var active = new System.Collections.Generic.List<ActiveHarvest>();
        foreach (var harvest in ctx.Db.ActiveHarvests.Iter())
        {
            active.Add(harvest);
        }

        foreach (var harvest in active)
        {
            if (now.CompareTo(harvest.CompletesAt) >= 0)
            {
                CompleteHarvest(ctx, harvest);
                continue;
            }

            var totalMicros = harvest.CompletesAt.TimeDurationSince(harvest.StartedAt).Microseconds;
            var elapsedMicros = now.TimeDurationSince(harvest.StartedAt).Microseconds;
            var progress =
                totalMicros <= 0
                    ? 1f
                    : System.Math.Clamp((float)elapsedMicros / totalMicros, 0f, 1f);

            if (progress != harvest.Progress)
            {
                ctx.Db.ActiveHarvests.Identity.Update(harvest with { Progress = progress });
            }
        }
    }

    private static void CompleteHarvest(ReducerContext ctx, ActiveHarvest harvest)
    {
        // End the channel first; every failure below simply yields nothing.
        ctx.Db.ActiveHarvests.Identity.Delete(harvest.Identity);

        if (ctx.Db.VitalsRows.Identity.Find(harvest.Identity) is not { IsDead: false })
        {
            return;
        }

        if (
            ctx.Db.ResourceNodes.NodeId.Find(harvest.NodeId) is not { } node
            || node.YieldRemaining <= 0
        )
        {
            return;
        }

        if (ctx.Db.Players.Identity.Find(harvest.Identity) is not { } player)
        {
            return;
        }

        if (ctx.Db.Entities.EntityId.Find(player.PlayerEntityId) is not { } entity)
        {
            return;
        }

        // Re-check reach at completion regardless of the client's cancel —
        // bounded client-trust, same posture as "never trust modal-open state".
        var config = GetHarvestConfig(ctx);
        var dx = entity.Position.X - node.Position.X;
        var dy = entity.Position.Y - node.Position.Y;
        if (dx * dx + dy * dy > config.HarvestRadius * config.HarvestRadius)
        {
            return;
        }

        var yieldItem = YieldItemFor(node.ResourceNodeTypeId);
        if (yieldItem == ItemTypeId.None)
        {
            return;
        }

        ctx.Db.ResourceNodes.NodeId.Update(node with { YieldRemaining = node.YieldRemaining - 1 });

        // Into the hotbar if there's room; otherwise drop at the node so the
        // channel is never wasted (death-drop precedent).
        if (FindFreeHotbarSlot(ctx, harvest.Identity) is { } slot)
        {
            ctx.Db.Items.Insert(
                new Item
                {
                    ItemId = 0,
                    ItemTypeId = yieldItem,
                    LocationKind = ItemLocationKind.Hotbar,
                    Position = new DbVector2 { X = 0, Y = 0 },
                    Holder = harvest.Identity,
                    SlotIndex = slot,
                    RoomSlotIndex = -1,
                }
            );
        }
        else
        {
            ctx.Db.Items.Insert(
                new Item
                {
                    ItemId = 0,
                    ItemTypeId = yieldItem,
                    LocationKind = ItemLocationKind.World,
                    Position = node.Position,
                    Holder = default,
                    SlotIndex = 0,
                    RoomSlotIndex = -1,
                }
            );
        }
    }
}
