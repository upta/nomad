#nullable enable

namespace Nomad.Game.Ship;

public class PowerRoomEntry
{
    public bool BreakerOn { get; set; } = true;

    public int Draw { get; set; }

    public bool IsPowered { get; set; } = true;

    public string Label { get; set; } = "";

    public string RoomId { get; set; } = "";

    public int Slot { get; set; }
}
