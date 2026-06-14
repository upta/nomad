#nullable enable

namespace Nomad.Game.Ship;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

// Reusable ship-interior component: wraps the ShipGrid (which already owns
// terminal/breaker/suit-rack spawning) and exposes mount points so per-node
// GameMap scenes can place the ship and seat the crew. The Quiet map drops it
// into open space; exterior maps (Planetside, Wreck, Trading) park it beside
// their grid and reach it through the AirlockMount. (Named ShipBody, not Ship,
// to avoid colliding with the Nomad.Game.Ship namespace.)
[Meta(typeof(IAutoNode))]
public partial class ShipBody : Node2D
{
    public override void _Notification(int what) => this.Notify(what);

    [Node]
    public Nomad.Game.Map.ShipGrid ShipGrid { get; set; } = default!;

    [Node]
    public Marker2D PlayerSpawn { get; set; } = default!;

    [Node]
    public Marker2D AirlockMount { get; set; } = default!;
}
