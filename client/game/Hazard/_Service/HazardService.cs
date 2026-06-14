#nullable enable

namespace Nomad.Game.Hazard;

using System;
using System.Collections.Generic;
using Godot;
using SpacetimeDB.Types;
using StdbHazard = SpacetimeDB.Types.Hazard;

// Plain-C# view of the Hazards table for the FireSpawner. Connected mode mirrors
// the server rows (intensity drives the flame's size/flicker); test mode (no
// connection) is seeded directly so pure harnesses drive the same spawner.
public class HazardService
{
    private readonly SortedDictionary<int, HazardEntry> _hazards = [];
    private DbConnection? _conn;
    private int _nextTestId = 1;

    public event Action? Changed;

    public IReadOnlyList<HazardEntry> Hazards => [.. _hazards.Values];

    public int Count => _hazards.Count;

    public void BindConnection(DbConnection conn)
    {
        _conn = conn;

        foreach (var hazard in conn.Db.Hazards.Iter())
            Apply(hazard);

        conn.Db.Hazards.OnInsert += OnInserted;
        conn.Db.Hazards.OnUpdate += OnUpdated;
        conn.Db.Hazards.OnDelete += OnDeleted;

        Changed?.Invoke();
    }

    public void Unbind()
    {
        if (_conn is null)
            return;

        _conn.Db.Hazards.OnInsert -= OnInserted;
        _conn.Db.Hazards.OnUpdate -= OnUpdated;
        _conn.Db.Hazards.OnDelete -= OnDeleted;
        _conn = null;
    }

    public int SeedTestHazard(string typeId, Vector2 position, float intensity)
    {
        var id = _nextTestId++;
        _hazards[id] = new HazardEntry(id, typeId, position, intensity);
        Changed?.Invoke();
        return id;
    }

    public void SetTestIntensity(int hazardId, float intensity)
    {
        if (!_hazards.TryGetValue(hazardId, out var entry))
            return;

        _hazards[hazardId] = entry with { Intensity = intensity };
        Changed?.Invoke();
    }

    public void RemoveTestHazard(int hazardId)
    {
        if (_hazards.Remove(hazardId))
            Changed?.Invoke();
    }

    public void ClearTestHazards()
    {
        if (_hazards.Count == 0)
            return;

        _hazards.Clear();
        Changed?.Invoke();
    }

    private void Apply(StdbHazard hazard) =>
        _hazards[hazard.HazardId] = new HazardEntry(
            hazard.HazardId,
            hazard.HazardTypeId.ToString(),
            new Vector2(hazard.Position.X, hazard.Position.Y),
            hazard.Intensity
        );

    private void OnInserted(EventContext ctx, StdbHazard hazard)
    {
        Apply(hazard);
        Changed?.Invoke();
    }

    private void OnUpdated(EventContext ctx, StdbHazard oldHazard, StdbHazard newHazard)
    {
        Apply(newHazard);
        Changed?.Invoke();
    }

    private void OnDeleted(EventContext ctx, StdbHazard hazard)
    {
        _hazards.Remove(hazard.HazardId);
        Changed?.Invoke();
    }
}

public record HazardEntry(int HazardId, string TypeId, Vector2 Position, float Intensity);
