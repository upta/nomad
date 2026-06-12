#nullable enable

namespace Nomad.Game.Ui;

using System;
using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Ship;

[Meta(typeof(IAutoNode))]
public partial class PowerRouterRow : HBoxContainer
{
    private Action? _onToggle;

    public override void _Notification(int what) => this.Notify(what);

    [Node]
    public ILabel DrawLabel { get; set; } = default!;

    [Node]
    public ILabel NameLabel { get; set; } = default!;

    [Node]
    public ILabel PoweredLabel { get; set; } = default!;

    [Node]
    public IButton ToggleButton { get; set; } = default!;

    public void OnReady()
    {
        ToggleButton.Pressed += OnTogglePressed;
    }

    public void Bind(PowerRoomEntry room, Action onToggle)
    {
        _onToggle = onToggle;
        Update(room);
    }

    public void FocusToggle() => ToggleButton.GrabFocus();

    public void Update(PowerRoomEntry room)
    {
        NameLabel.Text = room.Label;
        DrawLabel.Text = $"Draw {room.Draw}";
        PoweredLabel.Text = room.IsPowered ? "Powered" : "Dark";
        ToggleButton.Text = room.BreakerOn ? "On" : "Off";
    }

    private void OnTogglePressed() => _onToggle?.Invoke();
}
