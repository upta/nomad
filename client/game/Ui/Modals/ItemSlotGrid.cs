#nullable enable

namespace Nomad.Game.Ui;

using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Items;

// Grid of focusable item slots fed by SetSlots. Slots update in place — a
// rebuild would steal keyboard focus from an open modal (PowerRouterModal
// lesson).
[Meta(typeof(IAutoNode))]
public partial class ItemSlotGrid : GridContainer
{
    private readonly List<ItemSlotButton> _buttons = [];

    public override void _Notification(int what) => this.Notify(what);

    public event Action<int>? SlotPressed;

    [Export]
    public PackedScene SlotScene { get; set; } = null!;

    public IReadOnlyList<ItemSlotButton> Buttons => _buttons;

    public int OccupiedCount { get; private set; }

    public bool FocusFirstOccupied()
    {
        foreach (var button in _buttons)
        {
            if (button.IsOccupied)
            {
                button.GrabFocus();
                return true;
            }
        }

        return false;
    }

    public void SetSlots(IReadOnlyList<ItemType?> slots)
    {
        while (_buttons.Count < slots.Count)
        {
            var button = SlotScene.Instantiate<ItemSlotButton>();
            AddChild(button);
            var index = _buttons.Count;
            button.Pressed += () => SlotPressed?.Invoke(index);
            _buttons.Add(button);
        }

        while (_buttons.Count > slots.Count)
        {
            var last = _buttons[^1];
            _buttons.RemoveAt(_buttons.Count - 1);
            last.QueueFree();
        }

        var occupied = 0;
        for (var i = 0; i < _buttons.Count; i++)
        {
            _buttons[i].SetItem(slots[i]);
            if (slots[i] is not null)
                occupied++;
        }

        OccupiedCount = occupied;
    }
}
