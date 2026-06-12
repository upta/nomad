#nullable enable

namespace Nomad.Game.Character;

using System;
using SpacetimeDB;
using SpacetimeDB.Types;

// Plain-C# view of the local player's vitals for UI consumers (VitalsHud).
// Connected mode mirrors the server's VitalsRows row for the local identity;
// test mode (no connection) is seeded directly by pure harnesses.
public class VitalsService
{
    private DbConnection? _conn;

    public event Action? Changed;

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

        conn.Db.VitalsRows.OnInsert += OnVitalsInserted;
        conn.Db.VitalsRows.OnUpdate += OnVitalsUpdated;

        Changed?.Invoke();
    }

    public void SetTestHunger(float hunger, float maxHunger)
    {
        Hunger = hunger;
        MaxHunger = maxHunger;
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

    public void Unbind()
    {
        if (_conn is null)
            return;

        _conn.Db.VitalsRows.OnInsert -= OnVitalsInserted;
        _conn.Db.VitalsRows.OnUpdate -= OnVitalsUpdated;
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

    private void OnVitalsInserted(EventContext ctx, Vitals vitals)
    {
        if (!IsLocal(vitals.Identity))
            return;

        Apply(vitals);
        Changed?.Invoke();
    }

    private void OnVitalsUpdated(EventContext ctx, Vitals oldVitals, Vitals newVitals)
    {
        if (!IsLocal(newVitals.Identity))
            return;

        Apply(newVitals);
        Changed?.Invoke();
    }
}
