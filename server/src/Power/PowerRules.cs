public static partial class Module
{
    private const int DefaultGraceMillis = 10_000;
    private const int DefaultReactorOutput = 10;

    // Demand sums the draw of every breaker-on room; output is the reactor's
    // rated output, but only while the reactor's own breaker is closed —
    // cutting the reactor breaker overloads the whole grid.
    private static (int Demand, int Output) ComputeLoad(ReducerContext ctx, PowerGrid grid)
    {
        var demand = 0;
        var reactorOnline = false;

        foreach (var ra in ctx.Db.RoomAssignments.Iter())
        {
            if (!ra.BreakerOn)
            {
                continue;
            }

            if (ra.RoomTypeId == RoomTypeId.Reactor)
            {
                reactorOnline = true;
            }

            demand += PowerDrawFor(ra.RoomTypeId);
        }

        return (demand, reactorOnline ? grid.ReactorOutput : 0);
    }

    private static PowerGrid GetPowerGrid(ReducerContext ctx) =>
        ctx.Db.PowerGrids.Id.Find(0)
        ?? ctx.Db.PowerGrids.Insert(
            new PowerGrid
            {
                Id = 0,
                ReactorOutput = DefaultReactorOutput,
                GraceMillis = DefaultGraceMillis,
                Status = GridStatus.Stable,
            }
        );

    private static int PowerDrawFor(RoomTypeId roomTypeId) =>
        roomTypeId switch
        {
            RoomTypeId.Bridge => 2,
            RoomTypeId.CloningBay => 2,
            RoomTypeId.Hydroponics => 1,
            RoomTypeId.Workshop => 2,
            RoomTypeId.Kitchen => 1,
            _ => 0,
        };

    private static void CancelBlackoutTimers(ReducerContext ctx)
    {
        var pending = new System.Collections.Generic.List<ulong>();
        foreach (var timer in ctx.Db.GridBlackoutTimers.Iter())
        {
            pending.Add(timer.Id);
        }

        foreach (var id in pending)
        {
            ctx.Db.GridBlackoutTimers.Id.Delete(id);
        }
    }

    private static void RecomputePowerGrid(ReducerContext ctx)
    {
        var grid = GetPowerGrid(ctx);
        var (demand, output) = ComputeLoad(ctx, grid);

        if (demand <= output)
        {
            CancelBlackoutTimers(ctx);
            ctx.Db.PowerGrids.Id.Update(grid with { Status = GridStatus.Stable });
            SetRoomsPowered(ctx, gridLive: true);
            return;
        }

        switch (grid.Status)
        {
            case GridStatus.Stable:
                var blackoutAt = ctx.Timestamp + System.TimeSpan.FromMilliseconds(grid.GraceMillis);
                ctx.Db.GridBlackoutTimers.Insert(
                    new GridBlackoutTimer { Id = 0, ScheduledAt = new ScheduleAt.Time(blackoutAt) }
                );
                ctx.Db.PowerGrids.Id.Update(
                    grid with
                    {
                        Status = GridStatus.Overload,
                        BlackoutAt = blackoutAt,
                    }
                );
                SetRoomsPowered(ctx, gridLive: true);
                break;
            case GridStatus.Overload:
                // Grace window keeps running; rooms stay live so breaker flips
                // made during the warning still light up.
                SetRoomsPowered(ctx, gridLive: true);
                break;
            case GridStatus.Blackout:
                SetRoomsPowered(ctx, gridLive: false);
                break;
        }
    }

    private static void SetRoomsPowered(ReducerContext ctx, bool gridLive)
    {
        var stale = new System.Collections.Generic.List<RoomAssignment>();
        foreach (var ra in ctx.Db.RoomAssignments.Iter())
        {
            var powered = gridLive && ra.BreakerOn;
            if (ra.IsPowered != powered)
            {
                stale.Add(ra with { IsPowered = powered });
            }
        }

        foreach (var ra in stale)
        {
            ctx.Db.RoomAssignments.SlotIndex.Update(ra);
        }
    }
}
