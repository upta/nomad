public static partial class Module
{
    [SpacetimeDB.Reducer]
    public static void VitalsTick(ReducerContext ctx, VitalsTickTimer timer)
    {
        var config = GetVitalsConfig(ctx);

        foreach (var player in ctx.Db.Players.Iter())
        {
            if (!player.IsConnected)
            {
                continue;
            }

            if (ctx.Db.VitalsRows.Identity.Find(player.Identity) is not { } vitals || vitals.IsDead)
            {
                continue;
            }

            TickOxygen(ctx, config, player, vitals);
        }
    }

    private static void TickOxygen(
        ReducerContext ctx,
        VitalsConfig config,
        Player player,
        Vitals vitals
    )
    {
        var room =
            player.CurrentSlotIndex >= 0
                ? ctx.Db.RoomAssignments.SlotIndex.Find(player.CurrentSlotIndex)
                : null;

        // Pressurized + powered refills; pressurized but unpowered holds
        // steady (GDD §6.2: refill needs a "fully pressurized, powered room");
        // everything else — unpressurized room or no room at all — depletes.
        var oxygen = vitals.Oxygen.Current;
        if (room is { IsPressurized: true, IsPowered: true })
        {
            oxygen = System.Math.Min(vitals.Oxygen.Max, oxygen + config.OxygenRefillPerTick);
        }
        else if (room is not { IsPressurized: true })
        {
            oxygen = System.Math.Max(0f, oxygen - config.OxygenDepletePerTick);
        }

        if (oxygen != vitals.Oxygen.Current)
        {
            ctx.Db.VitalsRows.Identity.Update(
                vitals with
                {
                    Oxygen = vitals.Oxygen with { Current = oxygen },
                }
            );
        }

        if (oxygen <= 0f)
        {
            ApplyDamage(
                ctx,
                player.Identity,
                config.SuffocationDamagePerTick,
                DamageType.Suffocation
            );
        }
    }
}
