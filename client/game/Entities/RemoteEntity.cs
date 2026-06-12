namespace Nomad.Game.Entities;

[Meta(typeof(IAutoNode))]
public partial class RemoteEntity : Node2D
{
    private Color _baseSpriteColor;

    public override void _Notification(int what) => this.Notify(what);

    [Export]
    public Color GhostColor { get; set; } = new(0.65f, 0.85f, 1f, 0.45f);

    public bool IsGhost { get; private set; }

    [Node]
    public EntityMover Mover { get; set; } = default!;

    [Node]
    public Chickensoft.GodotNodeInterfaces.IColorRect Sprite { get; set; } = default!;

    public void Initialize(DbConnection server, Entity entity)
    {
        Mover.EntityId = entity.EntityId;
        Mover.Server = server;
        Mover.Initialize(entity);
        _baseSpriteColor = Sprite.Color;
    }

    public void SetGhost(bool ghost)
    {
        IsGhost = ghost;
        Sprite.Color = ghost ? GhostColor : _baseSpriteColor;
    }
}
