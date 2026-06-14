public static partial class Module
{
    // Airlock world geometry, mirroring the client scene layout the same way
    // HullGeometry mirrors the hull. The Ship component sits at map origin in
    // every map (so the interior's world coordinates — and fire — stay valid
    // across nodes), and its AirlockMount marker is at the ship's right edge.
    // The exterior grid extends to the right; a player crosses out to the
    // landing pad and back. If the client markers move, these must move with
    // them.
    //
    //   interior corridor ──[ ShipAirlock ]── gap ──[ ExteriorLanding ]── surface
    //        (-464..464)        (464,0)                  (560,0)            (600+)

    private static readonly DbVector2 ShipAirlock = new() { X = 464f, Y = 0f };

    // Where an exiting player lands on the surface — also where the exterior
    // airlock fixture sits, so a returning player reaches the door from here.
    private static readonly DbVector2 ExteriorLanding = new() { X = 560f, Y = 0f };

    // Where a returning player lands inside: one tile in from the airlock, on
    // the main corridor floor (cell 27,8 → corridor slot).
    private static readonly DbVector2 InteriorLanding = new() { X = 432f, Y = 0f };

    // Generous walk-up reach (3 tiles), matching the established interaction
    // radii; the server checks it regardless of the client's InteractTarget.
    private const float AirlockReach = 96f;

    // Which nodes present an exterior grid to cross onto. Planetside now;
    // Wreck (5.3) and TradingPost (5.4) append here as they land.
    private static bool NodeHasExterior(NodeKind kind) => kind == NodeKind.Planetside;

    private static bool WithinAirlockReach(DbVector2 from, DbVector2 airlock)
    {
        var dx = from.X - airlock.X;
        var dy = from.Y - airlock.Y;
        return dx * dx + dy * dy <= AirlockReach * AirlockReach;
    }

    // Brings every exterior player back inside on a node switch: the surface
    // they were standing on no longer exists. Zone flags only — entity
    // positions are client-authoritative, and the client re-seats the body when
    // it reloads the map for the new node.
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
        }
    }
}
