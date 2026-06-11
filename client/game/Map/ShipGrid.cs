namespace Nomad.Game.Map;

using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Db;
using Godot;
using Ship;
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
    private readonly HashSet<Vector2I> _floorCells = [];
    private readonly HashSet<Vector2I> _wallCells = [];
    private Font _font = null!;

    public override void _Notification(int what) => this.Notify(what);

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
    }

    public override void _ExitTree()
    {
        if (Server is { } svr)
        {
            svr.Db.RoomAssignments.OnInsert -= OnAssignmentInserted;
            svr.Db.RoomAssignments.OnUpdate -= OnAssignmentUpdated;
        }
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
                var entry = new Godot.Collections.Dictionary
                {
                    ["slot_index"] = slot.SlotIndex,
                    ["color_r"] = GetRoomColor(slot.SlotIndex).R,
                    ["color_g"] = GetRoomColor(slot.SlotIndex).G,
                    ["color_b"] = GetRoomColor(slot.SlotIndex).B,
                    ["label"] = GetRoomLabel(slot.SlotIndex),
                };
                roomList.Add(entry);
            }
        }

        return new Godot.Collections.Dictionary
        {
            ["rooms"] = roomList,
            ["map"] = new Godot.Collections.Dictionary
            {
                ["floor_count"] = _floorCells.Count,
                ["wall_count"] = _wallCells.Count,
                ["door_count"] = HullTemplate?.Doors.Count ?? 0,
            },
        };
    }

    public void SetTestAssignment(int slotIndex, string roomTypeId)
    {
        _assignments[slotIndex] = new StdbRa
        {
            SlotIndex = slotIndex,
            RoomTypeId = Enum.Parse<StdbRt>(roomTypeId),
            IsPowered = true,
            IsPressurized = true,
            BreakerOn = true,
            Health = 100f,
        };
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

    private Color GetRoomColor(int slotIndex)
    {
        if (_assignments.TryGetValue(slotIndex, out var ra))
        {
            var roomId = ra.RoomTypeId.ToString();
            if (RoomTypeRegistry?.Find(roomId) is { } rt)
                return rt.Color;
        }

        return DefaultRoomColor;
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
        QueueRedraw();
    }

    private void OnAssignmentUpdated(EventContext ctx, StdbRa oldRa, StdbRa newRa)
    {
        _assignments[newRa.SlotIndex] = newRa;
        QueueRedraw();
    }

    private void SubscribeToAssignments(DbConnection svr)
    {
        foreach (var ra in svr.Db.RoomAssignments.Iter())
            _assignments[ra.SlotIndex] = ra;

        svr.Db.RoomAssignments.OnInsert += OnAssignmentInserted;
        svr.Db.RoomAssignments.OnUpdate += OnAssignmentUpdated;

        QueueRedraw();
    }
}
