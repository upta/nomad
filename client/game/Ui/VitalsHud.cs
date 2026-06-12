#nullable enable

namespace Nomad.Game.Ui;

using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Character;

[Meta(typeof(IAutoNode))]
public partial class VitalsHud : CanvasLayer
{
    public override void _Notification(int what) => this.Notify(what);

    [Dependency]
    private VitalsService Vitals => this.DependOn<VitalsService>();

    [Export]
    public Color DeadFillColor { get; set; } = new(0.45f, 0.1f, 0.1f);

    [Export]
    public Color HealthFillColor { get; set; } = new(0.8f, 0.25f, 0.25f);

    [Node]
    public IColorRect HealthFill { get; set; } = default!;

    [Node]
    public ILabel HealthLabel { get; set; } = default!;

    [Node]
    public IColorRect HealthTrack { get; set; } = default!;

    [Export]
    public Color HungerFillColor { get; set; } = new(0.85f, 0.7f, 0.3f);

    [Node]
    public IColorRect HungerFill { get; set; } = default!;

    [Node]
    public ILabel HungerLabel { get; set; } = default!;

    [Node]
    public IColorRect HungerTrack { get; set; } = default!;

    [Export]
    public Color OxygenFillColor { get; set; } = new(0.3f, 0.65f, 0.9f);

    [Node]
    public IColorRect OxygenFill { get; set; } = default!;

    [Node]
    public ILabel OxygenLabel { get; set; } = default!;

    [Node]
    public IColorRect OxygenTrack { get; set; } = default!;

    public float HealthFillRatio { get; private set; } = 1f;

    public float HungerFillRatio { get; private set; } = 1f;

    public float OxygenFillRatio { get; private set; } = 1f;

    public bool ShowsDead { get; private set; }

    public override void _ExitTree()
    {
        Vitals.Changed -= OnVitalsChanged;
    }

    public void OnResolved()
    {
        Vitals.Changed += OnVitalsChanged;
        OnVitalsChanged();
    }

    private void OnVitalsChanged()
    {
        HealthFillRatio =
            Vitals.MaxHealth > 0f ? Mathf.Clamp(Vitals.Health / Vitals.MaxHealth, 0f, 1f) : 0f;
        ShowsDead = Vitals.IsDead;

        HealthFill.Size = new Vector2(HealthTrack.Size.X * HealthFillRatio, HealthFill.Size.Y);
        HealthFill.Color = ShowsDead ? DeadFillColor : HealthFillColor;
        HealthLabel.Text = ShowsDead
            ? "DECEASED"
            : $"HP {Mathf.RoundToInt(Vitals.Health)}/{Mathf.RoundToInt(Vitals.MaxHealth)}";

        OxygenFillRatio =
            Vitals.MaxOxygen > 0f ? Mathf.Clamp(Vitals.Oxygen / Vitals.MaxOxygen, 0f, 1f) : 0f;
        OxygenFill.Size = new Vector2(OxygenTrack.Size.X * OxygenFillRatio, OxygenFill.Size.Y);
        OxygenFill.Color = OxygenFillColor;
        OxygenLabel.Text =
            $"O2 {Mathf.RoundToInt(Vitals.Oxygen)}/{Mathf.RoundToInt(Vitals.MaxOxygen)}"
            + (Vitals.SuitEquipped ? " [SUIT]" : "");

        HungerFillRatio =
            Vitals.MaxHunger > 0f ? Mathf.Clamp(Vitals.Hunger / Vitals.MaxHunger, 0f, 1f) : 0f;
        HungerFill.Size = new Vector2(HungerTrack.Size.X * HungerFillRatio, HungerFill.Size.Y);
        HungerFill.Color = HungerFillColor;
        HungerLabel.Text =
            $"FOOD {Mathf.RoundToInt(Vitals.Hunger)}/{Mathf.RoundToInt(Vitals.MaxHunger)}";
    }
}
