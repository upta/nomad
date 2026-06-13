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

        // Active crafts ride the same ticker. A job is active when its
        // CompletesAt is set (queued jobs hold null until they reach the front).
        var activeJobs = new System.Collections.Generic.List<CraftingJob>();
        foreach (var job in ctx.Db.CraftingJobs.Iter())
        {
            if (job.CompletesAt is not null)
            {
                activeJobs.Add(job);
            }
        }

        foreach (var job in activeJobs)
        {
            if (now.CompareTo(job.CompletesAt!.Value) >= 0)
            {
                CompleteCraft(ctx, job);
                continue;
            }

            var startedAt = job.StartedAt!.Value;
            var totalMicros = job.CompletesAt.Value.TimeDurationSince(startedAt).Microseconds;
            var elapsedMicros = now.TimeDurationSince(startedAt).Microseconds;
            var progress =
                totalMicros <= 0
                    ? 1f
                    : System.Math.Clamp((float)elapsedMicros / totalMicros, 0f, 1f);

            if (progress != job.Progress)
            {
                ctx.Db.CraftingJobs.JobId.Update(job with { Progress = progress });
            }
        }
    }

    private static void CompleteCraft(ReducerContext ctx, CraftingJob job)
    {
        var room = ctx.Db.RoomAssignments.SlotIndex.Find(job.RoomSlotIndex);
        var def = RecipeFor(job.RecipeId);

        // Bench gone or recipe unknown, or the slot was reassigned to a room type
        // that can't run this recipe → abandon the job. Ingredients were spent at
        // queue time, consistent with tank-deposit "the machine ate it" semantics.
        if (room is not { } bench || def is not { } recipe || bench.RoomTypeId != recipe.Bench)
        {
            ctx.Db.CraftingJobs.JobId.Delete(job.JobId);
            ActivateNextQueued(ctx, job.RoomSlotIndex);
            return;
        }

        // An unpowered bench can't finish — hold at 1.0 and retry next tick. Do
        // NOT activate the queue: this job still owns the bench.
        if (!bench.IsPowered)
        {
            if (job.Progress != 1f)
            {
                ctx.Db.CraftingJobs.JobId.Update(job with { Progress = 1f });
            }

            return;
        }

        // Output lands in the reserved output zone; if that somehow filled, fall
        // back to a world item at the bench so the craft is never lost.
        if (FindFreeBenchOutputSlot(ctx, job.RoomSlotIndex) is { } outputSlot)
        {
            ctx.Db.Items.Insert(
                new Item
                {
                    ItemId = 0,
                    ItemTypeId = recipe.Output,
                    LocationKind = ItemLocationKind.Stored,
                    Position = new DbVector2 { X = 0, Y = 0 },
                    Holder = default,
                    SlotIndex = outputSlot,
                    RoomSlotIndex = job.RoomSlotIndex,
                }
            );
        }
        else
        {
            ctx.Db.Items.Insert(
                new Item
                {
                    ItemId = 0,
                    ItemTypeId = recipe.Output,
                    LocationKind = ItemLocationKind.World,
                    Position = SlotCenter(job.RoomSlotIndex),
                    Holder = default,
                    SlotIndex = 0,
                    RoomSlotIndex = -1,
                }
            );
        }

        ctx.Db.CraftingJobs.JobId.Delete(job.JobId);
        ActivateNextQueued(ctx, job.RoomSlotIndex);
    }

    // Promote the oldest queued job at a bench to active. Order by
    // (QueuedAt, JobId tiebreak) — never the AutoInc id alone.
    private static void ActivateNextQueued(ReducerContext ctx, int roomSlotIndex)
    {
        CraftingJob? next = null;
        foreach (var job in ctx.Db.CraftingJobs.Iter())
        {
            if (job.RoomSlotIndex != roomSlotIndex || job.CompletesAt is not null)
            {
                continue;
            }

            if (
                next is not { } current
                || job.QueuedAt.CompareTo(current.QueuedAt) < 0
                || (job.QueuedAt.CompareTo(current.QueuedAt) == 0 && job.JobId < current.JobId)
            )
            {
                next = job;
            }
        }

        if (next is { } chosen)
        {
            var config = GetCraftingConfig(ctx);
            var now = ctx.Timestamp;
            ctx.Db.CraftingJobs.JobId.Update(
                chosen with
                {
                    StartedAt = now,
                    CompletesAt = now + System.TimeSpan.FromMilliseconds(config.CraftMillis),
                    Progress = 0f,
                }
            );
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
