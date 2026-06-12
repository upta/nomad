#nullable enable

namespace Nomad.Game.Interaction;

using System;
using System.Collections.Generic;

public class InteractionService
{
    private readonly Queue<InteractionRegistration> _pending = new();
    private readonly HashSet<InteractionRegistration> _registrations = [];

    public event Action<InteractionRegistration?>? FocusChanged;
    public event Action? RegistrationsChanged;

    public ProbeData? CurrentProbeData { get; private set; }

    public InteractionRegistration? Focused { get; private set; }

    // While the probing player is a ghost, only ghost-accessible
    // registrations can focus or trigger.
    public bool IsGhost { get; set; }

    public IReadOnlyCollection<InteractionRegistration> Registrations => _registrations;

    public void NotifyTriggered(InteractionRegistration registration)
    {
        if (_registrations.Contains(registration))
        {
            _pending.Enqueue(registration);
        }
    }

    public void Process()
    {
        UpdateFocus();

        if (CurrentProbeData is null)
        {
            _pending.Clear();
            return;
        }

        List<InteractionRegistration>? candidates = null;
        while (_pending.Count > 0)
        {
            var registration = _pending.Dequeue();
            if (_registrations.Contains(registration) && IsSelectable(registration))
            {
                candidates ??= [];
                candidates.Add(registration);
            }
        }

        if (candidates is null)
        {
            return;
        }

        if (ResolveClosest(candidates, CurrentProbeData) is { } winner)
        {
            winner.OnInteraction(CurrentProbeData);
        }
    }

    public void Register(InteractionRegistration registration)
    {
        _registrations.Add(registration);
        RegistrationsChanged?.Invoke();
    }

    public void Unregister(InteractionRegistration registration)
    {
        _registrations.Remove(registration);
        RegistrationsChanged?.Invoke();
    }

    public void UpdateProbeData(ProbeData probeData)
    {
        CurrentProbeData = probeData;
    }

    private InteractionRegistration? ResolveClosest(
        IEnumerable<InteractionRegistration> candidates,
        ProbeData probeData
    )
    {
        InteractionRegistration? closest = null;
        var closestDistSq = float.MaxValue;

        foreach (var candidate in candidates)
        {
            if (!IsSelectable(candidate))
            {
                continue;
            }

            var distSq = candidate.Position.DistanceSquaredTo(probeData.Position);
            if (distSq < closestDistSq)
            {
                closestDistSq = distSq;
                closest = candidate;
            }
        }

        return closest;
    }

    private bool IsSelectable(InteractionRegistration registration) =>
        !IsGhost || registration.GhostAccessible;

    private void UpdateFocus()
    {
        var focused = CurrentProbeData is null
            ? null
            : ResolveClosest(_registrations, CurrentProbeData);

        if (ReferenceEquals(focused, Focused))
        {
            return;
        }

        Focused = focused;
        FocusChanged?.Invoke(focused);
    }
}
