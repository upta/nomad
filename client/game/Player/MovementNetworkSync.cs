namespace Nomad.Game.Player;

using Godot;

public class MovementNetworkSync(DbConnection server, float sendInterval = 0.05f)
{
    private Vector2 _lastSentPosition;
    private double _timeSinceLastSend;
    private bool _wasMoved;

    public void Initialize(Vector2 position) => _lastSentPosition = position;

    public void Update(
        int entityId,
        Vector2 position,
        Vector2 velocity,
        float rotation,
        double delta
    )
    {
        var hasMoved = !position.IsEqualApprox(_lastSentPosition);
        _timeSinceLastSend += delta;

        var shouldSend =
            (hasMoved && _timeSinceLastSend >= sendInterval) || (!hasMoved && _wasMoved);

        if (shouldSend)
        {
            server.Reducers.MoveEntity(
                entityId,
                new DbVector2(position.X, position.Y),
                new DbVector2(velocity.X, velocity.Y),
                rotation,
                Time.GetTicksMsec() / 1000.0
            );
            _lastSentPosition = position;
            _timeSinceLastSend = 0;
        }

        _wasMoved = hasMoved;
    }
}
