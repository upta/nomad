#nullable enable

namespace Nomad.Game.Ship;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

// Reusable ship-interior component: wraps the ShipGrid (which already owns
// terminal/breaker/suit-rack spawning) and carries the ship's airlock door so
// per-node GameMap scenes can place the ship and the crew can cross out/in. The
// Quiet map drops it into open space; exterior maps (Planetside, Wreck, Trading)
// park it beside their grid, reached through the Airlock. (Named ShipBody, not
// Ship, to avoid colliding with the Nomad.Game.Ship namespace.)
[Meta(typeof(IAutoNode))]
public partial class ShipBody : Node2D
{
    public override void _Notification(int what) => this.Notify(what);

    [Node]
    public Nomad.Game.Map.ShipGrid ShipGrid { get; set; } = default!;

    [Node]
    public Marker2D PlayerSpawn { get; set; } = default!;

    // The ship's single airlock door, on the right-edge hull. Present on every
    // map; MapHost wires its label/verb to the active node + the player's zone.
    [Node]
    public Nomad.Game.Map.Airlock Airlock { get; set; } = default!;
}
