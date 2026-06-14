#nullable enable

namespace Nomad.Game.Ui;

using System;
using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;

// Floating dev affordances (top-right): "Reset World" wipes run state to the
// Init seed; "Ignite Fire" starts a fire on the local player so the hazard
// system is playable before in-game ignition sources (events/breaches) land;
// "Toggle Node" debug-switches between the Quiet ship-in-space node and the
// Planetside surface (the eventual jump target in Phase 6). Main owns the
// reducer calls — this scene only surfaces the requests as events.
[Meta(typeof(IAutoNode))]
public partial class DebugHud : CanvasLayer
{
    public override void _Notification(int what) => this.Notify(what);

    public event Action? IgniteFireRequested;

    public event Action? NodeToggleRequested;

    public event Action? ResetRequested;

    [Node]
    public IButton IgniteFireButton { get; set; } = default!;

    [Node]
    public IButton NodeToggleButton { get; set; } = default!;

    [Node]
    public IButton ResetButton { get; set; } = default!;

    public void OnReady()
    {
        ResetButton.Pressed += OnResetPressed;
        IgniteFireButton.Pressed += OnIgniteFirePressed;
        NodeToggleButton.Pressed += OnNodeTogglePressed;
    }

    public override void _ExitTree()
    {
        ResetButton.Pressed -= OnResetPressed;
        IgniteFireButton.Pressed -= OnIgniteFirePressed;
        NodeToggleButton.Pressed -= OnNodeTogglePressed;
    }

    private void OnResetPressed() => ResetRequested?.Invoke();

    private void OnIgniteFirePressed() => IgniteFireRequested?.Invoke();

    private void OnNodeTogglePressed() => NodeToggleRequested?.Invoke();
}
