namespace Nomad.Game.Guide;

[GlobalClass, Tool]
public partial class InputModeContext : Resource
{
    [Export]
    public GuideMappingContextBinding? ControllerContext { get; set; }

    [Export]
    public GuideMappingContextBinding? KbmContext { get; set; }
}
