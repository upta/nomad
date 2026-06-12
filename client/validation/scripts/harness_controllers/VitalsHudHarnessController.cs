#nullable enable

namespace Nomad.Validation.HarnessControllers;

using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Character;
using Nomad.Game.Ui;

[Meta(typeof(IAutoNode), typeof(IProvide<VitalsService>))]
public partial class VitalsHudHarnessController : Node2D, IProvide<VitalsService>
{
    private readonly Dictionary<string, bool> _testActionState = [];
    private readonly VitalsService _vitalsService = new();
    private VitalsHud _hud = null!;
    private Dictionary<string, Action> _testActions = [];

    public override void _Notification(int what) => this.Notify(what);

    // Test actions poll in _PhysicsProcess with manual edge detection — the
    // driver's press/release window spans physics frames that can share a
    // single idle frame, so _Process polling can miss it entirely.
    public override void _PhysicsProcess(double delta)
    {
        foreach (var (action, run) in _testActions)
        {
            var pressed = Input.IsActionPressed(action);
            var wasPressed = _testActionState.TryGetValue(action, out var prior) && prior;
            _testActionState[action] = pressed;

            if (pressed && !wasPressed)
            {
                GD.Print($"[Harness] Test action '{action}' fired.");
                run();
            }
        }
    }

    public override void _Ready()
    {
        _hud = GetNode<VitalsHud>("VitalsHud");

        _testActions = new Dictionary<string, Action>
        {
            ["test_seed_health_full"] = () => _vitalsService.SetTestVitals(100, 100, false),
            ["test_seed_health_40"] = () => _vitalsService.SetTestVitals(40, 100, false),
            ["test_seed_health_zero"] = () => _vitalsService.SetTestVitals(0, 100, true),
            ["test_seed_oxygen_30"] = () => _vitalsService.SetTestOxygen(30, 100, false),
            ["test_seed_oxygen_suit"] = () => _vitalsService.SetTestOxygen(150, 200, true),
        };
        foreach (var action in _testActions.Keys)
        {
            if (!InputMap.HasAction(action))
                InputMap.AddAction(action);
        }

        this.Provide();
    }

    VitalsService IProvide<VitalsService>.Value() => _vitalsService;

    public Godot.Collections.Dictionary get_observed_state()
    {
        return new Godot.Collections.Dictionary
        {
            ["hud"] = new Godot.Collections.Dictionary
            {
                ["fill_ratio"] = _hud.HealthFillRatio,
                ["oxygen_fill_ratio"] = _hud.OxygenFillRatio,
                ["shows_dead"] = _hud.ShowsDead,
                ["visible"] = _hud.Visible,
            },
        };
    }
}
