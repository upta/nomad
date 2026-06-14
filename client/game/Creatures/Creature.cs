#nullable enable

namespace Nomad.Game.Creatures;

using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;

// A surface creature rendered from a Creature row. The server tick owns
// movement (chase/patrol); the node lerps toward the latest server position so
// the steady per-tick updates read as smooth motion (the RemoteEntity
// approach). No collider — creatures don't block the crew (avoid/flee, no
// combat); contact damage is applied server-side.
[Meta(typeof(IAutoNode))]
public partial class Creature : Node2D
{
    private Vector2 _target;

    public override void _Notification(int what) => this.Notify(what);

    // Higher snaps to the server position faster; lower glides.
    [Export]
    public float LerpSpeed { get; set; } = 8f;

    [Node]
    public ILabel Glyph { get; set; } = default!;

    [Node]
    public IColorRect Sprite { get; set; } = default!;

    public int CreatureId { get; private set; }

    public override void _Process(double delta)
    {
        GlobalPosition = GlobalPosition.Lerp(
            _target,
            Mathf.Clamp((float)delta * LerpSpeed, 0f, 1f)
        );
    }

    public void SetCreature(int creatureId, CreatureType type, Vector2 position)
    {
        CreatureId = creatureId;
        Sprite.Color = type.Color;
        Glyph.Text = type.Glyph;
        GlobalPosition = position;
        _target = position;
    }

    public void SetTarget(Vector2 position) => _target = position;
}
