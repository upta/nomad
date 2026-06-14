public static partial class Module
{
    private const int DefaultHazardTickMillis = 600;
    private const float DefaultIntensityPerTick = 0.15f;
    private const float DefaultSpreadThreshold = 0.6f;
    private const int DefaultMaxHazards = 24;
    private const float DefaultFireDamagePerTick = 3f;
    private const float DefaultFireDamageRadius = 40f;

    // Intensity is normalized 0->1; it caps here and SpreadThreshold/rendering
    // read against it. Not a config field — it's the meaning of the scale.
    private const float MaxHazardIntensity = 1f;

    // Walk-up extinguish reach (~2.25 tiles): forgiving enough for the
    // spawn-at-player validation pattern, tight enough to demand standing at the
    // fire. Server-checked regardless of the client's InteractTarget.
    private const float ExtinguishReach = 72f;

    private static HazardConfig GetHazardConfig(ReducerContext ctx) =>
        ctx.Db.HazardConfigs.Id.Find(0)
        ?? ctx.Db.HazardConfigs.Insert(
            new HazardConfig
            {
                Id = 0,
                TickMillis = DefaultHazardTickMillis,
                IntensityPerTick = DefaultIntensityPerTick,
                SpreadThreshold = DefaultSpreadThreshold,
                MaxHazards = DefaultMaxHazards,
                FireDamagePerTick = DefaultFireDamagePerTick,
                FireDamageRadius = DefaultFireDamageRadius,
            }
        );

    // The repeating hazard ticker is a single row; replacing it (not updating)
    // is the only way to change a scheduled table's interval.
    private static void RescheduleHazardTick(ReducerContext ctx, int tickMillis)
    {
        var stale = new System.Collections.Generic.List<ulong>();
        foreach (var timer in ctx.Db.HazardTickTimers.Iter())
        {
            stale.Add(timer.Id);
        }

        foreach (var id in stale)
        {
            ctx.Db.HazardTickTimers.Id.Delete(id);
        }

        ctx.Db.HazardTickTimers.Insert(
            new HazardTickTimer
            {
                Id = 0,
                ScheduledAt = new ScheduleAt.Interval(System.TimeSpan.FromMilliseconds(tickMillis)),
            }
        );
    }

    // Ignites a hazard on a floor cell, deduping against any hazard already on
    // that cell (one fire per cell keeps spread bounded and the map readable).
    private static void IgniteHazardAtCell(
        ReducerContext ctx,
        HazardTypeId typeId,
        (int X, int Y) cell,
        float intensity
    )
    {
        foreach (var existing in ctx.Db.Hazards.Iter())
        {
            if (WorldToCell(existing.Position) == cell)
            {
                return;
            }
        }

        ctx.Db.Hazards.Insert(
            new Hazard
            {
                HazardId = 0,
                HazardTypeId = typeId,
                Position = CellToWorld(cell),
                Intensity = intensity,
                RoomSlotIndex = SlotForCell(cell),
            }
        );
    }

    private static void DeleteAllHazards(ReducerContext ctx)
    {
        var stale = new System.Collections.Generic.List<int>();
        foreach (var hazard in ctx.Db.Hazards.Iter())
        {
            stale.Add(hazard.HazardId);
        }

        foreach (var id in stale)
        {
            ctx.Db.Hazards.HazardId.Delete(id);
        }
    }

    // One tick: grow every hazard's intensity, spread one new cell, then burn
    // anyone standing in fire. Deterministic throughout — no RNG (reducers
    // forbid it): spread always picks the lowest-ordered candidate cell.
    private static void TickHazards(ReducerContext ctx, HazardConfig config)
    {
        var hazards = new System.Collections.Generic.List<Hazard>();
        foreach (var hazard in ctx.Db.Hazards.Iter())
        {
            hazards.Add(hazard);
        }

        if (hazards.Count == 0)
        {
            return;
        }

        // 1) Grow intensity (capped at 1).
        for (var i = 0; i < hazards.Count; i++)
        {
            var hazard = hazards[i];
            var intensity = System.Math.Min(
                MaxHazardIntensity,
                hazard.Intensity + config.IntensityPerTick
            );
            if (intensity != hazard.Intensity)
            {
                hazard = hazard with { Intensity = intensity };
                ctx.Db.Hazards.HazardId.Update(hazard);
                hazards[i] = hazard;
            }
        }

        // 2) Spread one cell per tick, under the hazard cap.
        if (hazards.Count < config.MaxHazards)
        {
            SpreadFire(ctx, config, hazards);
        }

        // 3) Proximity damage to connected, living players.
        ApplyHazardProximityDamage(ctx, config, hazards);
    }

    // Seeds the single lowest-ordered un-ignited floor cell adjacent to any fire
    // hot enough to spread. "Lowest-ordered" = smallest (Y, then X), the same
    // canonical order BuildFloorCells would sort to — fully deterministic.
    private static void SpreadFire(
        ReducerContext ctx,
        HazardConfig config,
        System.Collections.Generic.List<Hazard> hazards
    )
    {
        var occupied = new System.Collections.Generic.HashSet<(int X, int Y)>();
        foreach (var hazard in hazards)
        {
            occupied.Add(WorldToCell(hazard.Position));
        }

        (int X, int Y)? best = null;
        foreach (var hazard in hazards)
        {
            if (
                hazard.HazardTypeId != HazardTypeId.Fire
                || hazard.Intensity < config.SpreadThreshold
            )
            {
                continue;
            }

            foreach (var neighbor in Neighbors4(WorldToCell(hazard.Position)))
            {
                if (!IsFloorCell(neighbor) || occupied.Contains(neighbor))
                {
                    continue;
                }

                if (best is not { } current || CellLess(neighbor, current))
                {
                    best = neighbor;
                }
            }
        }

        if (best is { } target)
        {
            IgniteHazardAtCell(ctx, HazardTypeId.Fire, target, config.IntensityPerTick);
        }
    }

    // A player standing within FireDamageRadius of ANY fire takes one damage
    // tick — burning is binary per tick, not stacked per overlapping flame, so
    // it stays predictable for validation. Dead/disconnected players are skipped
    // (ApplyDamage also no-ops on the dead).
    private static void ApplyHazardProximityDamage(
        ReducerContext ctx,
        HazardConfig config,
        System.Collections.Generic.List<Hazard> hazards
    )
    {
        var radiusSq = config.FireDamageRadius * config.FireDamageRadius;

        foreach (var player in ctx.Db.Players.Iter())
        {
            if (!player.IsConnected)
            {
                continue;
            }

            if (ctx.Db.VitalsRows.Identity.Find(player.Identity) is not { IsDead: false })
            {
                continue;
            }

            if (ctx.Db.Entities.EntityId.Find(player.PlayerEntityId) is not { } entity)
            {
                continue;
            }

            var inFire = false;
            foreach (var hazard in hazards)
            {
                if (hazard.HazardTypeId != HazardTypeId.Fire)
                {
                    continue;
                }

                var dx = entity.Position.X - hazard.Position.X;
                var dy = entity.Position.Y - hazard.Position.Y;
                if (dx * dx + dy * dy < radiusSq)
                {
                    inFire = true;
                    break;
                }
            }

            if (inFire)
            {
                ApplyDamage(ctx, player.Identity, config.FireDamagePerTick, DamageType.Fire);
            }
        }
    }

    private static bool CellLess((int X, int Y) a, (int X, int Y) b) =>
        a.Y != b.Y ? a.Y < b.Y : a.X < b.X;
}
