namespace Nomad.Game.Player;

using Godot;
using StdbPlayer = SpacetimeDB.Types.Player;

[Meta(typeof(IAutoNode))]
public partial class Player : CharacterBody2D
{
    private float _currentRotation;
    private MovementNetworkSync _networkSync = null!;
    private float _speedModifier = 1.0f;
    private int _entityId;

    [Dependency]
    private DbConnection Server => this.DependOn<DbConnection>();

    [Export]
    public Resource MoveAction { get; set; } = null!;

    [Export]
    public float Acceleration { get; set; } = 1600f;

    [Export]
    public float Deceleration { get; set; } = 2400f;

    [Export]
    public float MoveSpeed { get; set; } = 400f;

    public override void _Notification(int what) => this.Notify(what);

    public override void _PhysicsProcess(double delta)
    {
        if (MoveAction is null)
            return;

        var direction = (Vector2)MoveAction.Get("value_axis_2d");

        var inputMagnitude = direction.Length();
        if (inputMagnitude > 1f)
            direction /= inputMagnitude;

        var targetVelocity = direction * MoveSpeed * _speedModifier;
        var rate = direction != Vector2.Zero ? Acceleration : Deceleration;
        Velocity = Velocity.MoveToward(targetVelocity, rate * (float)delta);
        MoveAndSlide();

        if (direction != Vector2.Zero)
            _currentRotation = direction.Angle();

        _networkSync?.Update(_entityId, GlobalPosition, Velocity, _currentRotation, delta);
    }

    public void OnResolved()
    {
        _networkSync = new MovementNetworkSync(Server);
        _networkSync.Initialize(GlobalPosition);

        if (Server.Identity is { } identity)
        {
            var playerRow = Server.Db.Players.Identity.Find(identity);
            if (playerRow is { } p)
                _entityId = p.PlayerEntityId;
        }
    }

    public override void _ExitTree()
    {
        Server.Db.Players.OnUpdate -= OnPlayerUpdated;
    }

    private void OnPlayerUpdated(EventContext ctx, StdbPlayer oldPlayer, StdbPlayer newPlayer)
    {
        _ = newPlayer.IsConnected;
    }
}
