#nullable enable

namespace Nomad.Validation.HarnessControllers;

using System;
using System.Collections.Generic;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Creatures;
using Nomad.Game.Hazard;
using Nomad.Game.Interaction;
using Nomad.Game.Map;
using Nomad.Game.Ship;

// Pure (no-server) harness for the WreckMap: the derelict-salvage map renders
// the ship interior, its airlock, and a dim/cold exterior terrain, and the
// reused fire (5.1) and creature (5.2) stacks render on top of it. Instances
// the real WreckMap scene the MapHost loads at the Wreck node, wires its
// ShipGrid registry + seeds room assignments (the server does this in the
// connected game), and seeds a fire + a creature directly through the services
// so the same spawners run as in Main.
[Meta(
    typeof(IAutoNode),
    typeof(IProvide<InteractionService>),
    typeof(IProvide<HazardService>),
    typeof(IProvide<CreatureService>)
)]
public partial class WreckHarnessController
    : Node2D,
        IProvide<InteractionService>,
        IProvide<HazardService>,
        IProvide<CreatureService>
{
    private readonly CreatureService _creatureService = new();
    private readonly HazardService _hazardService = new();
    private readonly InteractionService _interactionService = new();
    private readonly Dictionary<string, bool> _testActionState = [];
    private CreatureSpawner _creatureSpawner = null!;
    private CreatureTypeRegistry _creatureTypeRegistry = null!;
    private FireSpawner _fireSpawner = null!;
    private HazardTypeRegistry _hazardTypeRegistry = null!;
    private GameMap _wreckMap = null!;
    private TerrainGrid _terrain = null!;
    private int _creatureId;
    private int _fireId;
    private Dictionary<string, Action> _testActions = [];

    public override void _Notification(int what) => this.Notify(what);

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
        _wreckMap = GetNode<GameMap>("WreckMap");
        _terrain = GetNode<TerrainGrid>("WreckMap/TerrainGrid");
        _fireSpawner = GetNode<FireSpawner>("FireSpawner");
        _creatureSpawner = GetNode<CreatureSpawner>("CreatureSpawner");
        _hazardTypeRegistry = GetNode<HazardTypeRegistry>("HazardTypeRegistry");
        _creatureTypeRegistry = GetNode<CreatureTypeRegistry>("CreatureTypeRegistry");
        _fireSpawner.Registry = _hazardTypeRegistry;
        _creatureSpawner.Registry = _creatureTypeRegistry;

        // Node exports don't bind across the scene-instance boundary, so hand
        // the ship's grid its room registry here (Main does the same).
        _wreckMap.Ship.ShipGrid.RoomTypeRegistry = GetNode<RoomTypeRegistry>("RoomTypeRegistry");

        _testActions = new Dictionary<string, Action>
        {
            // Fire on the ship interior (corridor center) — a ship hazard that
            // burns inside the docked ship, reused unchanged at the wreck.
            ["test_seed_fire"] = () =>
                _fireId = _hazardService.SeedTestHazard("Fire", new Vector2(0, 0), 0.5f),
            // A crawler out on the derelict terrain — the wreck's roaming hazard.
            ["test_seed_creature"] = () =>
                _creatureId = _creatureService.SeedTestCreature("Crawler", new Vector2(700, 0)),
        };
        foreach (var action in _testActions.Keys)
        {
            if (!InputMap.HasAction(action))
                InputMap.AddAction(action);
        }

        this.Provide();

        // Mirror the server room seed so the docked ship renders its hull.
        var grid = _wreckMap.Ship.ShipGrid;
        grid.SetTestAssignment(0, "Reactor");
        grid.SetTestAssignment(1, "Bridge");
        grid.SetTestAssignment(2, "CloningBay");
        grid.SetTestAssignment(3, "Hydroponics");
        grid.SetTestAssignment(4, "Workshop");
        grid.SetTestAssignment(5, "Kitchen");
        grid.SetTestAssignment(6, "CargoBay");
        grid.SetTestAssignment(7, "Corridor");
    }

    InteractionService IProvide<InteractionService>.Value() => _interactionService;

    HazardService IProvide<HazardService>.Value() => _hazardService;

    CreatureService IProvide<CreatureService>.Value() => _creatureService;

    public Godot.Collections.Dictionary get_observed_state()
    {
        var ship = _wreckMap.Ship;
        var ground = _terrain.GroundColor;

        return new Godot.Collections.Dictionary
        {
            ["ship_present"] = ship is not null,
            ["airlock_present"] = ship?.Airlock is not null,
            ["airlock_label"] = ship?.Airlock?.Label ?? "",
            ["terminal_count"] = ship?.ShipGrid.TerminalCount ?? 0,
            ["terrain"] = new Godot.Collections.Dictionary
            {
                ["ground_r"] = ground.R,
                ["ground_g"] = ground.G,
                ["ground_b"] = ground.B,
            },
            ["hazard_type_count"] = _hazardTypeRegistry.All.Count,
            ["creature_type_count"] = _creatureTypeRegistry.All.Count,
            ["fire_count"] = _fireSpawner.FireCount,
            ["creature_count"] = _creatureSpawner.CreatureCount,
        };
    }
}
