namespace Nomad.Game.Map;

using System;
using System.Collections.Generic;
using Db;
using Godot;
using Ship;
using StdbRa = SpacetimeDB.Types.RoomAssignment;
using StdbRt = SpacetimeDB.Types.RoomTypeId;

public partial class ShipGrid : Node2D
{
    private const int TileSize = 64;

    private static readonly Color FloorColor = new(0.25f, 0.28f, 0.33f);
    private static readonly Color WallColor = new(0.45f, 0.48f, 0.50f);
    private static readonly Color DefaultRoomColor = new(0.30f, 0.33f, 0.38f);

    private readonly Dictionary<int, StdbRa> _assignments = [];
    private Font _font = null!;

    [Export]
    public HullTemplate? HullTemplate { get; set; }

    [Export]
    public RoomTypeRegistry? RoomTypeRegistry { get; set; }

    [Export]
    public Node? DbManagerNode { get; set; }

    private DbConnection? Server => (DbManagerNode as DbManager)?.Connection;

    public override void _Ready()
    {
        _font = ThemeDB.FallbackFont;

        if (Server is { } svr)
            SubscribeToAssignments(svr);

        QueueRedraw();
    }

    public void BindToServer(DbManager dbManager)
    {
        DbManagerNode = dbManager;
        if (Server is { } svr)
            SubscribeToAssignments(svr);
    }

    private void SubscribeToAssignments(DbConnection svr)
    {
        // Load existing assignments before subscribing to events
        foreach (var ra in svr.Db.RoomAssignments.Iter())
            _assignments[ra.SlotIndex] = ra;

        svr.Db.RoomAssignments.OnInsert += OnAssignmentInserted;
        svr.Db.RoomAssignments.OnUpdate += OnAssignmentUpdated;

        QueueRedraw();
    }

    public override void _ExitTree()
    {
        if (Server is { } svr)
        {
            svr.Db.RoomAssignments.OnInsert -= OnAssignmentInserted;
            svr.Db.RoomAssignments.OnUpdate -= OnAssignmentUpdated;
        }
    }

    public override void _Draw()
    {
        if (HullTemplate is null)
            return;

        var halfW = HullTemplate.GridWidth * TileSize / 2f;
        var halfH = HullTemplate.GridHeight * TileSize / 2f;

        // Hull background
        DrawRect(
            new Rect2(
                -halfW,
                -halfH,
                HullTemplate.GridWidth * TileSize,
                HullTemplate.GridHeight * TileSize
            ),
            FloorColor,
            true
        );

        // Draw each room slot
        foreach (var slot in HullTemplate.RoomSlots)
        {
            var color = GetRoomColor(slot.SlotIndex);
            var rect = new Rect2(
                -halfW + slot.PositionX * TileSize,
                -halfH + slot.PositionY * TileSize,
                slot.Width * TileSize,
                slot.Height * TileSize
            );

            // Room interior
            DrawRect(rect, color, true);

            // Room walls
            DrawRect(rect, WallColor, false, 2);

            // Room label
            var label = GetRoomLabel(slot.SlotIndex);
            if (label.Length > 0)
            {
                var labelSize = _font.GetStringSize(label, HorizontalAlignment.Center);
                var labelPos = rect.Position + (rect.Size - labelSize) / 2f;
                DrawString(_font, labelPos, label, HorizontalAlignment.Left);
            }
        }

        // Hull outline
        DrawRect(
            new Rect2(
                -halfW,
                -halfH,
                HullTemplate.GridWidth * TileSize,
                HullTemplate.GridHeight * TileSize
            ),
            WallColor,
            false,
            3
        );
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

        return new Godot.Collections.Dictionary { ["rooms"] = roomList };
    }

    /// <summary>
    /// Injects room assignments directly for validation testing (bypasses SpacetimeDB).
    /// </summary>
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
}
