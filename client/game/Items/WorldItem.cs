#nullable enable

namespace Nomad.Game.Items;

using System;
using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Interaction;

[Meta(typeof(IAutoNode))]
public partial class WorldItem : Node2D
{
    private string _label = "";

    public override void _Notification(int what) => this.Notify(what);

    public event Action<int>? Interacted;

    [Node]
    public ILabel Glyph { get; set; } = default!;

    public int ItemId { get; private set; }

    [Node]
    public IColorRect Sprite { get; set; } = default!;

    [Node]
    public InteractTarget Target { get; set; } = default!;

    public void OnReady()
    {
        // GhostAccessible stays false (default) — ghosts cannot pick up.
        Target.Registration = new CallbackInteractionRegistration(
            () => GlobalPosition,
            () => $"Pick up {_label}",
            _ => Interacted?.Invoke(ItemId)
        );
    }

    public void SetItem(int itemId, ItemType type)
    {
        ItemId = itemId;
        _label = type.Label;
        Sprite.Color = type.Color;
        Glyph.Text = type.Glyph;
    }
}
