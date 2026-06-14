public static partial class Module
{
    private const int DefaultCreatureTickMillis = 200;

    // Movement per tick (px). At the default 200ms tick that's ~60px/s — well
    // under the crew's run speed so they comfortably outrun and observe them
    // (avoid/flee, no combat). Tunable via SetCreatureConfig.
    private const float DefaultCreatureMoveSpeed = 12f;

    // Chase any exterior player whose body is within this radius; otherwise
    // patrol. ~12 tiles.
    private const float DefaultCreatureChaseRange = 384f;

    // Contact damage when a creature closes to within this radius of a player
    // (~1 tile). One damage application per tick per contacted player.
    private const float DefaultCreatureContactRadius = 36f;
    private const float DefaultCreatureContactDamage = 4f;
    private const float DefaultCreatureMaxHealth = 30f;

    // Fixed patrol loop on the exterior grid (right of the landing pad at
    // x=560). Deterministic — no RNG in reducers — and shared by every
    // creature; each tracks its own PatrolIndex into this ring.
    private static readonly DbVector2[] CreaturePatrol =
    [
        new() { X = 800f, Y = -160f },
        new() { X = 1120f, Y = -160f },
        new() { X = 1120f, Y = 160f },
        new() { X = 800f, Y = 160f },
    ];

    private static CreatureConfig GetCreatureConfig(ReducerContext ctx) =>
        ctx.Db.CreatureConfigs.Id.Find(0)
        ?? ctx.Db.CreatureConfigs.Insert(
            new CreatureConfig
            {
                Id = 0,
                TickMillis = DefaultCreatureTickMillis,
                MoveSpeed = DefaultCreatureMoveSpeed,
                ChaseRange = DefaultCreatureChaseRange,
                ContactRadius = DefaultCreatureContactRadius,
                ContactDamage = DefaultCreatureContactDamage,
                MaxHealth = DefaultCreatureMaxHealth,
            }
        );

    // The repeating tick is a single row; replacing it (rather than updating)
    // is the only way to change a scheduled table's interval.
    private static void RescheduleCreatureTick(ReducerContext ctx, int tickMillis)
    {
        var stale = new System.Collections.Generic.List<ulong>();
        foreach (var timer in ctx.Db.CreatureTickTimers.Iter())
        {
            stale.Add(timer.Id);
        }

        foreach (var id in stale)
        {
            ctx.Db.CreatureTickTimers.Id.Delete(id);
        }

        ctx.Db.CreatureTickTimers.Insert(
            new CreatureTickTimer
            {
                Id = 0,
                ScheduledAt = new ScheduleAt.Interval(System.TimeSpan.FromMilliseconds(tickMillis)),
            }
        );
    }

    private static void SpawnCreatureAt(
        ReducerContext ctx,
        CreatureTypeId typeId,
        DbVector2 position,
        int patrolIndex
    )
    {
        ctx.Db.Creatures.Insert(
            new Creature
            {
                CreatureId = 0,
                CreatureTypeId = typeId,
                Position = position,
                Velocity = new DbVector2 { X = 0, Y = 0 },
                Health = GetCreatureConfig(ctx).MaxHealth,
                PatrolIndex = patrolIndex,
            }
        );
    }

    private static void DeleteAllCreatures(ReducerContext ctx)
    {
        var stale = new System.Collections.Generic.List<int>();
        foreach (var creature in ctx.Db.Creatures.Iter())
        {
            stale.Add(creature.CreatureId);
        }

        foreach (var id in stale)
        {
            ctx.Db.Creatures.CreatureId.Delete(id);
        }
    }

    // Seeds the Planetside surface with a couple of patrolling creatures,
    // offset to opposite waypoints so they don't stack.
    private static void SeedPlanetsideCreatures(ReducerContext ctx)
    {
        SpawnCreatureAt(ctx, CreatureTypeId.Crawler, new DbVector2 { X = 880f, Y = -160f }, 1);
        SpawnCreatureAt(ctx, CreatureTypeId.Crawler, new DbVector2 { X = 1040f, Y = 160f }, 3);
    }

    // The derelict's denizens — a couple of crawlers prowling the wreck
    // exterior (the salvage hazard), offset to opposite waypoints.
    private static void SeedWreckCreatures(ReducerContext ctx)
    {
        SpawnCreatureAt(ctx, CreatureTypeId.Crawler, new DbVector2 { X = 820f, Y = -120f }, 1);
        SpawnCreatureAt(ctx, CreatureTypeId.Crawler, new DbVector2 { X = 1060f, Y = 120f }, 3);
    }

    // One tick: every creature chases the nearest in-range exterior player
    // (deterministic — nearest by squared distance, ties by table order), else
    // walks its patrol ring. A creature that closes to contact range damages
    // that player via DamageType.Creature (ApplyDamage no-ops on the dead).
    private static void TickCreatures(ReducerContext ctx, CreatureConfig config)
    {
        var creatures = new System.Collections.Generic.List<Creature>();
        foreach (var creature in ctx.Db.Creatures.Iter())
        {
            creatures.Add(creature);
        }

        if (creatures.Count == 0)
        {
            return;
        }

        var targets = CollectExteriorTargets(ctx);

        var chaseRangeSq = config.ChaseRange * config.ChaseRange;
        var contactRadiusSq = config.ContactRadius * config.ContactRadius;

        foreach (var creature in creatures)
        {
            var moved = StepCreature(creature, config, targets, chaseRangeSq);

            ctx.Db.Creatures.CreatureId.Update(moved.Creature);

            if (moved.ContactIdentity is { } victim)
            {
                var dx = moved.Creature.Position.X - moved.TargetPosition.X;
                var dy = moved.Creature.Position.Y - moved.TargetPosition.Y;
                if (dx * dx + dy * dy < contactRadiusSq)
                {
                    ApplyDamage(ctx, victim, config.ContactDamage, DamageType.Creature);
                }
            }
        }
    }

    private readonly struct CreatureStep
    {
        public Creature Creature { get; init; }
        public Identity? ContactIdentity { get; init; }
        public DbVector2 TargetPosition { get; init; }
    }

    private static CreatureStep StepCreature(
        Creature creature,
        CreatureConfig config,
        System.Collections.Generic.List<(Identity Identity, DbVector2 Position)> targets,
        float chaseRangeSq
    )
    {
        Identity? chase = null;
        var chasePos = new DbVector2 { X = 0, Y = 0 };
        var bestSq = chaseRangeSq;
        foreach (var (identity, pos) in targets)
        {
            var dx = pos.X - creature.Position.X;
            var dy = pos.Y - creature.Position.Y;
            var distSq = dx * dx + dy * dy;
            if (distSq < bestSq)
            {
                bestSq = distSq;
                chase = identity;
                chasePos = pos;
            }
        }

        if (chase is { } victim)
        {
            var (next, _) = MoveToward(creature.Position, chasePos, config.MoveSpeed);
            return new CreatureStep
            {
                Creature = creature with
                {
                    Position = next,
                    Velocity = Step(creature.Position, next),
                },
                ContactIdentity = victim,
                TargetPosition = chasePos,
            };
        }

        // Patrol: walk to the current waypoint; advance the ring on arrival.
        var waypoint = CreaturePatrol[
            ((creature.PatrolIndex % CreaturePatrol.Length) + CreaturePatrol.Length)
                % CreaturePatrol.Length
        ];
        var (patrolNext, reached) = MoveToward(creature.Position, waypoint, config.MoveSpeed);
        return new CreatureStep
        {
            Creature = creature with
            {
                Position = patrolNext,
                Velocity = Step(creature.Position, patrolNext),
                PatrolIndex = reached ? creature.PatrolIndex + 1 : creature.PatrolIndex,
            },
            ContactIdentity = null,
            TargetPosition = waypoint,
        };
    }

    private static System.Collections.Generic.List<(
        Identity Identity,
        DbVector2 Position
    )> CollectExteriorTargets(ReducerContext ctx)
    {
        var targets = new System.Collections.Generic.List<(
            Identity Identity,
            DbVector2 Position
        )>();

        foreach (var player in ctx.Db.Players.Iter())
        {
            if (!player.IsConnected || !player.InExterior)
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

            targets.Add((player.Identity, entity.Position));
        }

        return targets;
    }

    // Moves `from` toward `to` by at most `maxStep` px. Returns the new position
    // and whether it reached the target this step.
    private static (DbVector2 Position, bool Reached) MoveToward(
        DbVector2 from,
        DbVector2 to,
        float maxStep
    )
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var dist = System.MathF.Sqrt(dx * dx + dy * dy);
        if (dist <= maxStep || dist <= 0.0001f)
        {
            return (to, true);
        }

        var t = maxStep / dist;
        return (new DbVector2 { X = from.X + dx * t, Y = from.Y + dy * t }, false);
    }

    private static DbVector2 Step(DbVector2 from, DbVector2 to) =>
        new() { X = to.X - from.X, Y = to.Y - from.Y };
}
