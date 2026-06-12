#nullable enable

namespace Nomad.Game.Ui;

using System;
using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoNode))]
public partial class DepositRow : HBoxContainer
{
    private Action? _onDeposit;

    public override void _Notification(int what) => this.Notify(what);

    [Node]
    public IButton DepositButton { get; set; } = default!;

    [Node]
    public ILabel NameLabel { get; set; } = default!;

    public bool DepositEnabled => !DepositButton.Disabled;

    public void OnReady()
    {
        DepositButton.Pressed += OnDepositPressed;
    }

    public void Bind(Action onDeposit)
    {
        _onDeposit = onDeposit;
    }

    public void FocusDeposit() => DepositButton.GrabFocus();

    public void Update(string label, bool enabled)
    {
        NameLabel.Text = label;
        DepositButton.Disabled = !enabled;
    }

    private void OnDepositPressed() => _onDeposit?.Invoke();
}
