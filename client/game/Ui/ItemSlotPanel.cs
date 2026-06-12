#nullable enable

namespace Nomad.Game.Ui;

using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Items;

// Shared slot visual for the hotbar and (later) storage grids: type color
// fill + glyph when occupied, dim fill when empty, ring when selected.
[Meta(typeof(IAutoNode))]
public partial class ItemSlotPanel : Control
{
    public override void _Notification(int what) => this.Notify(what);

    [Export]
    public Color EmptyFillColor { get; set; } = new(0.12f, 0.13f, 0.16f);

    [Node]
    public IColorRect Fill { get; set; } = default!;

    [Node]
    public ILabel Glyph { get; set; } = default!;

    [Node]
    public IColorRect SelectionRing { get; set; } = default!;

    public bool IsOccupied { get; private set; }

    public bool IsSelected { get; private set; }

    public void SetItem(ItemType? type)
    {
        IsOccupied = type is not null;
        Fill.Color = type?.Color ?? EmptyFillColor;
        Glyph.Text = type?.Glyph ?? "";
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        SelectionRing.Visible = selected;
    }
}
