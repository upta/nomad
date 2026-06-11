namespace Nomad.Validation.HarnessControllers;

using Godot;
using Nomad.Game.Ship;

public partial class RoomTypeHarnessController : Node
{
    private RoomTypeRegistry _registry = null!;

    public override void _Ready()
    {
        _registry = new RoomTypeRegistry { Name = "RoomTypeRegistry" };
        AddChild(_registry);
    }

    public Godot.Collections.Dictionary get_observed_state()
    {
        var types = _registry.All;
        var typeList = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        foreach (var rt in types)
        {
            typeList.Add(
                new Godot.Collections.Dictionary
                {
                    ["room_id"] = rt.RoomId,
                    ["label"] = rt.Label,
                    ["power_draw"] = rt.PowerDraw,
                    ["terminal_type"] = (int)rt.TerminalType,
                }
            );
        }

        return new Godot.Collections.Dictionary
        {
            ["type_count"] = types.Count,
            ["room_types"] = typeList,
        };
    }
}
