#nullable enable

namespace Nomad.Game.Ui;

using System;
using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoNode))]
public partial class RecipeRow : HBoxContainer
{
    private Action? _onQueue;

    public override void _Notification(int what) => this.Notify(what);

    [Node]
    public ILabel IngredientsLabel { get; set; } = default!;

    [Node]
    public ILabel NameLabel { get; set; } = default!;

    [Node]
    public IButton QueueButton { get; set; } = default!;

    public bool QueueEnabled => !QueueButton.Disabled;

    public void OnReady()
    {
        QueueButton.Pressed += OnQueuePressed;
    }

    public void Bind(Action onQueue)
    {
        _onQueue = onQueue;
    }

    public void FocusQueue() => QueueButton.GrabFocus();

    public void Update(string label, string ingredients, bool enabled)
    {
        NameLabel.Text = label;
        IngredientsLabel.Text = ingredients;
        QueueButton.Disabled = !enabled;
    }

    private void OnQueuePressed() => _onQueue?.Invoke();
}
