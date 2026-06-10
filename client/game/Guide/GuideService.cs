namespace Nomad.Game.Guide;

public enum InputMode
{
    Controller,
    Kbm,
}

public partial class GuideService(InputContext globalContext) : Node
{
    private const int BasePriority = 100;

    private readonly Dictionary<Resource, GuideMappingContextBinding> _activeBindings = [];
    private readonly HashSet<InputModeContext> _contextSet = [];
    private readonly Stack<InputModeContext> _contextStack = new();
    private readonly HashSet<InputModeContext> _exclusiveContexts = [];

    private Node _guide = null!;
    private InputMode _inputMode = InputMode.Kbm;
    private bool _signalsConnected;

    public InputModeContext? ActiveContext => _contextStack.TryPeek(out var ctx) ? ctx : null;

    public InputMode CurrentInputMode
    {
        get => _inputMode;
        private set
        {
            if (value == _inputMode)
            {
                return;
            }

            _inputMode = value;
            InputModeChanged?.Invoke(_inputMode);
            UpdateInput();
        }
    }

    public InputContext GlobalContext { get; } = globalContext;

    public event Action? ContextChanged;
    public event Action? InputMappingsChanged;
    public event Action<InputMode>? InputModeChanged;

    public void DisableMappingContext(GuideMappingContextBinding context)
    {
        _guide.Call("disable_mapping_context", context.MappingContext);
        _activeBindings.Remove(context.MappingContext);
    }

    public void EnableMappingContext(
        GuideMappingContextBinding context,
        bool disableOthers = false,
        int priority = 0
    )
    {
        if (disableOthers)
        {
            _activeBindings.Clear();
        }

        _guide.Call("enable_mapping_context", context.MappingContext, disableOthers, priority);
        _activeBindings[context.MappingContext] = context;
    }

    public IReadOnlyList<GuideMappingContextBinding> GetEnabledMappingContexts() =>
        _activeBindings.Values.ToList();

    public void Initialize()
    {
        _guide = GetNode("/root/GUIDE");
        ConnectSignals();
        ConnectInputModeActions();
    }

    public bool IsMappingContextEnabled(GuideMappingContextBinding context) =>
        (bool)_guide.Call("is_mapping_context_enabled", context.MappingContext);

    public InputModeContext? PopContext()
    {
        if (_contextStack.Count <= 1)
        {
            GD.PushWarning("Cannot pop the last context from the stack");
            return null;
        }

        var popped = _contextStack.Pop();
        _contextSet.Remove(popped);
        _exclusiveContexts.Remove(popped);
        ContextChanged?.Invoke();
        UpdateInput();
        return popped;
    }

    public void PushContext(InputModeContext context, bool exclusive = false)
    {
        if (!_contextSet.Add(context))
        {
            throw new InvalidOperationException(
                $"Context '{context.ResourceName}' is already on the stack."
            );
        }

        _contextStack.Push(context);
        if (exclusive)
        {
            _exclusiveContexts.Add(context);
        }
        ContextChanged?.Invoke();
        UpdateInput();
    }

    public void SetRemappingConfig(Resource config) => _guide.Call("set_remapping_config", config);

    private void ConnectInputModeActions()
    {
        if (GlobalContext.SwitchToControllerAction is not null)
        {
            GlobalContext.SwitchToControllerAction.Triggered += () =>
                CurrentInputMode = InputMode.Controller;
        }

        if (GlobalContext.SwitchToKbmAction is not null)
        {
            GlobalContext.SwitchToKbmAction.Triggered += () => CurrentInputMode = InputMode.Kbm;
        }
    }

    private void ConnectSignals()
    {
        if (_signalsConnected || _guide is null)
        {
            return;
        }

        _guide.Connect("input_mappings_changed", Callable.From(OnInputMappingsChanged));

        _signalsConnected = true;
    }

    private void OnInputMappingsChanged() => InputMappingsChanged?.Invoke();

    private void UpdateInput()
    {
        if (_contextStack.Count == 0)
        {
            return;
        }

        switch (_inputMode)
        {
            case InputMode.Kbm:
                if (GlobalContext.GlobalKbmContext is not null)
                {
                    EnableMappingContext(GlobalContext.GlobalKbmContext, disableOthers: true);
                }
                else
                {
                    GD.PushError("Global KBM context is null");
                }
                break;

            case InputMode.Controller:
                if (GlobalContext.GlobalControllerContext is not null)
                {
                    EnableMappingContext(
                        GlobalContext.GlobalControllerContext,
                        disableOthers: true
                    );
                }
                else
                {
                    GD.PushError("Global controller context is null");
                }
                break;
        }

        var contexts = _contextStack.ToArray();

        for (var i = 0; i < contexts.Length; i++)
        {
            var priority = BasePriority - (contexts.Length - i);
            var context = contexts[i];

            switch (_inputMode)
            {
                case InputMode.Kbm:
                    if (context.KbmContext is not null)
                    {
                        EnableMappingContext(context.KbmContext, priority: priority);
                    }
                    else
                    {
                        GD.PushWarning("KBM context for stacked mode is null");
                    }
                    break;

                case InputMode.Controller:
                    if (context.ControllerContext is not null)
                    {
                        EnableMappingContext(context.ControllerContext, priority: priority);
                    }
                    else
                    {
                        GD.PushWarning("Controller context for stacked mode is null");
                    }
                    break;
            }

            if (_exclusiveContexts.Contains(context))
            {
                break;
            }
        }
    }
}
