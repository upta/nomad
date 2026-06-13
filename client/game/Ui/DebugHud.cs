#nullable enable

namespace Nomad.Game.Ui;

using System;
using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;

// A floating dev affordance: a top-right "Reset World" button that asks the
// server to wipe run state back to the Init seed. Main owns the reducer call —
// this scene only surfaces the request as a C# event.
[Meta(typeof(IAutoNode))]
public partial class DebugHud : CanvasLayer
{
    public override void _Notification(int what) => this.Notify(what);

    public event Action? ResetRequested;

    [Node]
    public IButton ResetButton { get; set; } = default!;

    public void OnReady()
    {
        ResetButton.Pressed += OnResetPressed;
    }

    public override void _ExitTree()
    {
        ResetButton.Pressed -= OnResetPressed;
    }

    private void OnResetPressed() => ResetRequested?.Invoke();
}
