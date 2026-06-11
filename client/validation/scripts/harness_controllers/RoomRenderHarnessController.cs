namespace Nomad.Validation.HarnessControllers;

using Godot;
using Nomad.Game.Map;
using Nomad.Game.Ship;

public partial class RoomRenderHarnessController : Node2D
{
    private ShipGrid _shipGrid = null!;

    public override void _Ready()
    {
        _shipGrid = GetNode<ShipGrid>("ShipGrid");
        _shipGrid.RoomTypeRegistry = GetNode<RoomTypeRegistry>("RoomTypeRegistry");

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
