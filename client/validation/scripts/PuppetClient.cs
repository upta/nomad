namespace Nomad.Validation;

using Godot;
using SpacetimeDB.Types;

// A second, in-process SpacetimeDB client acting as another player.
// Connects anonymously (no token) so the server assigns a fresh identity,
// then drives its own entity rightward through the MoveEntity reducer.
public partial class PuppetClient : Node
{
    private const float SendInterval = 0.05f;

    private DbConnection? _conn;
    private bool _dataReady;
    private int _entityId;
    private SpacetimeDB.Identity? _identity;
    private Vector2? _initialPosition;
    private bool _moving;
    private Vector2 _position;
    private double _timeSinceLastSend;

    [Export]
    public float MoveSpeed { get; set; } = 100f;

    public bool DataReady => _dataReady;

    public float DisplacementFromInitial =>
        _initialPosition is { } initial ? _position.DistanceTo(initial) : 0f;

    public int EntityId => _entityId;

    public override void _ExitTree()
    {
        _conn?.Disconnect();
    }

    public override void _Process(double delta)
    {
        _conn?.FrameTick();

        if (_conn is null || !_dataReady)
            return;

        if (_entityId == 0)
        {
            ResolveEntity(_conn);
            return;
        }

        if (!_moving)
            return;

        _position += Vector2.Right * MoveSpeed * (float)delta;
        _timeSinceLastSend += delta;
        if (_timeSinceLastSend < SendInterval)
            return;

        _timeSinceLastSend = 0;
        _conn.Reducers.MoveEntity(
            _entityId,
            new DbVector2(_position.X, _position.Y),
            new DbVector2(MoveSpeed, 0f),
            0f,
            Time.GetTicksMsec() / 1000.0
        );
    }

    public override void _Ready()
    {
        _conn = DbConnection
            .Builder()
            .WithUri(GetConfig("NOMAD_STDB_URI", "http://localhost:3000"))
            .WithDatabaseName(GetConfig("NOMAD_STDB_DB", "nomad"))
            .OnConnect(OnConnected)
            .OnConnectError(e => GD.PushError($"[PuppetClient] Connection error: {e.Message}"))
            .Build();
    }

    private static string GetConfig(string variable, string fallback)
    {
        var value = OS.GetEnvironment(variable);
        return string.IsNullOrEmpty(value) ? fallback : value;
    }

    private void OnConnected(DbConnection conn, SpacetimeDB.Identity identity, string token)
    {
        _identity = identity;
        conn.SubscriptionBuilder().OnApplied(ctx => _dataReady = true).SubscribeToAllTables();
    }

    private void ResolveEntity(DbConnection conn)
    {
        if (_identity is not { } identity)
            return;

        if (conn.Db.Players.Identity.Find(identity) is not { } player)
            return;

        _entityId = player.PlayerEntityId;
        if (conn.Db.Entities.EntityId.Find(_entityId) is { } entity)
            _position = new Vector2(entity.Position.X, entity.Position.Y);

        _initialPosition = _position;
        _moving = true;
    }
}
