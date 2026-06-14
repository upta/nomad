#nullable enable

namespace Nomad.Game.Map;

using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

// Base for per-node map scenes. Each map holds a ShipBody component (and, for
// exterior nodes, its own grid/airlock); MapHost instantiates the map matching
// the active NodeKind and reaches the ship through here. Quiet is the
// ship-in-space default; PlanetsideMap (5.2) parks the same ship beside an
// exterior grid reached through airlocks.
[Meta(typeof(IAutoNode))]
public partial class GameMap : Node2D
{
    public override void _Notification(int what) => this.Notify(what);

    // Raised when a player uses the ship's airlock door. The direction (exit vs
    // enter) is decided by MapHost from the player's current zone; the map just
    // forwards the interaction. Auto-wired from any Airlock the map (or its
    // ShipBody) declares.
    public event Action? AirlockUsed;

    [Node]
    public Nomad.Game.Ship.ShipBody Ship { get; set; } = default!;

    // Auto-wires every Airlock the map scene declares (none on the Quiet base)
    // so a map only has to place its airlock fixtures — no per-map script. The
    // airlocks are freed with the map, so their subscriptions die with them.
    public void OnReady()
    {
        WireAirlocks(this);
    }

    private void WireAirlocks(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is Airlock airlock)
                airlock.Interacted += RaiseAirlockUsed;
            WireAirlocks(child);
        }
    }

    private void RaiseAirlockUsed() => AirlockUsed?.Invoke();
}
