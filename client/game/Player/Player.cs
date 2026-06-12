#nullable enable

namespace Nomad.Game.Player;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Guide;
using Nomad.Game.Interaction;
using StdbPlayer = SpacetimeDB.Types.Player;

[Meta(typeof(IAutoNode))]
public partial class Player : CharacterBody2D
{
    private Color _baseSpriteColor;
    private float _currentRotation;
    private MovementNetworkSync? _networkSync;
    private ProbeData? _probeData;
    private Nomad.Game.Ship.RoomLocator? _roomLocator;
    private float _speedModifier = 1.0f;
    private int _entityId;

    public override void _Notification(int what) => this.Notify(what);

    [Dependency]
    private InteractionService Interaction => this.DependOn<InteractionService>();

    [Node]
    public Chickensoft.GodotNodeInterfaces.IColorRect Sprite { get; set; } = default!;

    [Export]
    public Color SuitedColor { get; set; } = new(0.9f, 0.65f, 0.2f);

    [Export]
    public GuideActionBinding MoveAction { get; set; } = null!;

    [Export]
    public float Acceleration { get; set; } = 1600f;

    [Export]
    public float Deceleration { get; set; } = 2400f;

    [Export]
    public float MoveSpeed { get; set; } = 400f;

    [Export]
    public Node? DbManagerNode { get; set; }

    // Set by Main when instantiating (exports don't cross scene-instance
    // boundaries); enables room tracking when present.
    public Nomad.Game.Ship.HullTemplate? Hull { get; set; }

    public int CurrentSlotIndex { get; private set; } = int.MinValue;

    public float SpeedModifier => _speedModifier;

    public bool SuitEquipped { get; private set; }

    private DbConnection? Server => (DbManagerNode as Db.DbManager)?.Connection;

    public override void _PhysicsProcess(double delta)
    {
        if (MoveAction is null)
            return;

        var direction = MoveAction.ValueAxis2D;

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

        TrackCurrentRoom();

        if (_probeData is not null)
        {
            _probeData.Position = GlobalPosition;
            Interaction.Process();
        }
    }

    // Runs once the ancestor providing InteractionService has called Provide();
    // until then the interaction system stays dormant (harnesses without a
    // provider still get full movement).
    public void OnResolved()
    {
        _probeData = new ProbeData(_entityId, GlobalPosition);
        Interaction.UpdateProbeData(_probeData);
    }

    public override void _Ready()
    {
        if (Sprite is not null)
            _baseSpriteColor = Sprite.Color;

        if (Server is { } svr)
        {
            _networkSync = new MovementNetworkSync(svr);
            _networkSync.Initialize(GlobalPosition);

            if (svr.Identity is { } identity)
            {
                var playerRow = svr.Db.Players.Identity.Find(identity);
                if (playerRow is { } p)
                    _entityId = p.PlayerEntityId;
            }

            svr.Db.Players.OnUpdate += OnPlayerUpdated;
        }
    }

    public override void _ExitTree()
    {
        if (Server is { } svr)
            svr.Db.Players.OnUpdate -= OnPlayerUpdated;
    }

    private void OnPlayerUpdated(EventContext ctx, StdbPlayer oldPlayer, StdbPlayer newPlayer)
    {
        _ = newPlayer.IsConnected;
    }

    // The suit trades speed for tank capacity; the tint makes equipped state
    // readable on every client (single-piece sprite per GDD §7.1).
    public void SetSuitEquipped(bool equipped, float speedFactor)
    {
        SuitEquipped = equipped;
        _speedModifier = equipped ? speedFactor : 1.0f;

        if (Sprite is not null)
            Sprite.Color = equipped ? SuitedColor : _baseSpriteColor;
    }

    // Reports room transitions only — the reducer must not be spammed with
    // per-frame calls while the player stands still or moves within a room.
    private void TrackCurrentRoom()
    {
        if (Hull is null)
            return;

        _roomLocator ??= new Nomad.Game.Ship.RoomLocator(Hull);

        var slot = _roomLocator.SlotAt(GlobalPosition);
        if (slot == CurrentSlotIndex)
            return;

        CurrentSlotIndex = slot;
        Server?.Reducers.SetPlayerRoom(slot);
    }
}
