namespace Nomad.Bootstrap;

using Game.Db;
using Game.Guide;
using Godot;

[Meta(typeof(IAutoNode))]
public partial class AppRoot : Node, IProvide<DbConnection>, IProvide<GuideService>
{
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

    public override void _Ready()
    {
        _guideService = new GuideService(InputContext);
        AddChild(_guideService);
        _guideService.Initialize();

        _dbManager = new DbManager();
        _dbManager.OnDataReady += OnDataReady;
        AddChild(_dbManager);

        if (!IsTestMode())
        {
            _dbManager.Connect();
        }
        else
        {
            AddChild(TestScene.Instantiate());
        }
    }

    public void Setup() => this.Provide();

    DbConnection IProvide<DbConnection>.Value() => _dbManager.Connection;

    GuideService IProvide<GuideService>.Value() => _guideService;

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
        AddChild(GameScene.Instantiate());
        _dbManager.OnDataReady -= OnDataReady;
    }
}
