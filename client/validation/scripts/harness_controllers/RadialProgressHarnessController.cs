#nullable enable

namespace Nomad.Validation.HarnessControllers;

using System;
using System.Collections.Generic;
using Godot;
using Nomad.Game.Ui;

// Drives two RadialProgress rings — one Chunked, one Continuous — so a scenario
// can prove Continuous tracks the set value exactly while Chunked eases toward
// it over several frames.
public partial class RadialProgressHarnessController : Node2D
{
    private readonly Dictionary<string, bool> _testActionState = [];
    private RadialProgress _chunked = null!;
    private RadialProgress _continuous = null!;
    private Dictionary<string, Action> _testActions = [];

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
        _chunked = GetNode<RadialProgress>("Chunked");
        _continuous = GetNode<RadialProgress>("Continuous");

        _testActions = new Dictionary<string, Action>
        {
            ["test_set_both_04"] = () =>
            {
                _chunked.SetProgress(0.4f);
                _continuous.SetProgress(0.4f);
            },
        };

        foreach (var action in _testActions.Keys)
        {
            if (!InputMap.HasAction(action))
                InputMap.AddAction(action);
        }
    }

    public Godot.Collections.Dictionary get_observed_state() =>
        new()
        {
            ["chunked"] = new Godot.Collections.Dictionary
            {
                ["current"] = _chunked.CurrentProgress,
                ["target"] = _chunked.Progress,
            },
            ["continuous"] = new Godot.Collections.Dictionary
            {
                ["current"] = _continuous.CurrentProgress,
                ["target"] = _continuous.Progress,
            },
        };
}
