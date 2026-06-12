#nullable enable

namespace Nomad.Game.Ship;

using System;
using System.Collections.Generic;
using System.Linq;
using SpacetimeDB.Types;

// Plain-C# view of the ship's power grid for UI consumers (PowerRouterModal).
// Connected mode mirrors the server's RoomAssignments + PowerGrids tables and
// routes toggles through the ToggleBreaker reducer; test mode (no connection)
// flips local state so pure harnesses can drive the same UI.
public class PowerGridService
{
    private readonly Dictionary<string, (string Label, int Draw)> _catalog = [];
    private readonly SortedDictionary<int, PowerRoomEntry> _rooms = [];
    private DbConnection? _conn;

    public event Action? Changed;

    public int ReactorOutput { get; private set; }

    public IReadOnlyList<PowerRoomEntry> Rooms => [.. _rooms.Values];

    public string Status { get; private set; } = "Stable";

    public int TotalDemand => _rooms.Values.Where(r => r.BreakerOn).Sum(r => r.Draw);

    public void BindConnection(DbConnection conn)
    {
        _conn = conn;

        foreach (var ra in conn.Db.RoomAssignments.Iter())
            ApplyAssignment(ra);

        if (conn.Db.PowerGrids.Id.Find(0) is { } grid)
            ApplyGrid(grid);

        conn.Db.RoomAssignments.OnInsert += OnAssignmentInserted;
        conn.Db.RoomAssignments.OnUpdate += OnAssignmentUpdated;
        conn.Db.PowerGrids.OnInsert += OnGridInserted;
        conn.Db.PowerGrids.OnUpdate += OnGridUpdated;

        Changed?.Invoke();
    }

    public void RequestToggleBreaker(int slotIndex)
    {
        if (_conn is not null)
        {
            _conn.Reducers.ToggleBreaker(slotIndex);
            return;
        }

        if (!_rooms.TryGetValue(slotIndex, out var room))
            return;

        room.BreakerOn = !room.BreakerOn;
        room.IsPowered = room.BreakerOn && Status != "Blackout";
        Changed?.Invoke();
    }

    public void SeedTestRoom(int slotIndex, string roomId)
    {
        var (label, draw) = _catalog.TryGetValue(roomId, out var entry) ? entry : (roomId, 0);
        _rooms[slotIndex] = new PowerRoomEntry
        {
            Slot = slotIndex,
            RoomId = roomId,
            Label = label,
            Draw = draw,
        };
        Changed?.Invoke();
    }

    public void SetRoomCatalog(IEnumerable<RoomType> roomTypes)
    {
        foreach (var rt in roomTypes)
            _catalog[rt.RoomId] = (rt.Label, rt.PowerDraw);
    }

    public void SetTestGrid(int reactorOutput, string status)
    {
        ReactorOutput = reactorOutput;
        Status = status;
        Changed?.Invoke();
    }

    public void Unbind()
    {
        if (_conn is null)
            return;

        _conn.Db.RoomAssignments.OnInsert -= OnAssignmentInserted;
        _conn.Db.RoomAssignments.OnUpdate -= OnAssignmentUpdated;
        _conn.Db.PowerGrids.OnInsert -= OnGridInserted;
        _conn.Db.PowerGrids.OnUpdate -= OnGridUpdated;
        _conn = null;
    }

    private void ApplyAssignment(RoomAssignment ra)
    {
        var roomId = ra.RoomTypeId.ToString();
        var (label, draw) = _catalog.TryGetValue(roomId, out var entry) ? entry : (roomId, 0);

        _rooms[ra.SlotIndex] = new PowerRoomEntry
        {
            Slot = ra.SlotIndex,
            RoomId = roomId,
            Label = label,
            Draw = draw,
            BreakerOn = ra.BreakerOn,
            IsPowered = ra.IsPowered,
        };
    }

    private void ApplyGrid(PowerGrid grid)
    {
        ReactorOutput = grid.ReactorOutput;
        Status = grid.Status.ToString();
    }

    private void OnAssignmentInserted(EventContext ctx, RoomAssignment ra)
    {
        ApplyAssignment(ra);
        Changed?.Invoke();
    }

    private void OnAssignmentUpdated(EventContext ctx, RoomAssignment oldRa, RoomAssignment newRa)
    {
        ApplyAssignment(newRa);
        Changed?.Invoke();
    }

    private void OnGridInserted(EventContext ctx, PowerGrid grid)
    {
        ApplyGrid(grid);
        Changed?.Invoke();
    }

    private void OnGridUpdated(EventContext ctx, PowerGrid oldGrid, PowerGrid newGrid)
    {
        ApplyGrid(newGrid);
        Changed?.Invoke();
    }
}
