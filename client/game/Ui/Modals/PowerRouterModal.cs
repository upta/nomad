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
    private const string FuelCellTypeId = "FuelCell";

    private static readonly Color FuelDryColor = new(1f, 0.45f, 0.4f);
    private static readonly Color FuelNominalColor = new(1f, 1f, 1f);

    private readonly List<PowerRouterRow> _rows = [];
    private int _roomSlotIndex = -1;

    public override void _Notification(int what) => this.Notify(what);

    [Dependency]
    private Items.InventoryService Inventory => this.DependOn<Items.InventoryService>();

    [Dependency]
    private PowerGridService Power => this.DependOn<PowerGridService>();

    [Node]
    public DepositRow FuelDepositRow { get; set; } = default!;

    [Node]
    public ILabel FuelLabel { get; set; } = default!;

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
        Inventory.Changed -= OnInventoryChanged;
    }

    public void Initialize(RoomModalInfo info)
    {
        TitleLabel.Text = info.Label;
        _roomSlotIndex = info.SlotIndex;
    }

    public void OnResolved()
    {
        Power.Changed += OnPowerChanged;
        Inventory.Changed += OnInventoryChanged;
        FuelDepositRow.Bind(() => Inventory.RequestLoad(FuelCellTypeId, _roomSlotIndex));
        UpdateDepositRow();
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

    private void OnInventoryChanged() => UpdateDepositRow();

    private void UpdateDepositRow()
    {
        var held = Inventory.CountOf(FuelCellTypeId);
        FuelDepositRow.Update($"Fuel Cell ({held} held)", held > 0);
    }

    private void UpdateStatus()
    {
        StatusLabel.Text =
            $"Output {Power.ReactorOutput} / Demand {Power.TotalDemand} — {Power.Status}";
        FuelLabel.Text = Power.IsBurningDry ? $"Fuel: {Power.Fuel} — DRY" : $"Fuel: {Power.Fuel}";
        FuelLabel.Modulate = Power.IsBurningDry ? FuelDryColor : FuelNominalColor;
    }
}
