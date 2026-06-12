#nullable enable

namespace Nomad.Game.Ui;

using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoNode))]
public partial class RoomInfoModal : PanelContainer, IRoomModal
{
    public override void _Notification(int what) => this.Notify(what);

    [Node]
    public ILabel PowerLabel { get; set; } = default!;

    [Node]
    public ILabel PressureLabel { get; set; } = default!;

    [Node]
    public ILabel TerminalLabel { get; set; } = default!;

    [Node]
    public ILabel TitleLabel { get; set; } = default!;

    public void Initialize(RoomModalInfo info)
    {
        TitleLabel.Text = info.Label;
        TerminalLabel.Text = $"Terminal: {info.TerminalType}";
        PowerLabel.Text = $"Power: {(info.IsPowered ? "Online" : "Offline")}";
        PressureLabel.Text = $"Pressure: {(info.IsPressurized ? "Nominal" : "Lost")}";
    }
}
