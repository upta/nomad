#nullable enable

namespace Nomad.Game.Harvest;

using System;
using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Interaction;
using Nomad.Game.Ui;

// A harvestable node rendered from a ResourceNode row. Depletion is driven by
// YieldRemaining/YieldMax: full color and size when untouched, lerping toward
// the husk color and DepletedScale as it empties. No blocking collider — the
// player walks through nodes; only the InteractTarget area registers.
[Meta(typeof(IAutoNode))]
public partial class ResourceNode : Node2D
{
    private bool _harvestActive;
    private ResourceNodeType _type = default!;

    public override void _Notification(int what) => this.Notify(what);

    public event Action<int>? Interacted;

    // Color a fully depleted node lerps to — a dark, desaturated husk.
    [Export]
    public Color HuskColor { get; set; } = new(0.18f, 0.18f, 0.2f);

    // Visual scale at zero yield; full yield renders at 1.0.
    [Export]
    public float DepletedScale { get; set; } = 0.6f;

    [Node]
    public ILabel Glyph { get; set; } = default!;

    [Node]
    public RadialProgress HarvestRing { get; set; } = default!;

    [Node]
    public IColorRect Sprite { get; set; } = default!;

    [Node]
    public InteractTarget Target { get; set; } = default!;

    [Node]
    public INode2D Visual { get; set; } = default!;

    public int NodeId { get; private set; }

    public int YieldMax { get; private set; }

    public int YieldRemaining { get; private set; }

    public void OnReady()
    {
        // Harvest wiring lands in Task 4.2; the registration already advertises
        // the prompt so the node reads as an interactable-to-be.
        Target.Registration = new CallbackInteractionRegistration(
            () => GlobalPosition,
            () => $"Harvest {_type.Label}",
            _ => Interacted?.Invoke(NodeId)
        );
    }

    public void SetNode(int nodeId, ResourceNodeType type, int yieldRemaining, int yieldMax)
    {
        NodeId = nodeId;
        _type = type;
        Glyph.Text = type.Glyph;
        SetYield(yieldRemaining, yieldMax);
    }

    public void SetYield(int yieldRemaining, int yieldMax)
    {
        YieldRemaining = yieldRemaining;
        YieldMax = yieldMax;
        RefreshVisual();
    }

    // Driven by the spawner from the local player's active channel. The ring
    // snaps to empty when a channel begins (no Chunked flash of the old fill),
    // then eases up with Progress; it hides the instant the channel ends.
    public void SetHarvestProgress(bool active, float progress)
    {
        if (active && !_harvestActive)
            HarvestRing.Reset(0f);

        _harvestActive = active;
        HarvestRing.Visible = active;

        if (active)
            HarvestRing.SetProgress(progress);
    }

    public bool HarvestRingVisible => HarvestRing.Visible;

    private void RefreshVisual()
    {
        var frac = YieldMax > 0 ? Mathf.Clamp((float)YieldRemaining / YieldMax, 0f, 1f) : 0f;
        Sprite.Color = _type.Color.Lerp(HuskColor, 1f - frac);
        Visual.Scale = Vector2.One * Mathf.Lerp(DepletedScale, 1f, frac);
    }
}
