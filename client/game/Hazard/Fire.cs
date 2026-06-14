#nullable enable

namespace Nomad.Game.Hazard;

using System;
using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Interaction;

// A fire hazard rendered from a Hazard row. The sprite takes the hazard type's
// color, grows with intensity, and flickers (cosmetic only). A walk-up
// InteractTarget offers "Extinguish {Label}"; it is NOT GhostAccessible, so
// ghosts cannot put fires out. No blocking collider — fire sits on the floor
// the crew walks.
[Meta(typeof(IAutoNode))]
public partial class Fire : Node2D
{
    private float _flickerElapsed;
    private Color _baseColor = Colors.OrangeRed;
    private HazardType _type = default!;

    public override void _Notification(int what) => this.Notify(what);

    public event Action<int>? Interacted;

    // Visual scale at intensity 0; full intensity renders at 1.0.
    [Export]
    public float MinScale { get; set; } = 0.55f;

    [Export]
    public float FlickerSpeed { get; set; } = 9f;

    [Export(PropertyHint.Range, "0,1")]
    public float FlickerAmount { get; set; } = 0.22f;

    [Node]
    public ILabel Glyph { get; set; } = default!;

    [Node]
    public IColorRect Sprite { get; set; } = default!;

    [Node]
    public InteractTarget Target { get; set; } = default!;

    [Node]
    public INode2D Visual { get; set; } = default!;

    public int HazardId { get; private set; }

    public float Intensity { get; private set; }

    public void OnReady()
    {
        Target.Registration = new CallbackInteractionRegistration(
            () => GlobalPosition,
            () => $"Extinguish {_type.Label}",
            _ => Interacted?.Invoke(HazardId)
        );
    }

    // Flicker the fire's brightness so it reads as a live flame; deeper as
    // intensity climbs. Cosmetic — never asserted by validation.
    public override void _Process(double delta)
    {
        _flickerElapsed += (float)delta * FlickerSpeed;
        var swing = FlickerAmount * Intensity * (0.5f + 0.5f * Mathf.Sin(_flickerElapsed));
        Sprite.Color = _baseColor.Darkened(swing);
    }

    public void SetHazard(int hazardId, HazardType type, float intensity)
    {
        HazardId = hazardId;
        _type = type;
        _baseColor = type.Color;
        Sprite.Color = type.Color;
        Glyph.Text = type.Glyph;
        SetIntensity(intensity);
    }

    public void SetIntensity(float intensity)
    {
        Intensity = intensity;
        Visual.Scale = Vector2.One * Mathf.Lerp(MinScale, 1f, Mathf.Clamp(intensity, 0f, 1f));
    }
}
