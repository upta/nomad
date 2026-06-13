#nullable enable

namespace Nomad.Game.Character;

using System;
using System.Collections.Generic;
using System.Linq;
using SpacetimeDB;
using SpacetimeDB.Types;

public sealed record CrewEntry(string Key, string Label, bool IsDead, bool IsSelf);

// Plain-C# view of the crew's vitals for UI consumers (VitalsHud,
// CloningModal). Connected mode mirrors the server's VitalsRows (own row for
// the HUD, full roster for the cloning bay) plus ShipStores biomass; test
// mode (no connection) is seeded directly by pure harnesses.
public class VitalsService
{
    private readonly Dictionary<
        string,
        (Identity? Id, string Label, bool IsDead, bool IsSelf)
    > _crew = [];
    private DbConnection? _conn;

    public event Action? Changed;

    public int Biomass { get; private set; }

    public IReadOnlyList<CrewEntry> DeadCrew =>
        [
            .. _crew
                .Where(kv => kv.Value.IsDead)
                .Select(kv => new CrewEntry(
                    kv.Key,
                    kv.Value.Label,
                    kv.Value.IsDead,
                    kv.Value.IsSelf
                )),
        ];

    public float Health { get; private set; } = 100f;

    public float Hunger { get; private set; } = 100f;

    public bool IsDead { get; private set; }

    public float MaxHunger { get; private set; } = 100f;

    public float MaxHealth { get; private set; } = 100f;

    public float MaxOxygen { get; private set; } = 100f;

    public float Oxygen { get; private set; } = 100f;

    public bool SuitEquipped { get; private set; }

    public float SuitSpeedFactor { get; private set; } = 0.8f;

    public void BindConnection(DbConnection conn)
    {
        _conn = conn;

        if (conn.Identity is { } me && conn.Db.VitalsRows.Identity.Find(me) is { } vitals)
            Apply(vitals);

        if (conn.Db.VitalsConfigs.Id.Find(0) is { } config)
            SuitSpeedFactor = config.SuitSpeedFactor;

        foreach (var row in conn.Db.VitalsRows.Iter())
            TrackCrew(row);

        if (conn.Db.ShipStoresRows.Id.Find(0) is { } stores)
            Biomass = stores.Biomass;

        conn.Db.VitalsRows.OnInsert += OnVitalsInserted;
        conn.Db.VitalsRows.OnUpdate += OnVitalsUpdated;
        conn.Db.ShipStoresRows.OnInsert += OnStoresChangedRow;
        conn.Db.ShipStoresRows.OnUpdate += OnStoresUpdated;

        Changed?.Invoke();
    }

    public void SetTestHunger(float hunger, float maxHunger)
    {
        Hunger = hunger;
        MaxHunger = maxHunger;
        Changed?.Invoke();
    }

    // Test-mode mirror of RestoreHungerFor — pure harnesses bump hunger when an
    // eaten meal fires InventoryService.TestUseRequested.
    public void RestoreTestHunger(float amount)
    {
        Hunger = Math.Min(MaxHunger, Hunger + amount);
        Changed?.Invoke();
    }

    public void SetTestOxygen(float oxygen, float maxOxygen, bool suitEquipped)
    {
        Oxygen = oxygen;
        MaxOxygen = maxOxygen;
        SuitEquipped = suitEquipped;
        Changed?.Invoke();
    }

    public void SetTestVitals(float health, float maxHealth, bool isDead)
    {
        Health = health;
        MaxHealth = maxHealth;
        IsDead = isDead;
        Changed?.Invoke();
    }

    // Connected mode routes through the server; test mode mirrors what the
    // RequestRespawn reducer would do so pure harnesses exercise the same UI.
    public void RequestRespawn(string crewKey)
    {
        if (!_crew.TryGetValue(crewKey, out var entry))
            return;

        if (_conn is not null)
        {
            if (entry.Id is { } identity)
                _conn.Reducers.RequestRespawn(identity);
            return;
        }

        if (Biomass <= 0)
            return;

        Biomass -= 1;
        _crew[crewKey] = entry with { IsDead = false };
        if (entry.IsSelf)
        {
            Health = MaxHealth;
            IsDead = false;
        }
        Changed?.Invoke();
    }

    public void SeedTestCrewMember(string label, bool isDead, bool isSelf = false)
    {
        _crew[label] = (null, label, isDead, isSelf);
        Changed?.Invoke();
    }

    public void SetTestBiomass(int biomass)
    {
        Biomass = biomass;
        Changed?.Invoke();
    }

    public void Unbind()
    {
        if (_conn is null)
            return;

        _conn.Db.VitalsRows.OnInsert -= OnVitalsInserted;
        _conn.Db.VitalsRows.OnUpdate -= OnVitalsUpdated;
        _conn.Db.ShipStoresRows.OnInsert -= OnStoresChangedRow;
        _conn.Db.ShipStoresRows.OnUpdate -= OnStoresUpdated;
        _conn = null;
    }

    private void Apply(Vitals vitals)
    {
        Health = vitals.Health.Current;
        MaxHealth = vitals.Health.Max;
        Oxygen = vitals.Oxygen.Current;
        MaxOxygen = vitals.Oxygen.Max;
        Hunger = vitals.Hunger.Current;
        MaxHunger = vitals.Hunger.Max;
        SuitEquipped = vitals.SuitEquipped;
        IsDead = vitals.IsDead;
    }

    private bool IsLocal(Identity identity) => _conn?.Identity is { } me && identity == me;

    private void OnStoresChangedRow(EventContext ctx, ShipStores stores)
    {
        Biomass = stores.Biomass;
        Changed?.Invoke();
    }

    private void OnStoresUpdated(EventContext ctx, ShipStores oldStores, ShipStores newStores) =>
        OnStoresChangedRow(ctx, newStores);

    private void OnVitalsInserted(EventContext ctx, Vitals vitals)
    {
        TrackCrew(vitals);
        if (IsLocal(vitals.Identity))
            Apply(vitals);

        Changed?.Invoke();
    }

    private void OnVitalsUpdated(EventContext ctx, Vitals oldVitals, Vitals newVitals)
    {
        TrackCrew(newVitals);
        if (IsLocal(newVitals.Identity))
            Apply(newVitals);

        Changed?.Invoke();
    }

    private void TrackCrew(Vitals vitals)
    {
        var isSelf = IsLocal(vitals.Identity);
        var hex = vitals.Identity.ToString();
        var label = (isSelf ? "You" : $"Crew {hex[..6]}");
        _crew[hex] = (vitals.Identity, label, vitals.IsDead, isSelf);
    }
}
