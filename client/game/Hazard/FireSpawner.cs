#nullable enable

namespace Nomad.Game.Hazard;

using System;
using System.Collections.Generic;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

// Spawns one Fire per Hazard row, updating intensity in place and freeing fires
// when their row leaves. Mirrors ResourceNodeSpawner; fires are free-floating
// world entities, so this lives beside ShipGrid rather than inside it.
[Meta(typeof(IAutoNode))]
public partial class FireSpawner : Node2D
{
    private readonly Dictionary<int, Fire> _fires = [];
    private bool _subscribed;

    public override void _Notification(int what) => this.Notify(what);

    public event Action<int>? Interacted;

    [Dependency]
    private HazardService Hazards => this.DependOn<HazardService>();

    // Handed by Main/harness in OnReady — node exports don't bind across
    // scene-instance boundaries.
    public HazardTypeRegistry? Registry { get; set; }

    public int FireCount => _fires.Count;

    [Export]
    public PackedScene FireScene { get; set; } = null!;

    public override void _ExitTree()
    {
        if (_subscribed)
            Hazards.Changed -= OnHazardsChanged;
    }

    public void OnResolved()
    {
        Hazards.Changed += OnHazardsChanged;
        _subscribed = true;
        OnHazardsChanged();
    }

    private void OnHazardsChanged()
    {
        var live = Hazards.Hazards;
        var liveIds = live.Select(e => e.HazardId).ToHashSet();

        foreach (var staleId in _fires.Keys.Where(id => !liveIds.Contains(id)).ToList())
        {
            _fires[staleId].QueueFree();
            _fires.Remove(staleId);
        }

        foreach (var entry in live)
        {
            if (_fires.TryGetValue(entry.HazardId, out var existing))
            {
                existing.SetIntensity(entry.Intensity);
                continue;
            }

            if (Registry?.Find(entry.TypeId) is not { } type)
            {
                GD.PushWarning($"[FireSpawner] No HazardType registered for '{entry.TypeId}'.");
                continue;
            }

            var fire = FireScene.Instantiate<Fire>();
            fire.Position = entry.Position;
            AddChild(fire);
            fire.SetHazard(entry.HazardId, type, entry.Intensity);
            fire.Interacted += OnFireInteracted;
            _fires[entry.HazardId] = fire;
        }
    }

    private void OnFireInteracted(int hazardId) => Interacted?.Invoke(hazardId);
}
