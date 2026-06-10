namespace Nomad.Game.Entities;

public class SnapshotInterpolator(
    int maxSnapshots = 20,
    float renderDelay = 0.15f,
    float maxExtrapolationTime = 0.25f
)
{
    private const double OffsetSmoothingFactor = 0.1;

    private double? _clockOffset;
    private readonly LinkedList<MovementSnapshot> _snapshots = new();

    public bool IsSettled { get; private set; }

    public int SnapshotCount => _snapshots.Count;

    public double GetRenderTime() => Time.GetTicksMsec() / 1000.0 - renderDelay;

    public (
        Vector2 Position,
        float Rotation,
        float VelocityMagnitude,
        bool IsExtrapolating
    ) Interpolate(double renderTime)
    {
        if (_snapshots.Count == 0)
        {
            throw new System.InvalidOperationException(
                "Cannot interpolate with no snapshots; ensure at least one snapshot has been pushed before calling Interpolate."
            );
        }

        var first = _snapshots.First!.Value;
        var last = _snapshots.Last!.Value;

        if (renderTime <= first.Timestamp)
        {
            return (first.Position, first.Rotation, first.VelocityMagnitude, false);
        }

        var node = _snapshots.First!;
        while (node.Next is not null)
        {
            var a = node.Value;
            var b = node.Next.Value;

            if (renderTime >= a.Timestamp && renderTime <= b.Timestamp)
            {
                var span = b.Timestamp - a.Timestamp;
                var t = span > 0 ? (float)((renderTime - a.Timestamp) / span) : 1f;
                return (
                    a.Position.Lerp(b.Position, t),
                    Mathf.LerpAngle(a.Rotation, b.Rotation, t),
                    Mathf.Lerp(a.VelocityMagnitude, b.VelocityMagnitude, t),
                    false
                );
            }

            node = node.Next;
        }

        var elapsed = (float)(renderTime - last.Timestamp);
        if (elapsed >= maxExtrapolationTime)
        {
            IsSettled = true;
            return (last.Position, last.Rotation, 0f, false);
        }

        var normalizedElapsed = elapsed / maxExtrapolationTime;
        var decay = 1f - normalizedElapsed * normalizedElapsed;
        var extrapolated = last.Position + last.Velocity * elapsed * decay;
        return (extrapolated, last.Rotation, last.VelocityMagnitude * decay, true);
    }

    public void PushSnapshot(
        Vector2 position,
        Vector2 velocity,
        float rotation,
        double senderTimestamp = 0
    )
    {
        var now = Time.GetTicksMsec() / 1000.0;
        double localTimestamp;

        if (senderTimestamp > 0)
        {
            var newOffset = now - senderTimestamp;
            _clockOffset = _clockOffset.HasValue
                ? _clockOffset.Value * (1.0 - OffsetSmoothingFactor)
                    + newOffset * OffsetSmoothingFactor
                : newOffset;
            localTimestamp = senderTimestamp + _clockOffset.Value;
        }
        else
        {
            localTimestamp = now;
        }

        var snapshot = new MovementSnapshot
        {
            Position = position,
            Rotation = rotation,
            Timestamp = localTimestamp,
            Velocity = velocity,
            VelocityMagnitude = velocity.Length(),
        };

        IsSettled = false;
        _snapshots.AddLast(snapshot);

        while (_snapshots.Count > maxSnapshots)
        {
            _snapshots.RemoveFirst();
        }
    }

    private struct MovementSnapshot
    {
        public Vector2 Position;
        public float Rotation;
        public double Timestamp;
        public Vector2 Velocity;
        public float VelocityMagnitude;
    }
}
