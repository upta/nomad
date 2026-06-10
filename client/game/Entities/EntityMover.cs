namespace Nomad.Game.Entities;

using Godot;

public partial class EntityMover : Node
{
    private const float CorrectionBlendSpeed = 12f;
    private const float SnapDistanceThreshold = 1000f;

    private Vector2 _correctionOffset;
    private SnapshotInterpolator _interpolator = null!;
    private bool _wasExtrapolating;

    public int EntityId { get; set; }
    public DbConnection Server { get; set; } = null!;

    [Export]
    public float RenderDelay { get; set; } = 0.15f;

    [Export]
    public Node2D? TargetNode { get; set; }

    public override void _Process(double delta)
    {
        if (TargetNode is null || _interpolator.SnapshotCount == 0 || _interpolator.IsSettled)
            return;

        var renderTime = _interpolator.GetRenderTime();
        var (position, rotation, velocityMagnitude, isExtrapolating) = _interpolator.Interpolate(
            renderTime
        );

        if (_wasExtrapolating && !isExtrapolating)
            _correctionOffset = TargetNode.GlobalPosition - position;

        _wasExtrapolating = isExtrapolating;

        _correctionOffset = _correctionOffset.Lerp(
            Vector2.Zero,
            Mathf.Min(1f, CorrectionBlendSpeed * (float)delta)
        );

        var smoothedPosition = position + _correctionOffset;
        var distToSmoothed = TargetNode.GlobalPosition.DistanceTo(smoothedPosition);

        if (distToSmoothed > SnapDistanceThreshold)
            _correctionOffset = Vector2.Zero;

        TargetNode.GlobalPosition = smoothedPosition;
    }

    public override void _ExitTree()
    {
        Server.Db.ActiveEntities.OnInsert -= OnEntityUpdated;
    }

    public void Initialize(Entity entity)
    {
        _interpolator = new SnapshotInterpolator(renderDelay: RenderDelay);
        TargetNode ??= GetParent() as Node2D;
        Server.Db.ActiveEntities.OnInsert += OnEntityUpdated;

        var position = new Vector2(entity.Position.X, entity.Position.Y);
        var velocity = new Vector2(entity.Velocity.X, entity.Velocity.Y);
        if (TargetNode is not null)
            TargetNode.GlobalPosition = position;
        _interpolator.PushSnapshot(position, velocity, entity.Rotation, entity.SenderTimestamp);
    }

    private void OnEntityUpdated(EventContext ctx, Entity entity)
    {
        if (entity.EntityId != EntityId)
            return;

        var position = new Vector2(entity.Position.X, entity.Position.Y);
        var velocity = new Vector2(entity.Velocity.X, entity.Velocity.Y);
        _interpolator.PushSnapshot(position, velocity, entity.Rotation, entity.SenderTimestamp);
    }
}
