#nullable enable

namespace Nomad.Game.Map;

using System;
using System.Collections.Generic;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Db;
using Godot;
using Ship;
using StdbGs = SpacetimeDB.Types.GridStatus;
using StdbPg = SpacetimeDB.Types.PowerGrid;
using StdbRa = SpacetimeDB.Types.RoomAssignment;
using StdbRt = SpacetimeDB.Types.RoomTypeId;

[Meta(typeof(IAutoNode))]
public partial class ShipGrid : Node2D
{
    private const float RoomTintAlpha = 0.45f;
    private const int TileSize = 32;

    private static readonly Color DefaultRoomColor = new(0.30f, 0.33f, 0.38f);
    private static readonly Vector2I FloorTile = new(0, 0);
    private static readonly Vector2I WallTile = new(1, 0);

    private readonly Dictionary<int, StdbRa> _assignments = [];
    private readonly Dictionary<int, Ship.Breaker> _breakers = [];
    private readonly Dictionary<int, Ship.SuitRack> _suitRacks = [];
    private readonly HashSet<Vector2I> _floorCells = [];
    private readonly Dictionary<int, Ship.Terminal> _terminals = [];
    private readonly HashSet<Vector2I> _wallCells = [];
    private double _flickerElapsed;
    private bool _flickerOn;
    private Font _font = null!;
    private StdbGs _gridStatus = StdbGs.Stable;

    public override void _Notification(int what) => this.Notify(what);

    public event Action<Ship.Breaker>? BreakerInteracted;

    public event Action<Ship.SuitRack>? SuitRackInteracted;

    public event Action<Ship.Terminal>? TerminalInteracted;

    [Node]
    public ITileMapLayer FloorLayer { get; set; } = default!;

    [Node]
    public ITileMapLayer WallLayer { get; set; } = default!;

    [Export]
    public Node? DbManagerNode { get; set; }

    [Export]
    public HullTemplate? HullTemplate { get; set; }

    [Export]
    public RoomTypeRegistry? RoomTypeRegistry { get; set; }

    [Export]
    public PackedScene? BreakerScene { get; set; }

    [Export]
    public PackedScene? SuitRackScene { get; set; }

    [Export]
    public PackedScene? TerminalScene { get; set; }

    [Export(PropertyHint.Range, "0,1")]
    public float DepressurizedBlend { get; set; } = 0.55f;

    [Export(PropertyHint.Range, "0,1")]
    public float FlickerDimFactor { get; set; } = 0.6f;

    [Export]
    public float FlickerIntervalSeconds { get; set; } = 0.12f;

    [Export(PropertyHint.Range, "0,1")]
    public float UnpoweredDimFactor { get; set; } = 0.35f;

    [Export]
    public Color VacuumTint { get; set; } = new(0.35f, 0.45f, 0.6f);

    public int BreakerCount => _breakers.Count;

    public int FlickerCycles { get; private set; }

    public int TerminalCount => _terminals.Count;

    // The corridor network rides the room-assignment table as one extra slot
    // past the hull's room slots (Corvette: 7); it has no RoomSlot entry, so
    // terminal/breaker spawning naturally skips it.
    private int CorridorSlotIndex => HullTemplate?.RoomSlots.Count ?? -1;

    private Vector2I GridOffset =>
        HullTemplate is null
            ? Vector2I.Zero
            : new(HullTemplate.GridWidth / 2, HullTemplate.GridHeight / 2);

    private DbConnection? Server => (DbManagerNode as DbManager)?.Connection;

    public void OnReady()
    {
        _font = ThemeDB.FallbackFont;

        if (Server is { } svr)
            SubscribeToAssignments(svr);

        BuildMap();
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (HullTemplate is null)
            return;

        var offset = GridOffset;

        foreach (var slot in HullTemplate.RoomSlots)
        {
            var rect = new Rect2(
                (slot.PositionX - offset.X) * TileSize,
                (slot.PositionY - offset.Y) * TileSize,
                slot.Width * TileSize,
                slot.Height * TileSize
            );

            DrawRect(rect, new Color(GetRoomColor(slot.SlotIndex), RoomTintAlpha), true);

            var label = GetRoomLabel(slot.SlotIndex);
            if (label.Length > 0)
            {
                var labelSize = _font.GetStringSize(label, HorizontalAlignment.Center);
                var labelPos = rect.Position + (rect.Size - labelSize) / 2f;
                DrawString(_font, labelPos, label, HorizontalAlignment.Left);
            }
        }

        foreach (var corridor in HullTemplate.Corridors)
        {
            var rect = new Rect2(
                (corridor.PositionX - offset.X) * TileSize,
                (corridor.PositionY - offset.Y) * TileSize,
                corridor.Width * TileSize,
                corridor.Height * TileSize
            );

            DrawRect(rect, new Color(GetRoomColor(CorridorSlotIndex), RoomTintAlpha), true);
        }
    }

    public override void _ExitTree()
    {
        if (Server is { } svr)
        {
            svr.Db.RoomAssignments.OnInsert -= OnAssignmentInserted;
            svr.Db.RoomAssignments.OnUpdate -= OnAssignmentUpdated;
            svr.Db.PowerGrids.OnInsert -= OnPowerGridInserted;
            svr.Db.PowerGrids.OnUpdate -= OnPowerGridUpdated;
        }
    }

    // Overload warning renders as flickering lights: powered rooms dip toward
    // their dimmed color on a fixed interval until the grid settles.
    public override void _Process(double delta)
    {
        if (_gridStatus != StdbGs.Overload)
        {
            if (_flickerOn)
            {
                _flickerOn = false;
                QueueRedraw();
            }
            return;
        }

        _flickerElapsed += delta;
        if (_flickerElapsed < FlickerIntervalSeconds)
            return;

        _flickerElapsed = 0;
        _flickerOn = !_flickerOn;
        if (_flickerOn)
            FlickerCycles++;
        QueueRedraw();
    }

    public void BindToServer(DbManager dbManager)
    {
        DbManagerNode = dbManager;
        if (Server is { } svr)
            SubscribeToAssignments(svr);
    }

    public Godot.Collections.Dictionary GetObservedRoomState()
    {
        var roomList = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        if (HullTemplate is not null)
        {
            foreach (var slot in HullTemplate.RoomSlots)
            {
                var assigned = _assignments.TryGetValue(slot.SlotIndex, out var ra);
                var entry = new Godot.Collections.Dictionary
                {
                    ["slot_index"] = slot.SlotIndex,
                    ["color_r"] = GetRoomColor(slot.SlotIndex).R,
                    ["color_g"] = GetRoomColor(slot.SlotIndex).G,
                    ["color_b"] = GetRoomColor(slot.SlotIndex).B,
                    ["label"] = GetRoomLabel(slot.SlotIndex),
                    ["is_powered"] = assigned && ra!.IsPowered,
                    ["breaker_on"] = assigned && ra!.BreakerOn,
                    ["is_pressurized"] = assigned && ra!.IsPressurized,
                };
                roomList.Add(entry);
            }

            var corridorAssigned = _assignments.TryGetValue(CorridorSlotIndex, out var cra);
            roomList.Add(
                new Godot.Collections.Dictionary
                {
                    ["slot_index"] = CorridorSlotIndex,
                    ["color_r"] = GetRoomColor(CorridorSlotIndex).R,
                    ["color_g"] = GetRoomColor(CorridorSlotIndex).G,
                    ["color_b"] = GetRoomColor(CorridorSlotIndex).B,
                    ["label"] = GetRoomLabel(CorridorSlotIndex),
                    ["is_powered"] = corridorAssigned && cra!.IsPowered,
                    ["breaker_on"] = corridorAssigned && cra!.BreakerOn,
                    ["is_pressurized"] = corridorAssigned && cra!.IsPressurized,
                }
            );
        }

        return new Godot.Collections.Dictionary
        {
            ["rooms"] = roomList,
            ["map"] = new Godot.Collections.Dictionary
            {
                ["floor_count"] = _floorCells.Count,
                ["wall_count"] = _wallCells.Count,
                ["door_count"] = HullTemplate?.Doors.Count ?? 0,
                ["terminal_count"] = _terminals.Count,
                ["breaker_count"] = _breakers.Count,
                ["suit_rack_count"] = _suitRacks.Count,
            },
            ["power"] = new Godot.Collections.Dictionary
            {
                ["status"] = _gridStatus.ToString(),
                ["flicker_cycles"] = FlickerCycles,
            },
        };
    }

    public void SetSuitRackState(bool suitTaken)
    {
        foreach (var rack in _suitRacks.Values)
            rack.SetSuitTaken(suitTaken);
    }

    public void SetTestAssignment(
        int slotIndex,
        string roomTypeId,
        bool isPowered = true,
        bool breakerOn = true,
        bool isPressurized = true
    )
    {
        _assignments[slotIndex] = new StdbRa
        {
            SlotIndex = slotIndex,
            RoomTypeId = Enum.Parse<StdbRt>(roomTypeId),
            IsPowered = isPowered,
            IsPressurized = isPressurized,
            BreakerOn = breakerOn,
            Health = 100f,
        };
        EnsureRoomNodes(slotIndex);
        QueueRedraw();
    }

    public void SetTestGridStatus(string status)
    {
        _gridStatus = Enum.Parse<StdbGs>(status);
        QueueRedraw();
    }

    public void SetTestPower(int slotIndex, bool breakerOn, bool isPowered)
    {
        if (!_assignments.TryGetValue(slotIndex, out var ra))
            return;

        ra.BreakerOn = breakerOn;
        ra.IsPowered = isPowered;
        EnsureRoomNodes(slotIndex);
        QueueRedraw();
    }

    public void SetTestPressurization(int slotIndex, bool isPressurized)
    {
        if (!_assignments.TryGetValue(slotIndex, out var ra))
            return;

        ra.IsPressurized = isPressurized;
        EnsureRoomNodes(slotIndex);
        QueueRedraw();
    }

    private void AddFloorRect(int x, int y, int width, int height)
    {
        for (var dx = 0; dx < width; dx++)
        for (var dy = 0; dy < height; dy++)
            _floorCells.Add(new Vector2I(x + dx, y + dy));
    }

    // Floors come straight from the hull data (rooms, corridors, doors); walls
    // are derived as every cell touching a floor cell, so the layout stays
    // sealed no matter how the .tres is reshaped.
    private void BuildMap()
    {
        if (HullTemplate is null)
            return;

        _floorCells.Clear();
        _wallCells.Clear();

        foreach (var slot in HullTemplate.RoomSlots)
            AddFloorRect(slot.PositionX, slot.PositionY, slot.Width, slot.Height);

        foreach (var corridor in HullTemplate.Corridors)
            AddFloorRect(corridor.PositionX, corridor.PositionY, corridor.Width, corridor.Height);

        foreach (var door in HullTemplate.Doors)
            _floorCells.Add(door);

        foreach (var cell in _floorCells)
        {
            for (var dx = -1; dx <= 1; dx++)
            for (var dy = -1; dy <= 1; dy++)
            {
                var neighbor = cell + new Vector2I(dx, dy);
                if (!_floorCells.Contains(neighbor))
                    _wallCells.Add(neighbor);
            }
        }

        var offset = GridOffset;
        foreach (var cell in _floorCells)
            FloorLayer.SetCell(cell - offset, 0, FloorTile);
        foreach (var cell in _wallCells)
            WallLayer.SetCell(cell - offset, 0, WallTile);
    }

    // Breakers are data-driven like terminals (one per assigned room slot),
    // mounted at the room's top-left interior cell so they read as wall
    // fixtures and stay clear of the slot-center terminal.
    private void EnsureBreaker(int slotIndex)
    {
        if (BreakerScene is null || HullTemplate is null)
            return;

        var slot = HullTemplate.RoomSlots.FirstOrDefault(s => s.SlotIndex == slotIndex);
        if (slot is null)
            return;

        if (!_breakers.TryGetValue(slotIndex, out var breaker))
        {
            breaker = BreakerScene.Instantiate<Ship.Breaker>();
            breaker.SlotIndex = slotIndex;

            var offset = GridOffset;
            breaker.Position = new Vector2(
                (slot.PositionX - offset.X + 0.5f) * TileSize,
                (slot.PositionY - offset.Y + 0.5f) * TileSize
            );

            breaker.Interacted += OnBreakerInteracted;
            AddChild(breaker);
            _breakers[slotIndex] = breaker;
        }

        if (
            _assignments.TryGetValue(slotIndex, out var ra)
            && RoomTypeRegistry?.Find(ra.RoomTypeId.ToString()) is { } rt
        )
        {
            breaker.SetState(rt.Label, ra.BreakerOn);
        }
    }

    private void EnsureRoomNodes(int slotIndex)
    {
        EnsureTerminal(slotIndex);
        EnsureBreaker(slotIndex);
        EnsureSuitRack(slotIndex);
    }

    // The suit rack is gear storage, so it spawns only in the CargoBay-
    // assigned slot — top-right interior cell, clear of the breaker (top-left)
    // and terminal (center).
    private void EnsureSuitRack(int slotIndex)
    {
        if (SuitRackScene is null || HullTemplate is null)
            return;

        var isCargoBay =
            _assignments.TryGetValue(slotIndex, out var ra) && ra.RoomTypeId == StdbRt.CargoBay;

        if (!isCargoBay)
        {
            if (_suitRacks.TryGetValue(slotIndex, out var stale))
            {
                _suitRacks.Remove(slotIndex);
                stale.Interacted -= OnSuitRackInteracted;
                stale.QueueFree();
            }
            return;
        }

        if (_suitRacks.ContainsKey(slotIndex))
            return;

        var slot = HullTemplate.RoomSlots.FirstOrDefault(s => s.SlotIndex == slotIndex);
        if (slot is null)
            return;

        var rack = SuitRackScene.Instantiate<Ship.SuitRack>();
        var offset = GridOffset;
        rack.Position = new Vector2(
            (slot.PositionX - offset.X + slot.Width - 0.5f) * TileSize,
            (slot.PositionY - offset.Y + 0.5f) * TileSize
        );

        rack.Interacted += OnSuitRackInteracted;
        AddChild(rack);
        _suitRacks[slotIndex] = rack;
    }

    private void OnSuitRackInteracted(Ship.SuitRack rack) => SuitRackInteracted?.Invoke(rack);

    // Terminals are data-driven (one per assigned room slot), spawned at the
    // slot's center cell and refreshed whenever the assignment changes.
    private void EnsureTerminal(int slotIndex)
    {
        if (TerminalScene is null || HullTemplate is null)
            return;

        var slot = HullTemplate.RoomSlots.FirstOrDefault(s => s.SlotIndex == slotIndex);
        if (slot is null)
            return;

        if (!_terminals.TryGetValue(slotIndex, out var terminal))
        {
            terminal = TerminalScene.Instantiate<Ship.Terminal>();
            terminal.SlotIndex = slotIndex;

            var offset = GridOffset;
            terminal.Position = new Vector2(
                (slot.PositionX - offset.X + slot.Width / 2f) * TileSize,
                (slot.PositionY - offset.Y + slot.Height / 2f) * TileSize
            );

            terminal.Interacted += OnTerminalInteracted;
            AddChild(terminal);
            _terminals[slotIndex] = terminal;
        }

        if (
            _assignments.TryGetValue(slotIndex, out var ra)
            && RoomTypeRegistry?.Find(ra.RoomTypeId.ToString()) is { } rt
        )
        {
            terminal.SetRoomState(rt.Label, rt.TerminalType, ra.IsPowered, ra.IsPressurized);
        }
    }

    private void OnBreakerInteracted(Ship.Breaker breaker) => BreakerInteracted?.Invoke(breaker);

    private void OnTerminalInteracted(Ship.Terminal terminal) =>
        TerminalInteracted?.Invoke(terminal);

    private Color GetRoomColor(int slotIndex)
    {
        if (!_assignments.TryGetValue(slotIndex, out var ra))
            return DefaultRoomColor;

        var color = RoomTypeRegistry?.Find(ra.RoomTypeId.ToString()) is { } rt
            ? rt.Color
            : DefaultRoomColor;

        // Tint before dimming so depressurized + unpowered compose: a hue
        // shift toward VacuumTint stays distinguishable from any Darkened().
        if (!ra.IsPressurized)
            color = color.Lerp(VacuumTint, DepressurizedBlend);

        if (!ra.IsPowered)
            return color.Darkened(UnpoweredDimFactor);

        return _flickerOn ? color.Darkened(FlickerDimFactor) : color;
    }

    private string GetRoomLabel(int slotIndex)
    {
        if (_assignments.TryGetValue(slotIndex, out var ra))
        {
            var roomId = ra.RoomTypeId.ToString();
            if (RoomTypeRegistry?.Find(roomId) is { } rt)
                return rt.Label;
        }

        return "";
    }

    private void OnAssignmentInserted(EventContext ctx, StdbRa ra)
    {
        _assignments[ra.SlotIndex] = ra;
        EnsureRoomNodes(ra.SlotIndex);
        QueueRedraw();
    }

    private void OnAssignmentUpdated(EventContext ctx, StdbRa oldRa, StdbRa newRa)
    {
        _assignments[newRa.SlotIndex] = newRa;
        EnsureRoomNodes(newRa.SlotIndex);
        QueueRedraw();
    }

    private void OnPowerGridInserted(EventContext ctx, StdbPg grid)
    {
        _gridStatus = grid.Status;
        QueueRedraw();
    }

    private void OnPowerGridUpdated(EventContext ctx, StdbPg oldGrid, StdbPg newGrid)
    {
        _gridStatus = newGrid.Status;
        QueueRedraw();
    }

    private void SubscribeToAssignments(DbConnection svr)
    {
        foreach (var ra in svr.Db.RoomAssignments.Iter())
        {
            _assignments[ra.SlotIndex] = ra;
            EnsureRoomNodes(ra.SlotIndex);
        }

        if (svr.Db.PowerGrids.Id.Find(0) is { } grid)
            _gridStatus = grid.Status;

        svr.Db.RoomAssignments.OnInsert += OnAssignmentInserted;
        svr.Db.RoomAssignments.OnUpdate += OnAssignmentUpdated;
        svr.Db.PowerGrids.OnInsert += OnPowerGridInserted;
        svr.Db.PowerGrids.OnUpdate += OnPowerGridUpdated;

        QueueRedraw();
    }
}
