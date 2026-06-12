#nullable enable

namespace Nomad.Game.Ui;

using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Character;

[Meta(typeof(IAutoNode))]
public partial class CloningModal : PanelContainer, IRoomModal
{
    private const string BiomassTypeId = "Biomass";

    private readonly List<CloningRow> _rows = [];
    private int _roomSlotIndex = -1;

    public override void _Notification(int what) => this.Notify(what);

    [Dependency]
    private Items.InventoryService Inventory => this.DependOn<Items.InventoryService>();

    [Dependency]
    private VitalsService Vitals => this.DependOn<VitalsService>();

    [Node]
    public DepositRow BiomassDepositRow { get; set; } = default!;

    [Node]
    public ILabel BiomassLabel { get; set; } = default!;

    [Node]
    public ILabel EmptyLabel { get; set; } = default!;

    [Node]
    public IVBoxContainer RowContainer { get; set; } = default!;

    [Export]
    public PackedScene RowScene { get; set; } = null!;

    [Node]
    public ILabel TitleLabel { get; set; } = default!;

    public int DeadRowCount => _rows.Count;

    public int ShownBiomass { get; private set; }

    public override void _ExitTree()
    {
        Vitals.Changed -= OnVitalsChanged;
        Inventory.Changed -= OnInventoryChanged;
    }

    public void Initialize(RoomModalInfo info)
    {
        TitleLabel.Text = info.Label;
        _roomSlotIndex = info.SlotIndex;
    }

    public void OnResolved()
    {
        Vitals.Changed += OnVitalsChanged;
        Inventory.Changed += OnInventoryChanged;
        BiomassDepositRow.Bind(() => Inventory.RequestLoad(BiomassTypeId, _roomSlotIndex));
        UpdateDepositRow();
        BuildRows();
        if (_rows.Count > 0)
            Callable.From(_rows[0].FocusClone).CallDeferred();
        else
            Callable.From(BiomassDepositRow.FocusDeposit).CallDeferred();
    }

    // Rows rebuild on roster changes (a respawned crewmate's row disappears);
    // focus moves to the first remaining row so keyboard flow survives.
    private void BuildRows()
    {
        foreach (var row in _rows)
            row.QueueFree();
        _rows.Clear();

        foreach (var entry in Vitals.DeadCrew)
        {
            var row = RowScene.Instantiate<CloningRow>();
            RowContainer.AddChildEx(row);
            var key = entry.Key;
            row.Bind(entry.Label, () => Vitals.RequestRespawn(key));
            _rows.Add(row);
        }

        ShownBiomass = Vitals.Biomass;
        BiomassLabel.Text = $"Biomass: {ShownBiomass}";
        EmptyLabel.Visible = _rows.Count == 0;
    }

    private void OnInventoryChanged() => UpdateDepositRow();

    private void OnVitalsChanged()
    {
        var hadRows = _rows.Count;
        BuildRows();
        if (_rows.Count > 0 && _rows.Count != hadRows)
            Callable.From(_rows[0].FocusClone).CallDeferred();
    }

    private void UpdateDepositRow()
    {
        var held = Inventory.CountOf(BiomassTypeId);
        BiomassDepositRow.Update($"Biomass ({held} held)", held > 0);
    }
}
