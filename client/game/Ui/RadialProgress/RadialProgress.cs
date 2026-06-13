#nullable enable

namespace Nomad.Game.Ui;

using Godot;

// C# port of trail's radial_progress.gd, upgraded with a ProgressMode export.
// Fed by a server Progress float (0→1). The ShaderMaterial is declared in the
// .tscn (resource_local_to_scene) rather than built in _Ready, per scene-first
// rules — _Ready only reads it and pushes uniforms.
[GlobalClass]
public partial class RadialProgress : Control
{
    private float _currentProgress = 1f;
    private float _fillAlpha;
    private float _innerProgress = 1f;
    private ShaderMaterial? _material;
    private float _targetProgress = 1f;
    private Tween? _fillTween;
    private Tween? _innerTween;
    private Tween? _outerTween;

    [Signal]
    public delegate void ProgressChangedEventHandler(float newProgress);

    // Chunked eases the outer ring toward each discrete target (the original
    // behavior, with an inner-delay ghost ring + fill flash) so modest server
    // tick rates still read smooth. Continuous sets the ring directly each
    // frame, bypassing the tweens.
    public enum ProgressMode
    {
        Chunked,
        Continuous,
    }

    [Export]
    public float AnimationSpeed { get; set; } = 1.5f;

    [Export]
    public float AntiAliasWidth { get; set; } = 1.5f;

    [Export]
    public Color BackgroundColor { get; set; } = Colors.Transparent;

    [Export]
    public float FadeDuration { get; set; } = 0.65f;

    [Export]
    public Color FillCenterColor { get; set; } = new(1f, 1f, 1f, 0.4f);

    [Export]
    public Color FillColor { get; set; } = new(1f, 1f, 1f, 0.65f);

    [Export]
    public float InnerDelay { get; set; } = 0.5f;

    [Export]
    public Color InnerColor { get; set; } = new("d4af44");

    [Export]
    public float LineWidth { get; set; } = 8f;

    [Export]
    public ProgressMode Mode { get; set; } = ProgressMode.Chunked;

    [Export]
    public Color OuterColor { get; set; } = Colors.White;

    public float CurrentProgress => _currentProgress;

    public float Progress => _targetProgress;

    public override void _Draw() => DrawRect(new Rect2(Vector2.Zero, Size), Colors.White);

    public override void _Ready()
    {
        _material = Material as ShaderMaterial;
        Resized += UpdateShaderParams;
        UpdateShaderParams();
    }

    // Snap every internal value to one progress without tweening — used when a
    // channel begins so a Chunked ring doesn't flash the old fill first.
    public void Reset(float value)
    {
        _outerTween?.Kill();
        _innerTween?.Kill();
        _fillTween?.Kill();
        _targetProgress = _currentProgress = _innerProgress = Mathf.Clamp(value, 0f, 1f);
        _fillAlpha = 0f;
        UpdateShaderParams();
    }

    public void SetProgress(float value)
    {
        var newTarget = Mathf.Clamp(value, 0f, 1f);

        // Direct set every frame — the tweens are bypassed entirely so the ring
        // tracks the fed value exactly.
        if (Mode == ProgressMode.Continuous)
        {
            _targetProgress = newTarget;
            _currentProgress = newTarget;
            _innerProgress = newTarget;
            UpdateShaderParams();
            EmitSignal(SignalName.ProgressChanged, _currentProgress);
            return;
        }

        if (Mathf.IsEqualApprox(newTarget, _targetProgress))
            return;

        _targetProgress = newTarget;
        AnimateToTarget();
        AnimateFillFade();
    }

    private void AnimateFillFade()
    {
        _fillTween?.Kill();

        _fillTween = CreateTween();
        // Fade in quickly, then fade out more gradually.
        _fillTween
            .TweenMethod(Callable.From<float>(SetFillAlpha), 0f, 1f, FadeDuration * 0.3f)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Quad);
        _fillTween
            .TweenMethod(Callable.From<float>(SetFillAlpha), 1f, 0f, FadeDuration * 0.7f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Quad);
    }

    private void AnimateToTarget()
    {
        _outerTween?.Kill();
        _innerTween?.Kill();

        // Outer ring animates immediately.
        _outerTween = CreateTween();
        var outerDuration = Mathf.Abs(_currentProgress - _targetProgress) / AnimationSpeed;
        _outerTween.TweenMethod(
            Callable.From<float>(SetCurrentProgress),
            _currentProgress,
            _targetProgress,
            outerDuration
        );

        // Inner ghost ring trails behind by InnerDelay.
        _innerTween = CreateTween();
        var innerDuration = Mathf.Abs(_innerProgress - _targetProgress) / AnimationSpeed;
        _innerTween
            .TweenMethod(
                Callable.From<float>(SetInnerProgress),
                _innerProgress,
                _targetProgress,
                innerDuration
            )
            .SetDelay(InnerDelay);
    }

    private void SetCurrentProgress(float value)
    {
        _currentProgress = value;
        UpdateShaderParams();
        EmitSignal(SignalName.ProgressChanged, _currentProgress);
    }

    private void SetFillAlpha(float value)
    {
        _fillAlpha = value;
        UpdateShaderParams();
    }

    private void SetInnerProgress(float value)
    {
        _innerProgress = value;
        UpdateShaderParams();
    }

    private void UpdateShaderParams()
    {
        if (_material is null)
            return;

        _material.SetShaderParameter("outer_progress", _currentProgress);
        _material.SetShaderParameter("inner_progress", _innerProgress);
        _material.SetShaderParameter("outer_color", OuterColor);
        _material.SetShaderParameter("inner_color", InnerColor);
        _material.SetShaderParameter("background_color", BackgroundColor);
        _material.SetShaderParameter("fill_color", FillColor);
        _material.SetShaderParameter("fill_center_color", FillCenterColor);
        _material.SetShaderParameter("fill_alpha", _fillAlpha);
        _material.SetShaderParameter("line_width", LineWidth);
        _material.SetShaderParameter("anti_alias_width", AntiAliasWidth);
        _material.SetShaderParameter("resolution", Size);
    }
}
