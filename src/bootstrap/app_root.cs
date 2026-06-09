using Godot;
using System.Collections.Generic;

public partial class AppRoot : Node
{
    private static readonly PackedScene ProductionScene =
        GD.Load<PackedScene>("res://game/main.tscn");

    private static readonly PackedScene TestScene =
        GD.Load<PackedScene>("res://addons/agentic_godot_validation/runtime/scenes/test_bootstrap.tscn");

    private static readonly Dictionary<string, Key> ActionKeys = new()
    {
        ["move_up"] = Key.W,
        ["move_down"] = Key.S,
        ["move_left"] = Key.A,
        ["move_right"] = Key.D,
        ["pause"] = Key.Escape,
        ["ui_accept"] = Key.Enter,
    };

    public override void _Ready()
    {
        EnsureInputActions();
        var nextScene = IsTestMode() ? TestScene : ProductionScene;
        AddChild(nextScene.Instantiate());
    }

    private static bool IsTestMode()
    {
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (arg == "--test-mode")
                return true;
        }
        return false;
    }

    private static void EnsureInputActions()
    {
        foreach (var (actionName, key) in ActionKeys)
        {
            if (!InputMap.HasAction(actionName))
                InputMap.AddAction(actionName);

            var inputEvent = new InputEventKey { PhysicalKeycode = key };

            if (!InputMap.ActionHasEvent(actionName, inputEvent))
                InputMap.ActionAddEvent(actionName, inputEvent);
        }
    }
}
