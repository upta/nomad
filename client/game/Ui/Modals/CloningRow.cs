#nullable enable

namespace Nomad.Game.Ui;

using System;
using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoNode))]
public partial class CloningRow : HBoxContainer
{
    private Action? _onClone;

    public override void _Notification(int what) => this.Notify(what);

    [Node]
    public IButton CloneButton { get; set; } = default!;

    [Node]
    public ILabel NameLabel { get; set; } = default!;

    public void OnReady()
    {
        CloneButton.Pressed += OnClonePressed;
    }

    public void Bind(string label, Action onClone)
    {
        NameLabel.Text = label;
        _onClone = onClone;
    }

    public void FocusClone() => CloneButton.GrabFocus();

    private void OnClonePressed() => _onClone?.Invoke();
}
