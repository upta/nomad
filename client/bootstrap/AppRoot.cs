namespace Nomad.Bootstrap;

using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game;
using Nomad.Game.Db;
using Nomad.Game.Guide;

[Meta(typeof(IAutoNode), typeof(IProvide<GuideService>))]
public partial class AppRoot : Node, IProvide<GuideService>
{
    private static readonly Dictionary<string, Key> ActionKeys = new()
    {
        ["move_up"] = Key.W,
        ["move_down"] = Key.S,
        ["move_left"] = Key.A,
        ["move_right"] = Key.D,
        ["interact"] = Key.E,
        ["ui_cancel_modal"] = Key.Escape,
        ["hotbar_slot_1"] = Key.Key1,
        ["hotbar_slot_2"] = Key.Key2,
        ["hotbar_slot_3"] = Key.Key3,
        ["hotbar_slot_4"] = Key.Key4,
        ["hotbar_drop"] = Key.Q,
        ["hotbar_use"] = Key.F,
    };

    private static readonly PackedScene TestScene = GD.Load<PackedScene>(
        "res://addons/agentic_godot_validation/runtime/scenes/test_bootstrap.tscn"
    );

    [Export]
    private PackedScene GameScene { get; set; } = null!;

    [Export]
    private InputContext InputContext { get; set; } = null!;

    [Export]
    private InputModeContext GameplayModeContext { get; set; } = null!;

    private DbManager _dbManager = null!;
    private GuideService _guideService = null!;

    public override void _ExitTree()
    {
        _dbManager.OnDataReady -= OnDataReady;
    }

    public override void _Notification(int what) => this.Notify(what);

    GuideService IProvide<GuideService>.Value() => _guideService;

    public override void _Ready()
    {
        EnsureInputActions();

        _guideService = new GuideService(InputContext);
        AddChild(_guideService);
        _guideService.Initialize();
        this.Provide();

        _dbManager = new DbManager();
        _dbManager.OnDataReady += OnDataReady;
        AddChild(_dbManager);

        if (!IsTestMode())
        {
            _dbManager.Connect();
        }
        else
        {
            // Validation harnesses exercise the real input path, so the gameplay
            // GUIDE context must be active even though OnDataReady never fires here.
            _guideService.PushContext(GameplayModeContext);
            AddChild(TestScene.Instantiate());
        }
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

    private static bool IsTestMode()
    {
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (arg == "--test-mode")
                return true;
        }
        return false;
    }

    private void OnDataReady()
    {
        _guideService.PushContext(GameplayModeContext);
        var main = GameScene.Instantiate<Main>();
        AddChild(main);
        main.InstantiatePlayer(_dbManager);
        _dbManager.OnDataReady -= OnDataReady;
    }
}
