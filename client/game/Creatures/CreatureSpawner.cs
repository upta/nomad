#nullable enable

namespace Nomad.Game.Creatures;

using System.Collections.Generic;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

// Spawns one Creature per Creature row, retargeting it in place as the server
// moves it and freeing it when its row leaves. Mirrors FireSpawner; creatures
// are free-floating world entities, so this lives beside ShipGrid (on Main)
// rather than inside a map.
[Meta(typeof(IAutoNode))]
public partial class CreatureSpawner : Node2D
{
    private readonly Dictionary<int, Creature> _creatures = [];
    private bool _subscribed;

    public override void _Notification(int what) => this.Notify(what);

    [Dependency]
    private CreatureService Creatures => this.DependOn<CreatureService>();

    // Handed by Main/harness in OnReady — node exports don't bind across
    // scene-instance boundaries.
    public CreatureTypeRegistry? Registry { get; set; }

    public int CreatureCount => _creatures.Count;

    [Export]
    public PackedScene CreatureScene { get; set; } = null!;

    public override void _ExitTree()
    {
        if (_subscribed)
            Creatures.Changed -= OnCreaturesChanged;
    }

    public void OnResolved()
    {
        Creatures.Changed += OnCreaturesChanged;
        _subscribed = true;
        OnCreaturesChanged();
    }

    private void OnCreaturesChanged()
    {
        var live = Creatures.Creatures;
        var liveIds = live.Select(e => e.CreatureId).ToHashSet();

        foreach (var staleId in _creatures.Keys.Where(id => !liveIds.Contains(id)).ToList())
        {
            _creatures[staleId].QueueFree();
            _creatures.Remove(staleId);
        }

        foreach (var entry in live)
        {
            if (_creatures.TryGetValue(entry.CreatureId, out var existing))
            {
                existing.SetTarget(entry.Position);
                continue;
            }

            if (Registry?.Find(entry.TypeId) is not { } type)
            {
                GD.PushWarning(
                    $"[CreatureSpawner] No CreatureType registered for '{entry.TypeId}'."
                );
                continue;
            }

            var creature = CreatureScene.Instantiate<Creature>();
            AddChild(creature);
            creature.SetCreature(entry.CreatureId, type, entry.Position);
            _creatures[entry.CreatureId] = creature;
        }
    }
}
