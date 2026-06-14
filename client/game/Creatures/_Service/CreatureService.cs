#nullable enable

namespace Nomad.Game.Creatures;

using System;
using System.Collections.Generic;
using Godot;
using SpacetimeDB.Types;
using StdbCreature = SpacetimeDB.Types.Creature;

// Plain-C# view of the Creatures table for the CreatureSpawner. Connected mode
// mirrors the server rows (the server tick drives position/velocity, the client
// interpolates); test mode (no connection) is seeded directly so the pure
// CreatureHarness drives the same spawner. Mirrors HazardService.
public class CreatureService
{
    private readonly SortedDictionary<int, CreatureEntry> _creatures = [];
    private DbConnection? _conn;
    private int _nextTestId = 1;

    public event Action? Changed;

    public IReadOnlyList<CreatureEntry> Creatures => [.. _creatures.Values];

    public int Count => _creatures.Count;

    public void BindConnection(DbConnection conn)
    {
        _conn = conn;

        foreach (var creature in conn.Db.Creatures.Iter())
            Apply(creature);

        conn.Db.Creatures.OnInsert += OnInserted;
        conn.Db.Creatures.OnUpdate += OnUpdated;
        conn.Db.Creatures.OnDelete += OnDeleted;

        Changed?.Invoke();
    }

    public void Unbind()
    {
        if (_conn is null)
            return;

        _conn.Db.Creatures.OnInsert -= OnInserted;
        _conn.Db.Creatures.OnUpdate -= OnUpdated;
        _conn.Db.Creatures.OnDelete -= OnDeleted;
        _conn = null;
    }

    public int SeedTestCreature(string typeId, Vector2 position)
    {
        var id = _nextTestId++;
        _creatures[id] = new CreatureEntry(id, typeId, position, Vector2.Zero, 30f);
        Changed?.Invoke();
        return id;
    }

    public void SetTestCreaturePosition(int creatureId, Vector2 position)
    {
        if (!_creatures.TryGetValue(creatureId, out var entry))
            return;

        var velocity = position - entry.Position;
        _creatures[creatureId] = entry with { Position = position, Velocity = velocity };
        Changed?.Invoke();
    }

    public void RemoveTestCreature(int creatureId)
    {
        if (_creatures.Remove(creatureId))
            Changed?.Invoke();
    }

    public void ClearTestCreatures()
    {
        if (_creatures.Count == 0)
            return;

        _creatures.Clear();
        Changed?.Invoke();
    }

    private void Apply(StdbCreature creature) =>
        _creatures[creature.CreatureId] = new CreatureEntry(
            creature.CreatureId,
            creature.CreatureTypeId.ToString(),
            new Vector2(creature.Position.X, creature.Position.Y),
            new Vector2(creature.Velocity.X, creature.Velocity.Y),
            creature.Health
        );

    private void OnInserted(EventContext ctx, StdbCreature creature)
    {
        Apply(creature);
        Changed?.Invoke();
    }

    private void OnUpdated(EventContext ctx, StdbCreature oldCreature, StdbCreature newCreature)
    {
        Apply(newCreature);
        Changed?.Invoke();
    }

    private void OnDeleted(EventContext ctx, StdbCreature creature)
    {
        _creatures.Remove(creature.CreatureId);
        Changed?.Invoke();
    }
}

public record CreatureEntry(
    int CreatureId,
    string TypeId,
    Vector2 Position,
    Vector2 Velocity,
    float Health
);
