#nullable enable

namespace Nomad.Game.Ui;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Guide;
using Nomad.Game.Ship;

[Meta(typeof(IAutoNode))]
public partial class ModalHost : CanvasLayer
{
    private Control? _current;
    private RoomModalInfo? _currentInfo;

    public override void _Notification(int what) => this.Notify(what);

    [Dependency]
    private GuideService Guide => this.DependOn<GuideService>();

    [Export]
    public GuideActionBinding CancelAction { get; set; } = null!;

    [Export]
    public PackedScene CloningModal { get; set; } = null!;

    [Export]
    public PackedScene FabricatorModal { get; set; } = null!;

    [Export]
    public PackedScene InfoModal { get; set; } = null!;

    [Export]
    public PackedScene PowerRouterModal { get; set; } = null!;

    [Export]
    public PackedScene StarChartModal { get; set; } = null!;

    [Export]
    public InputModeContext UiModeContext { get; set; } = null!;

    public RoomModalInfo? CurrentInfo => _currentInfo;

    public string CurrentTitle => _currentInfo?.Label ?? "";

    public bool IsOpen => _current is not null;

    public override void _ExitTree()
    {
        CancelAction.JustTriggered -= OnCancelTriggered;

        if (_current is not null)
        {
            Guide.PopContext();
        }
    }

    public void Close()
    {
        if (_current is null)
        {
            return;
        }

        _current.QueueFree();
        _current = null;
        _currentInfo = null;
        Guide.PopContext();
    }

    public void Open(RoomModalInfo info)
    {
        if (IsOpen)
        {
            return;
        }

        var modal = SceneFor(info.TerminalType).Instantiate<Control>();
        AddChild(modal);

        if (modal is IRoomModal roomModal)
        {
            roomModal.Initialize(info);
        }

        _current = modal;
        _currentInfo = info;
        Guide.PushContext(UiModeContext, exclusive: true);
    }

    public void OnResolved()
    {
        CancelAction.JustTriggered += OnCancelTriggered;
    }

    // Deferred to avoid re-entrant mapping context changes during GUIDE's
    // input-processing cycle.
    private void OnCancelTriggered() => Callable.From(Close).CallDeferred();

    private PackedScene SceneFor(TerminalType type) =>
        type switch
        {
            TerminalType.StarChart => StarChartModal,
            TerminalType.PowerRouter => PowerRouterModal,
            TerminalType.Fabricator => FabricatorModal,
            TerminalType.Cloning => CloningModal,
            _ => InfoModal,
        };
}
