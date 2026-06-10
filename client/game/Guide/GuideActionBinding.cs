namespace Nomad.Game.Guide;

public enum GuideActionValueType
{
    Axis1D = 1,
    Axis2D = 2,
    Axis3D = 3,
    Bool = 0,
}

public enum GuideActionState
{
    Completed,
    Ongoing,
    Triggered,
}

[GlobalClass, Tool]
public partial class GuideActionBinding : Godot.Resource
{
    private Resource _action = null!;
    private bool _signalsConnected;

    [Export(PropertyHint.ResourceType, "GUIDEAction")]
    public Resource Action
    {
        get => _action;
        set
        {
            if (_action == value)
            {
                return;
            }

            DisconnectSignals();
            _action = value;
            ConnectSignals();
        }
    }

    public StringName ActionName
    {
        get => (StringName)Action.Get("name");
        set => Action.Set("name", value);
    }

    public GuideActionValueType ActionValueType
    {
        get => (GuideActionValueType)(int)Action.Get("action_value_type");
        set => Action.Set("action_value_type", (int)value);
    }

    public bool BlockLowerPriorityActions
    {
        get => (bool)Action.Get("block_lower_priority_actions");
        set => Action.Set("block_lower_priority_actions", value);
    }

    public string DisplayCategory
    {
        get => (string)Action.Get("display_category");
        set => Action.Set("display_category", value);
    }

    public string DisplayName
    {
        get => (string)Action.Get("display_name");
        set => Action.Set("display_name", value);
    }

    public float ElapsedRatio => (float)Action.Get("elapsed_ratio");

    public float ElapsedSeconds => (float)Action.Get("elapsed_seconds");

    public bool EmitAsGodotActions
    {
        get => (bool)Action.Get("emit_as_godot_actions");
        set => Action.Set("emit_as_godot_actions", value);
    }

    public bool IsRemappable
    {
        get => (bool)Action.Get("is_remappable");
        set => Action.Set("is_remappable", value);
    }

    public float TriggeredSeconds => (float)Action.Get("triggered_seconds");

    public float ValueAxis1D => (float)Action.Get("value_axis_1d");

    public Vector2 ValueAxis2D => (Vector2)Action.Get("value_axis_2d");

    public Vector3 ValueAxis3D => (Vector3)Action.Get("value_axis_3d");

    public bool ValueBool => (bool)Action.Get("value_bool");

    public event Action? Cancelled;
    public event Action? Completed;
    public event Action? JustTriggered;
    public event Action? Ongoing;
    public event Action? Started;
    public event Action? Triggered;

    public bool IsCompleted() => (bool)Action.Call("is_completed");

    public bool IsOngoing() => (bool)Action.Call("is_ongoing");

    public bool IsTriggered() => (bool)Action.Call("is_triggered");

    private void ConnectSignals()
    {
        if (_signalsConnected || _action is null)
        {
            return;
        }

        _action.Connect("cancelled", Callable.From(OnCancelled));
        _action.Connect("completed", Callable.From(OnCompleted));
        _action.Connect("just_triggered", Callable.From(OnJustTriggered));
        _action.Connect("ongoing", Callable.From(OnOngoing));
        _action.Connect("started", Callable.From(OnStarted));
        _action.Connect("triggered", Callable.From(OnTriggered));

        _signalsConnected = true;
    }

    private void DisconnectSignals()
    {
        if (!_signalsConnected || _action is null)
        {
            return;
        }

        _action.Disconnect("cancelled", Callable.From(OnCancelled));
        _action.Disconnect("completed", Callable.From(OnCompleted));
        _action.Disconnect("just_triggered", Callable.From(OnJustTriggered));
        _action.Disconnect("ongoing", Callable.From(OnOngoing));
        _action.Disconnect("started", Callable.From(OnStarted));
        _action.Disconnect("triggered", Callable.From(OnTriggered));

        _signalsConnected = false;
    }

    private void OnCancelled() => Cancelled?.Invoke();

    private void OnCompleted() => Completed?.Invoke();

    private void OnJustTriggered() => JustTriggered?.Invoke();

    private void OnOngoing() => Ongoing?.Invoke();

    private void OnStarted() => Started?.Invoke();

    private void OnTriggered() => Triggered?.Invoke();
}
