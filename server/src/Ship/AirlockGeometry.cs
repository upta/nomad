public static partial class Module
{
    // Airlock world geometry, mirroring the client scene layout the same way
    // HullGeometry mirrors the hull. The Ship component sits at map origin in
    // every map (so the interior's world coordinates — and fire — stay valid
    // across nodes), and a single Airlock door rides the ship at its right edge.
    // It is one physical door used from both sides:
    // interacting from inside steps you out onto the surface, from outside steps
    // you back in. The exterior grid abuts the ship to the right. If the client
    // door moves, ShipAirlock must move with it.
    //
    //   interior corridor ──[ ShipAirlock door ]── surface
    //        (-464..464)         (464,0)      (ExteriorLanding 540,0 ... 1300)

    private static readonly DbVector2 ShipAirlock = new() { X = 464f, Y = 0f };

    // Where an exiting player lands on the surface — just outside the door, far
    // enough that the door isn't still in interact range (you step onto the
    // surface, then walk back to the door to re-enter).
    private static readonly DbVector2 ExteriorLanding = new() { X = 540f, Y = 0f };

    // Where a returning player lands inside: one tile in from the airlock, on
    // the main corridor floor (cell 27,8 → corridor slot).
    private static readonly DbVector2 InteriorLanding = new() { X = 432f, Y = 0f };

    // Generous walk-up reach (3 tiles), matching the established interaction
    // radii; the server checks it regardless of the client's InteractTarget.
    private const float AirlockReach = 96f;

    // Which nodes present an exterior grid to cross onto. Planetside and the
    // abandoned Wreck (5.3) now; TradingPost (5.4) appends here as it lands.
    private static bool NodeHasExterior(NodeKind kind) =>
        kind == NodeKind.Planetside || kind == NodeKind.Wreck;

    private static bool WithinAirlockReach(DbVector2 from, DbVector2 airlock)
    {
        var dx = from.X - airlock.X;
        var dy = from.Y - airlock.Y;
        return dx * dx + dy * dy <= AirlockReach * AirlockReach;
    }

    // Brings every exterior player back inside on a node switch: the surface
    // they were standing on no longer exists. Clears the zone flag AND teleports
    // the body to the interior landing — otherwise the map flips to the new node
    // while the body keeps its old surface position, leaving the player floating
    // outside the hull (the play-test oddity). The client snaps to the new
    // server position on the InExterior flip.
    private static void ReturnPlayersToInterior(ReducerContext ctx)
    {
        var exterior = new System.Collections.Generic.List<Player>();
        foreach (var player in ctx.Db.Players.Iter())
        {
            if (player.InExterior)
            {
                exterior.Add(player);
            }
        }

        var interiorSlot = SlotForCell(WorldToCell(InteriorLanding));
        foreach (var player in exterior)
        {
            ctx.Db.Players.Identity.Update(
                player with
                {
                    InExterior = false,
                    CurrentSlotIndex = interiorSlot,
                }
            );

            if (ctx.Db.Entities.EntityId.Find(player.PlayerEntityId) is { } entity)
            {
                ctx.Db.Entities.EntityId.Update(
                    entity with
                    {
                        Position = InteriorLanding,
                        Velocity = new DbVector2 { X = 0, Y = 0 },
                    }
                );
            }
        }
    }
}
