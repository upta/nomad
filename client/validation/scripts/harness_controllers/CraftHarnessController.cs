#nullable enable

namespace Nomad.Validation.HarnessControllers;

using System;
using System.Collections.Generic;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Crafting;
using Nomad.Game.Interaction;
using Nomad.Game.Items;
using Nomad.Game.Map;
using Nomad.Game.Ship;
using Nomad.Game.Ui;

// Pure crafting harness: drives the FabricatorModal off seeded service state,
// with no server. The test mirror lives in the controller (the HarvestHarness
// pattern): TestQueueRequested consumes ingredients and seeds a job, the physics
// tick advances it, and JobCompleted deposits the output into the bench output
// zone.
[Meta(
    typeof(IAutoNode),
    typeof(IProvide<InteractionService>),
    typeof(IProvide<InventoryService>),
    typeof(IProvide<CraftingService>),
    typeof(IProvide<RecipeRegistry>),
    typeof(IProvide<ItemTypeRegistry>)
)]
public partial class CraftHarnessController
    : Node2D,
        IProvide<InteractionService>,
        IProvide<InventoryService>,
        IProvide<CraftingService>,
        IProvide<RecipeRegistry>,
        IProvide<ItemTypeRegistry>
{
    private const int WorkshopSlot = 4;

    // ~25 frames (~0.4s) to complete — a comfortable window to observe the ring
    // climb before the output lands.
    private const float TestCraftProgressPerFrame = 0.04f;

    private static readonly Dictionary<string, Key> ActionKeyBridge = new()
    {
        ["move_up"] = Key.W,
        ["move_down"] = Key.S,
        ["move_left"] = Key.A,
        ["move_right"] = Key.D,
        ["interact"] = Key.E,
        ["ui_cancel_modal"] = Key.Escape,
        ["modal_accept"] = Key.Enter,
        ["modal_down"] = Key.Down,
        ["modal_up"] = Key.Up,
    };

    private readonly Dictionary<string, bool> _bridgeState = [];
    private readonly CraftingService _craftingService = new();
    private readonly InteractionService _interactionService = new();
    private readonly InventoryService _inventoryService = new();
    private readonly Dictionary<string, bool> _testActionState = [];
    private ItemTypeRegistry _itemTypeRegistry = null!;
    private ModalHost _modalHost = null!;
    private Nomad.Game.Player.Player _player = null!;
    private InteractPrompt _prompt = null!;
    private RecipeRegistry _recipeRegistry = null!;
    private ShipGrid _shipGrid = null!;
    private Dictionary<string, Action> _testActions = [];

    public override void _Notification(int what) => this.Notify(what);

    // Polls in _PhysicsProcess with manual edge detection — the bridge belongs
    // here so a slow render frame can't straddle a whole press→release window
    // (the bridged modal-key flake root cause; cf. PowerHarnessController).
    public override void _PhysicsProcess(double delta)
    {
        BridgeInputActionsToKeys();

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

        _craftingService.AdvanceTestActiveJobs(TestCraftProgressPerFrame);
    }

    public override void _Ready()
    {
        _player = GetNode<Nomad.Game.Player.Player>("Player");
        _shipGrid = GetNode<ShipGrid>("ShipGrid");
        _modalHost = GetNode<ModalHost>("ModalHost");
        _prompt = GetNode<InteractPrompt>("InteractPrompt");
        _recipeRegistry = GetNode<RecipeRegistry>("RecipeRegistry");
        _itemTypeRegistry = GetNode<ItemTypeRegistry>("ItemTypeRegistry");
        _shipGrid.RoomTypeRegistry = GetNode<RoomTypeRegistry>("RoomTypeRegistry");
        _shipGrid.TerminalInteracted += OnTerminalInteracted;

        _craftingService.TestQueueRequested += OnTestQueueRequested;
        _craftingService.JobCompleted += OnJobCompleted;

        _testActions = new Dictionary<string, Action>
        {
            ["test_open_fabricator"] = OpenFabricator,
            ["test_clear"] = () =>
            {
                _inventoryService.ClearTestItems();
                _craftingService.ClearTestJobs();
            },
            ["test_give_fueldeposit_slot0"] = () => _inventoryService.SetTestSlot(0, "FuelDeposit"),
            ["test_give_ore_slot0"] = () => _inventoryService.SetTestSlot(0, "RawOre"),
            ["test_give_ore_slot1"] = () => _inventoryService.SetTestSlot(1, "RawOre"),
            // Pre-load a FuelDeposit straight into the bench input zone (slot 0).
            ["test_load_fueldeposit_to_bench"] = () =>
                _inventoryService.SeedTestStoredItemAt("FuelDeposit", WorkshopSlot, 0),
            // Direct queue (the exact call the modal Queue button makes) — keeps
            // the progress/output proof free of key-nav timing.
            ["test_queue_fuelcell"] = () =>
                _craftingService.RequestQueueCraft(WorkshopSlot, "FuelCell"),
        };
        foreach (var action in _testActions.Keys.Concat(ActionKeyBridge.Keys))
        {
            if (!InputMap.HasAction(action))
                InputMap.AddAction(action);
        }

        this.Provide();

        // Mirror the server Init seed so terminals spawn for every room.
        _shipGrid.SetTestAssignment(0, "Reactor");
        _shipGrid.SetTestAssignment(1, "Bridge");
        _shipGrid.SetTestAssignment(2, "CloningBay");
        _shipGrid.SetTestAssignment(3, "Hydroponics");
        _shipGrid.SetTestAssignment(4, "Workshop");
        _shipGrid.SetTestAssignment(5, "Kitchen");
        _shipGrid.SetTestAssignment(6, "CargoBay");
        _shipGrid.SetTestAssignment(7, "Corridor");
    }

    InteractionService IProvide<InteractionService>.Value() => _interactionService;

    InventoryService IProvide<InventoryService>.Value() => _inventoryService;

    CraftingService IProvide<CraftingService>.Value() => _craftingService;

    RecipeRegistry IProvide<RecipeRegistry>.Value() => _recipeRegistry;

    ItemTypeRegistry IProvide<ItemTypeRegistry>.Value() => _itemTypeRegistry;

    public Godot.Collections.Dictionary get_observed_state()
    {
        var hotbar = new Godot.Collections.Dictionary();
        var slots = _inventoryService.Slots;
        for (var i = 0; i < slots.Count; i++)
        {
            if (slots[i] is { } typeId)
                hotbar[i.ToString()] = typeId;
        }

        var modal = new Godot.Collections.Dictionary
        {
            ["open"] = _modalHost.IsOpen,
            ["title"] = _modalHost.CurrentTitle,
        };
        if (_modalHost.CurrentModal is FabricatorModal fabricator)
        {
            foreach (var (key, value) in fabricator.GetObservedState())
                modal[key] = value;
        }

        return new Godot.Collections.Dictionary
        {
            ["modal"] = modal,
            ["hotbar"] = hotbar,
            ["hotbar_count"] = hotbar.Count,
            ["bench_input"] = BenchZone(0, _craftingService.BenchInputSlots),
            ["bench_output"] = BenchZone(
                _craftingService.BenchInputSlots,
                _craftingService.BenchInputSlots + _craftingService.BenchOutputSlots
            ),
            ["player"] = new Godot.Collections.Dictionary
            {
                ["x"] = _player.GlobalPosition.X,
                ["y"] = _player.GlobalPosition.Y,
            },
        };
    }

    // Stored items in [startSlot, endSlot) at the Workshop bench, slot-ordered.
    private Godot.Collections.Array BenchZone(int startSlot, int endSlot)
    {
        var zone = new Godot.Collections.Array();
        foreach (var entry in _inventoryService.StoredIn(WorkshopSlot))
        {
            if (entry.SlotIndex >= startSlot && entry.SlotIndex < endSlot)
            {
                zone.Add(
                    new Godot.Collections.Dictionary
                    {
                        ["type"] = entry.TypeId,
                        ["slot"] = entry.SlotIndex,
                    }
                );
            }
        }
        return zone;
    }

    private void OpenFabricator() =>
        _modalHost.Open(
            new RoomModalInfo(
                "Workshop",
                TerminalType.Fabricator,
                true,
                true,
                WorkshopSlot,
                "Workshop"
            )
        );

    // Pure-mode queue mirror: reserve a distinct row per ingredient (hotbar-first
    // then bench input), validate all before removing any, then seed the job.
    private void OnTestQueueRequested(int roomSlot, string recipeId)
    {
        if (_recipeRegistry.Find(recipeId) is not { } recipe)
            return;

        var consumedHotbar = new List<int>();
        var consumedStored = new List<int>();
        foreach (var ingredient in recipe.IngredientItemIds)
        {
            if (FindHotbarSlot(ingredient, consumedHotbar) is { } slot)
            {
                consumedHotbar.Add(slot);
                continue;
            }

            if (FindBenchInputItem(roomSlot, ingredient, consumedStored) is { } itemId)
            {
                consumedStored.Add(itemId);
                continue;
            }

            return; // Missing an ingredient — the button should have been disabled.
        }

        foreach (var slot in consumedHotbar)
            _inventoryService.SetTestSlot(slot, null);
        foreach (var itemId in consumedStored)
            _inventoryService.RemoveTestStoredItem(itemId);

        var active = _craftingService.ActiveJobAt(roomSlot) is null;
        _craftingService.SeedTestJob(roomSlot, recipeId, active);
    }

    private int? FindHotbarSlot(string itemId, List<int> consumed)
    {
        var slots = _inventoryService.Slots;
        for (var i = 0; i < slots.Count; i++)
        {
            if (slots[i] == itemId && !consumed.Contains(i))
                return i;
        }
        return null;
    }

    private int? FindBenchInputItem(int roomSlot, string itemId, List<int> consumed)
    {
        foreach (var entry in _inventoryService.StoredIn(roomSlot))
        {
            if (
                entry.SlotIndex < _craftingService.BenchInputSlots
                && entry.TypeId == itemId
                && !consumed.Contains(entry.ItemId)
            )
                return entry.ItemId;
        }
        return null;
    }

    private void OnJobCompleted(int roomSlot, string recipeId)
    {
        if (_recipeRegistry.Find(recipeId) is not { } recipe)
            return;

        var occupied = new HashSet<int>();
        foreach (var entry in _inventoryService.StoredIn(roomSlot))
            occupied.Add(entry.SlotIndex);

        var start = _craftingService.BenchInputSlots;
        var end = start + _craftingService.BenchOutputSlots;
        for (var slot = start; slot < end; slot++)
        {
            if (!occupied.Contains(slot))
            {
                _inventoryService.SeedTestStoredItemAt(recipe.OutputItemId, roomSlot, slot);
                return;
            }
        }
    }

    private void OnTerminalInteracted(Terminal terminal) =>
        _modalHost.Open(
            new RoomModalInfo(
                terminal.RoomLabel,
                terminal.TerminalType,
                terminal.IsPowered,
                terminal.IsPressurized,
                terminal.SlotIndex,
                terminal.RoomId
            )
        );

    private void BridgeInputActionsToKeys()
    {
        foreach (var (action, key) in ActionKeyBridge)
        {
            var pressed = Input.IsActionPressed(action);
            if (_bridgeState.TryGetValue(action, out var wasPressed) && wasPressed == pressed)
                continue;

            _bridgeState[action] = pressed;
            Input.ParseInputEvent(
                new InputEventKey
                {
                    Keycode = key,
                    PhysicalKeycode = key,
                    Pressed = pressed,
                }
            );
        }
    }
}
