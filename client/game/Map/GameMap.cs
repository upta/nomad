#nullable enable

namespace Nomad.Game.Map;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

// Base for per-node map scenes. Each map holds a ShipBody component (and, for
// exterior nodes, its own grid/airlock); MapHost instantiates the map matching
// the active NodeKind and reaches the ship through here. Quiet is the only map
// until the exterior nodes land in 5.2.
[Meta(typeof(IAutoNode))]
public partial class GameMap : Node2D
{
    public override void _Notification(int what) => this.Notify(what);

    [Node]
    public Nomad.Game.Ship.ShipBody Ship { get; set; } = default!;
}
