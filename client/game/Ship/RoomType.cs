namespace Nomad.Game.Ship;

using Godot;

[GlobalClass]
public partial class RoomType : Resource
{
    [Export]
    public string RoomId { get; set; } = "";

    [Export]
    public string Label { get; set; } = "";

    [Export]
    public int PowerDraw { get; set; }

    [Export]
    public TerminalType TerminalType { get; set; } = TerminalType.Info;

    [Export]
    public Color Color { get; set; } = Colors.Gray;
}
