#nullable enable

namespace Nomad.Game.Ui;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Items;

// Focusable wrapper around the shared ItemSlotPanel for modal slot grids.
// Empty slots drop out of focus navigation so arrows skip straight between
// occupied slots.
[Meta(typeof(IAutoNode))]
public partial class ItemSlotButton : Button
{
    public override void _Notification(int what) => this.Notify(what);

    [Node]
    public ItemSlotPanel Panel { get; set; } = default!;

    public bool IsOccupied => Panel.IsOccupied;

    public void OnReady()
    {
        FocusEntered += () => Panel.SetSelected(true);
        FocusExited += () => Panel.SetSelected(false);
    }

    public void SetItem(ItemType? type)
    {
        Panel.SetItem(type);
        Disabled = type is null;
        FocusMode = type is null ? FocusModeEnum.None : FocusModeEnum.All;
    }
}
