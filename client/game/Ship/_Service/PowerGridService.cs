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

    // Fuel mirrors ShipStores here (rather than VitalsService) so the power
    // modal has a single service dependency for its readouts.
    public int Fuel { get; private set; }

    public int FuelPerBurn { get; private set; }

    public bool IsBurningDry => Fuel == 0 && FuelPerBurn > 0;

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

        if (conn.Db.ShipStoresRows.Id.Find(0) is { } stores)
            Fuel = stores.Fuel;

        conn.Db.RoomAssignments.OnInsert += OnAssignmentInserted;
        conn.Db.RoomAssignments.OnUpdate += OnAssignmentUpdated;
        conn.Db.PowerGrids.OnInsert += OnGridInserted;
        conn.Db.PowerGrids.OnUpdate += OnGridUpdated;
        conn.Db.ShipStoresRows.OnInsert += OnStoresInserted;
        conn.Db.ShipStoresRows.OnUpdate += OnStoresUpdated;

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
        if (roomId == nameof(RoomTypeId.Corridor))
            return;

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

    public void SetTestFuel(int fuel, int fuelPerBurn)
    {
        Fuel = fuel;
        FuelPerBurn = fuelPerBurn;
        Changed?.Invoke();
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
        _conn.Db.ShipStoresRows.OnInsert -= OnStoresInserted;
        _conn.Db.ShipStoresRows.OnUpdate -= OnStoresUpdated;
        _conn = null;
    }

    private void ApplyAssignment(RoomAssignment ra)
    {
        // Corridors carry pressure state only — they draw no power and stay
        // out of the PowerRouter modal.
        if (ra.RoomTypeId == RoomTypeId.Corridor)
            return;

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
        FuelPerBurn = grid.FuelPerBurn;
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

    private void OnStoresInserted(EventContext ctx, ShipStores stores)
    {
        Fuel = stores.Fuel;
        Changed?.Invoke();
    }

    private void OnStoresUpdated(EventContext ctx, ShipStores oldStores, ShipStores newStores)
    {
        Fuel = newStores.Fuel;
        Changed?.Invoke();
    }
}
