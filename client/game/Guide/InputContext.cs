namespace Nomad.Game.Guide;

[GlobalClass, Tool]
public partial class InputContext : Resource
{
    [Export]
    public GuideMappingContextBinding? GlobalControllerContext { get; set; }

    [Export]
    public GuideMappingContextBinding? GlobalKbmContext { get; set; }

    [Export]
    public GuideActionBinding? SwitchToControllerAction { get; set; }

    [Export]
    public GuideActionBinding? SwitchToKbmAction { get; set; }
}
