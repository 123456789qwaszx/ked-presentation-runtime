using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Presentation Motion",
    "#Reset Target Transform",
    Order = -950)]
public sealed class ResetPresentationTargetTransformCommandSpec : PresentationTargetCommandSpecBase
{
    [Header("Reset Position")]
    public bool resetAnchoredPosition = true;
    public Vector2 anchoredPosition = Vector2.zero;

    [Header("Reset Rotation")]
    public bool resetRotation = true;
    public Vector3 localEulerAngles = Vector3.zero;

    [Header("Reset Scale")]
    public bool resetScale = true;
    public Vector3 localScale = Vector3.one;

    [Header("Reset Size")]
    public bool resetSizeDelta = false;
    public Vector2 sizeDelta = Vector2.zero;

    [Header("Options")]
    public bool killTween = true;
}

public sealed class ResetPresentationTargetTransformCommand : CommandBase
{
    private readonly ResetPresentationTargetTransformCommandSpec _spec;

    private RectTransform _rect;
    private bool _resolveAttempted;

    public override bool WaitForCompletion => true;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public ResetPresentationTargetTransformCommand(ResetPresentationTargetTransformCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        Apply();
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        Apply();
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }

    private void Apply()
    {
        if (_rect == null)
            return;

        if (_spec.killTween)
            _rect.DOKill(true); // Finish previous motion so this command starts from a committed state.

        if (_spec.resetAnchoredPosition)
            _rect.anchoredPosition = _spec.anchoredPosition;

        if (_spec.resetRotation)
            _rect.localEulerAngles = _spec.localEulerAngles;

        if (_spec.resetScale)
            _rect.localScale = _spec.localScale;

        if (_spec.resetSizeDelta)
            _rect.sizeDelta = _spec.sizeDelta;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;
        //***
        // RectTransform rect = PresentationTargetResolver.ResolveRect(
        //     scope,
        //     _spec.target,
        //     _spec.strict,
        //     nameof(FocusBlurCurtainCommand));

        _rect =   UIManager.Instance.GetUI<PresentationUIRoot>().Stage00BackgroundSlot;
    }
}