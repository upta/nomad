namespace Nomad.Game.Entities;

[Meta(typeof(IAutoNode))]
public partial class RemoteEntity : Node2D
{
    public override void _Notification(int what) => this.Notify(what);

    [Node]
    public EntityMover Mover { get; set; } = default!;

    public void Initialize(DbConnection server, Entity entity)
    {
        Mover.EntityId = entity.EntityId;
        Mover.Server = server;
        Mover.Initialize(entity);
    }
}
