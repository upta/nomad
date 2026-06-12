#nullable enable

namespace Nomad.Game.Ui;

using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Guide;
using Nomad.Game.Interaction;

[Meta(typeof(IAutoNode))]
public partial class InteractPrompt : Node2D
{
    private InteractionRegistration? _current;
    private GuideInputFormatter? _formatter;

    public override void _Notification(int what) => this.Notify(what);

    [Dependency]
    private InteractionService Interaction => this.DependOn<InteractionService>();

    // The GUIDE input formatter renders the currently-bound input for an
    // action ("[E]" on keyboard, the button glyph on a pad) and tracks
    // context/device changes.
    [Export]
    public GDScript FormatterScript { get; set; } = null!;

    [Export]
    public GuideActionBinding InteractAction { get; set; } = null!;

    [Node]
    public ILabel Label { get; set; } = default!;

    public override void _ExitTree()
    {
        Interaction.FocusChanged -= OnFocusChanged;
    }

    public override void _Process(double delta)
    {
        if (_current is not null)
        {
            GlobalPosition = _current.Position;
        }
    }

    public void OnResolved()
    {
        _formatter = GuideInputFormatter.ForActiveContexts(FormatterScript);

        Interaction.FocusChanged += OnFocusChanged;
        OnFocusChanged(Interaction.Focused);
    }

    private string InteractKeyText() => _formatter?.ActionAsText(InteractAction) ?? "";

    private void OnFocusChanged(InteractionRegistration? registration)
    {
        _current = registration;

        if (registration is null)
        {
            Visible = false;
            return;
        }

        var keyText = InteractKeyText();
        Label.Text = keyText.Length > 0 ? $"{keyText} {registration.Label}" : registration.Label;
        GlobalPosition = registration.Position;
        Visible = true;
    }
}
