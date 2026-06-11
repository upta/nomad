namespace Nomad.Validation.HarnessControllers;

using Godot;
using Nomad.Game.Map;
using Nomad.Game.Ship;

public partial class RoomRenderHarnessController : Node2D
{
    private ShipGrid _shipGrid = null!;

    public override void _Ready()
    {
        // Set up the same configuration as Main.cs does
        var registry = new RoomTypeRegistry { Name = "RoomTypeRegistry" };
        AddChild(registry);

        var hull = GD.Load<HullTemplate>("res://game/Ship/CorvetteHull.tres");

        _shipGrid = new ShipGrid
        {
            Name = "ShipGrid",
            HullTemplate = hull,
            RoomTypeRegistry = registry,
        };
        AddChild(_shipGrid);

        // Inject test assignments matching the Init seed defaults
        _shipGrid.SetTestAssignment(0, "Reactor");
        _shipGrid.SetTestAssignment(1, "Bridge");
        _shipGrid.SetTestAssignment(2, "CloningBay");
        _shipGrid.SetTestAssignment(3, "Hydroponics");
        _shipGrid.SetTestAssignment(4, "Workshop");
        _shipGrid.SetTestAssignment(5, "Kitchen");
        _shipGrid.SetTestAssignment(6, "CargoBay");
    }

    public Godot.Collections.Dictionary get_observed_state()
    {
        return _shipGrid.GetObservedRoomState();
    }
}
