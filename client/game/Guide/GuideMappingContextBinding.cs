namespace Nomad.Game.Guide;

[GlobalClass, Tool]
public partial class GuideMappingContextBinding : Resource
{
    private Resource _mappingContext = null!;
    private bool _signalsConnected;
    private Callable _onDisabledCallable;
    private Callable _onEnabledCallable;

    [Export(PropertyHint.ResourceType, "GUIDEMappingContext")]
    public Resource MappingContext
    {
        get => _mappingContext;
        set
        {
            if (_mappingContext == value)
            {
                return;
            }

            DisconnectSignals();
            _mappingContext = value;
            ConnectSignals();
        }
    }

    public string DisplayName
    {
        get => (string)MappingContext.Get("display_name");
        set => MappingContext.Set("display_name", value);
    }

    public Godot.Collections.Array<Resource> Mappings
    {
        get => (Godot.Collections.Array<Resource>)MappingContext.Get("mappings");
        set => MappingContext.Set("mappings", value);
    }

    public event Action? Disabled;
    public event Action? Enabled;

    private void ConnectSignals()
    {
        if (_signalsConnected || _mappingContext is null)
        {
            return;
        }

        if (_onDisabledCallable.Equals(default(Callable)))
        {
            _onDisabledCallable = Callable.From(OnDisabled);
        }

        if (_onEnabledCallable.Equals(default(Callable)))
        {
            _onEnabledCallable = Callable.From(OnEnabled);
        }

        _mappingContext.Connect("disabled", _onDisabledCallable);
        _mappingContext.Connect("enabled", _onEnabledCallable);

        _signalsConnected = true;
    }

    private void DisconnectSignals()
    {
        if (!_signalsConnected || _mappingContext is null)
        {
            return;
        }

        if (!_onDisabledCallable.Equals(default(Callable)))
        {
            _mappingContext.Disconnect("disabled", _onDisabledCallable);
        }

        if (!_onEnabledCallable.Equals(default(Callable)))
        {
            _mappingContext.Disconnect("enabled", _onEnabledCallable);
        }

        _signalsConnected = false;
    }

    private void OnDisabled() => Disabled?.Invoke();

    private void OnEnabled() => Enabled?.Invoke();
}
