#nullable enable

namespace Nomad.Validation.HarnessControllers;

using System;
using System.Collections.Generic;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Interaction;
using Nomad.Game.Items;
using Nomad.Game.Map;
using Nomad.Game.Ship;
using Nomad.Game.Ui;

[Meta(
    typeof(IAutoNode),
    typeof(IProvide<InteractionService>),
    typeof(IProvide<InventoryService>),
    typeof(IProvide<ItemTypeRegistry>)
)]
public partial class InventoryHarnessController
    : Node2D,
        IProvide<InteractionService>,
        IProvide<InventoryService>,
        IProvide<ItemTypeRegistry>
{
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
        ["hotbar_slot_1"] = Key.Key1,
        ["hotbar_slot_2"] = Key.Key2,
        ["hotbar_slot_3"] = Key.Key3,
        ["hotbar_slot_4"] = Key.Key4,
        ["hotbar_drop"] = Key.Q,
    };

    private readonly Dictionary<string, bool> _bridgeState = [];
    private readonly InteractionService _interactionService = new();
    private readonly InventoryService _inventoryService = new();
    private readonly Dictionary<string, bool> _testActionState = [];
    private HotbarHud _hotbarHud = null!;
    private ItemSpawner _itemSpawner = null!;
    private ItemTypeRegistry _itemTypeRegistry = null!;
    private ModalHost _modalHost = null!;
    private int _oreItemId;
    private Nomad.Game.Player.Player _player = null!;
    private InteractPrompt _prompt = null!;
    private ShipGrid _shipGrid = null!;
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

    public override void _Process(double delta)
    {
        BridgeInputActionsToKeys();
    }

    public override void _Ready()
    {
        _player = GetNode<Nomad.Game.Player.Player>("Player");
        _shipGrid = GetNode<ShipGrid>("ShipGrid");
        _prompt = GetNode<InteractPrompt>("InteractPrompt");
        _itemSpawner = GetNode<ItemSpawner>("ItemSpawner");
        _itemTypeRegistry = GetNode<ItemTypeRegistry>("ItemTypeRegistry");
        _modalHost = GetNode<ModalHost>("ModalHost");
        _hotbarHud = GetNode<HotbarHud>("HotbarHud");
        _shipGrid.RoomTypeRegistry = GetNode<RoomTypeRegistry>("RoomTypeRegistry");
        _itemSpawner.Registry = _itemTypeRegistry;
        _hotbarHud.Registry = _itemTypeRegistry;
        _shipGrid.TerminalInteracted += OnTerminalInteracted;

        // Mirror Main's pickup/drop wiring so the test-mode mirrors drive
        // the same consumer chain as the connected game.
        _itemSpawner.Interacted += itemId => _inventoryService.RequestPickUp(itemId);
        _hotbarHud.DropRequested += () =>
            _inventoryService.RequestDrop(_inventoryService.SelectedSlot, _player.GlobalPosition);

        _testActions = new Dictionary<string, Action>
        {
            ["test_seed_ore_at_origin"] = () =>
                _oreItemId = _inventoryService.SeedTestWorldItem("RawOre", new Vector2(0, 0)),
            // Kitchen (slot 5) center — proves items render inside a room.
            ["test_seed_fuelcell_in_kitchen"] = () =>
                _inventoryService.SeedTestWorldItem("FuelCell", new Vector2(16, 144)),
            ["test_remove_ore"] = () => _inventoryService.RemoveTestWorldItem(_oreItemId),
            ["test_clear_items"] = () => _inventoryService.ClearTestItems(),
            ["test_seed_biomass_slot0"] = () => _inventoryService.SetTestSlot(0, "Biomass"),
            ["test_seed_ore_slot2"] = () => _inventoryService.SetTestSlot(2, "RawOre"),
            // Drives the real ModalHost.Open path (exclusive UiModeContext
            // push) without depending on navigation — the walk-up→modal flow
            // is already covered by terminal_interact_opens_modal.
            ["test_open_kitchen_modal"] = () =>
                _modalHost.Open(new RoomModalInfo("Kitchen", TerminalType.Info, true, true, 5)),
            ["test_kill"] = () => _player.SetGhostMode(true),
            ["test_revive"] = () => _player.SetGhostMode(false),
        };
        foreach (var action in _testActions.Keys.Concat(ActionKeyBridge.Keys))
        {
            if (!InputMap.HasAction(action))
                InputMap.AddAction(action);
        }

        this.Provide();

        // Mirror the server Init seed so the hull renders its rooms.
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

    ItemTypeRegistry IProvide<ItemTypeRegistry>.Value() => _itemTypeRegistry;

    public Godot.Collections.Dictionary get_observed_state()
    {
        var itemTypes = new Godot.Collections.Array();
        foreach (var it in _itemTypeRegistry.All)
        {
            itemTypes.Add(
                new Godot.Collections.Dictionary
                {
                    ["item_id"] = it.ItemId,
                    ["label"] = it.Label,
                    ["glyph"] = it.Glyph,
                    ["color_r"] = it.Color.R,
                    ["color_g"] = it.Color.G,
                    ["color_b"] = it.Color.B,
                }
            );
        }

        var worldItems = new Godot.Collections.Array();
        foreach (var child in _itemSpawner.GetChildren())
        {
            if (child is not WorldItem item || item.IsQueuedForDeletion())
                continue;

            worldItems.Add(
                new Godot.Collections.Dictionary
                {
                    ["item_id"] = item.ItemId,
                    ["glyph"] = item.Glyph.Text,
                    ["x"] = item.Position.X,
                    ["y"] = item.Position.Y,
                    ["color_r"] = item.Sprite.Color.R,
                    ["color_g"] = item.Sprite.Color.G,
                    ["color_b"] = item.Sprite.Color.B,
                }
            );
        }

        return new Godot.Collections.Dictionary
        {
            ["type_count"] = _itemTypeRegistry.All.Count,
            ["item_types"] = itemTypes,
            ["world_item_nodes"] = _itemSpawner.WorldItemNodeCount,
            ["world_items"] = worldItems,
            ["player"] = new Godot.Collections.Dictionary
            {
                ["x"] = _player.GlobalPosition.X,
                ["y"] = _player.GlobalPosition.Y,
                ["is_ghost"] = _player.IsGhostMode,
            },
            ["interaction"] = new Godot.Collections.Dictionary
            {
                ["focused_exists"] = _interactionService.Focused is not null,
                ["focused_label"] = _interactionService.Focused?.Label ?? "",
                ["prompt_visible"] = _prompt.Visible,
            },
            ["hotbar"] = _hotbarHud.GetObservedState(),
            ["storage"] = new Godot.Collections.Dictionary
            {
                ["stored_in_cargo"] = _inventoryService.StoredIn(6).Count,
            },
            ["modal"] = BuildModalState(),
        };
    }

    private Godot.Collections.Dictionary BuildModalState()
    {
        var state = new Godot.Collections.Dictionary
        {
            ["open"] = _modalHost.IsOpen,
            ["title"] = _modalHost.CurrentTitle,
        };

        if (_modalHost.CurrentModal is StorageModal storage)
        {
            state["storage_title"] = storage.TitleLabel.Text;
            state["stored_shown"] = storage.ShownStoredCount;
            state["hotbar_occupied"] = storage.HotbarGrid.OccupiedCount;
            state["cargo_occupied"] = storage.CargoGrid.OccupiedCount;
        }

        return state;
    }

    private void OnTerminalInteracted(Terminal terminal) =>
        _modalHost.Open(
            new RoomModalInfo(
                terminal.RoomLabel,
                terminal.TerminalType,
                terminal.IsPowered,
                terminal.IsPressurized,
                terminal.SlotIndex
            )
        );

    // The validation runtime presses InputMap actions, but GUIDE only sees
    // physical key events — forward action state as synthetic key presses.
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
