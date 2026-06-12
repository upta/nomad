#nullable enable

namespace Nomad.Game.Ui;

using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Guide;
using Nomad.Game.Items;

[Meta(typeof(IAutoNode))]
public partial class HotbarHud : CanvasLayer
{
    private readonly List<ItemSlotPanel> _panels = [];

    public override void _Notification(int what) => this.Notify(what);

    public event Action? DropRequested;

    [Dependency]
    private InventoryService Inventory => this.DependOn<InventoryService>();

    [Export]
    public GuideActionBinding DropAction { get; set; } = null!;

    [Node]
    public ItemSlotPanel Slot0 { get; set; } = default!;

    [Node]
    public ItemSlotPanel Slot1 { get; set; } = default!;

    [Export]
    public GuideActionBinding Slot1Action { get; set; } = null!;

    [Node]
    public ItemSlotPanel Slot2 { get; set; } = default!;

    [Export]
    public GuideActionBinding Slot2Action { get; set; } = null!;

    [Node]
    public ItemSlotPanel Slot3 { get; set; } = default!;

    [Export]
    public GuideActionBinding Slot3Action { get; set; } = null!;

    [Export]
    public GuideActionBinding Slot4Action { get; set; } = null!;

    // Handed by Main/harness in their ready hooks — node exports don't bind
    // across scene-instance boundaries.
    public ItemTypeRegistry? Registry { get; set; }

    public override void _ExitTree()
    {
        Inventory.Changed -= Sync;
        Slot1Action.JustTriggered -= OnSlot1;
        Slot2Action.JustTriggered -= OnSlot2;
        Slot3Action.JustTriggered -= OnSlot3;
        Slot4Action.JustTriggered -= OnSlot4;
        DropAction.JustTriggered -= OnDrop;
    }

    public override void _Ready()
    {
        _panels.AddRange([Slot0, Slot1, Slot2, Slot3]);
    }

    public Godot.Collections.Dictionary GetObservedState()
    {
        var slots = new Godot.Collections.Array();
        foreach (var panel in _panels)
        {
            slots.Add(
                new Godot.Collections.Dictionary
                {
                    ["occupied"] = panel.IsOccupied,
                    ["glyph"] = panel.Glyph.Text,
                    ["color_r"] = panel.Fill.Color.R,
                    ["color_g"] = panel.Fill.Color.G,
                    ["color_b"] = panel.Fill.Color.B,
                    ["selected"] = panel.IsSelected,
                }
            );
        }

        return new Godot.Collections.Dictionary
        {
            ["slot_count"] = Inventory.HotbarSlotCount,
            ["selected"] = Inventory.SelectedSlot,
            ["slots"] = slots,
        };
    }

    public void OnResolved()
    {
        Inventory.Changed += Sync;
        Slot1Action.JustTriggered += OnSlot1;
        Slot2Action.JustTriggered += OnSlot2;
        Slot3Action.JustTriggered += OnSlot3;
        Slot4Action.JustTriggered += OnSlot4;
        DropAction.JustTriggered += OnDrop;
        Sync();
    }

    private void OnDrop() => DropRequested?.Invoke();

    private void OnSlot1() => Inventory.SelectSlot(0);

    private void OnSlot2() => Inventory.SelectSlot(1);

    private void OnSlot3() => Inventory.SelectSlot(2);

    private void OnSlot4() => Inventory.SelectSlot(3);

    private void Sync()
    {
        var slots = Inventory.Slots;
        if (slots.Count != _panels.Count)
        {
            GD.PushWarning(
                $"[HotbarHud] Config wants {slots.Count} slots; scene declares {_panels.Count}."
            );
        }

        for (var i = 0; i < _panels.Count; i++)
        {
            var typeId = i < slots.Count ? slots[i] : null;
            _panels[i].SetItem(typeId is null ? null : Registry?.Find(typeId));
            _panels[i].SetSelected(i == Inventory.SelectedSlot);
        }
    }
}
