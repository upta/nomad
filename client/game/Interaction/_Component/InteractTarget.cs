#nullable enable

namespace Nomad.Game.Interaction;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Nomad.Game.Guide;

[Meta(typeof(IAutoNode))]
[GlobalClass]
public partial class InteractTarget : Area2D
{
    private bool _isRegistered;

    public InteractTarget()
    {
        CollisionLayer = (uint)CollisionLayers.Interactable;
        CollisionMask = (uint)CollisionLayers.Interactable;
    }

    public enum TriggerMode
    {
        Action,
        Enter,
    }

    [Dependency]
    private InteractionService Service => this.DependOn<InteractionService>();

    public bool InteractionEnabled
    {
        set
        {
            SetDeferred(Area2D.PropertyName.Monitorable, value);
            SetDeferred(Area2D.PropertyName.Monitoring, value);
        }
    }

    [Export]
    public TriggerMode Mode { get; set; }

    public InteractionRegistration? Registration { get; set; }

    [Export]
    public GuideActionBinding? Trigger { get; set; }

    public override void _ExitTree()
    {
        Cleanup();
    }

    public override void _Notification(int what) => this.Notify(what);

    public override void _Ready()
    {
        if (Mode == TriggerMode.Action && Trigger?.Action == null)
        {
            GD.PushError("Trigger action is required for Action-mode interactables.");
        }

        AreaEntered += OnAreaEntered;
        AreaExited += OnAreaExited;
    }

    private void Cleanup()
    {
        if (!_isRegistered || Registration is null)
        {
            return;
        }

        if (Mode == TriggerMode.Action && Trigger is not null)
        {
            Trigger.JustTriggered -= OnTriggerFired;
        }

        Service.Unregister(Registration);
        _isRegistered = false;
    }

    private void OnAreaEntered(Area2D area)
    {
        if (area is not InteractProbe || Registration is null)
        {
            return;
        }

        Service.Register(Registration);
        _isRegistered = true;

        if (Mode == TriggerMode.Action && Trigger is not null)
        {
            Trigger.JustTriggered += OnTriggerFired;
        }
        else if (Mode == TriggerMode.Enter)
        {
            Service.NotifyTriggered(Registration);
        }
    }

    private void OnAreaExited(Area2D area)
    {
        if (area is not InteractProbe)
        {
            return;
        }

        Cleanup();
    }

    private void OnTriggerFired()
    {
        if (Registration is not null)
        {
            Service.NotifyTriggered(Registration);
        }
    }
}
