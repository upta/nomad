namespace Nomad.Game.Ui;

using Nomad.Game.Ship;

public record RoomModalInfo(
    string Label,
    TerminalType TerminalType,
    bool IsPowered,
    bool IsPressurized,
    int SlotIndex,
    string RoomId = ""
);
