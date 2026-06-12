#nullable enable

namespace Nomad.Game.Ui;

using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Ship;

[Meta(typeof(IAutoNode))]
public partial class PowerRouterModal : PanelContainer, IRoomModal
{
    private readonly List<PowerRouterRow> _rows = [];

    public override void _Notification(int what) => this.Notify(what);

    [Dependency]
    private PowerGridService Power => this.DependOn<PowerGridService>();

    [Node]
    public IVBoxContainer RowContainer { get; set; } = default!;

    [Export]
    public PackedScene RowScene { get; set; } = null!;

    [Node]
    public ILabel StatusLabel { get; set; } = default!;

    [Node]
    public ILabel TitleLabel { get; set; } = default!;

    public override void _ExitTree()
    {
        Power.Changed -= OnPowerChanged;
    }

    public void Initialize(RoomModalInfo info)
    {
        TitleLabel.Text = info.Label;
    }

    public void OnResolved()
    {
        Power.Changed += OnPowerChanged;
        BuildRows();
        if (_rows.Count > 0)
            Callable.From(_rows[0].FocusToggle).CallDeferred();
    }

    // Rows are updated in place on power changes — rebuilding would steal
    // keyboard focus from the row being toggled.
    private void BuildRows()
    {
        foreach (var row in _rows)
            row.QueueFree();
        _rows.Clear();

        foreach (var room in Power.Rooms)
        {
            var row = RowScene.Instantiate<PowerRouterRow>();
            RowContainer.AddChildEx(row);
            var slot = room.Slot;
            row.Bind(room, () => Power.RequestToggleBreaker(slot));
            _rows.Add(row);
        }

        UpdateStatus();
    }

    private void OnPowerChanged()
    {
        var rooms = Power.Rooms;
        if (rooms.Count != _rows.Count)
        {
            BuildRows();
            return;
        }

        for (var i = 0; i < rooms.Count; i++)
            _rows[i].Update(rooms[i]);

        UpdateStatus();
    }

    private void UpdateStatus() =>
        StatusLabel.Text =
            $"Output {Power.ReactorOutput} / Demand {Power.TotalDemand} — {Power.Status}";
}
