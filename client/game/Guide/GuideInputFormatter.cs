#nullable enable

namespace Nomad.Game.Guide;

using Godot;

// Typed wrapper around GUIDE's GUIDEInputFormatter (GDScript): addon access
// stays in the Guide layer, callers get a typed API like the other bindings.
public class GuideInputFormatter
{
    private readonly GodotObject _formatter;

    private GuideInputFormatter(GodotObject formatter)
    {
        _formatter = formatter;
    }

    // Renders the input currently bound to an action ("[E]" on keyboard, a
    // pad glyph name on controller), tracking active contexts and device.
    // Headless display servers can't resolve physical key labels (GUIDE's
    // formatter raises an engine error), so consumers get "" and fall back
    // to their bare label.
    public string ActionAsText(GuideActionBinding action) =>
        DisplayServer.GetName() == "headless"
            ? ""
            : _formatter.Call("action_as_text", action.Action).AsString();

    public static GuideInputFormatter ForActiveContexts(
        GDScript formatterScript,
        int iconSize = 32
    ) => new(formatterScript.Call("for_active_contexts", iconSize).AsGodotObject());
}
