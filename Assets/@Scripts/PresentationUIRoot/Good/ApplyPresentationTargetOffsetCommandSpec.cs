using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Presentation Motion",
    "#Apply Target Offset (default = ResetToZero)",
    Order = -940)]
public sealed class ApplyPresentationTargetOffsetCommandSpec : PresentationTargetCommandSpecBase
{
    [Header("Offset")]
    [Tooltip("현재 anchoredPosition 기준으로 더해질 오프셋(픽셀 단위).")]
    public Vector2 offset = Vector2.zero;

    [Header("Reset Target Before Apply")]
    [Tooltip("체크하면 target의 위치를 먼저 (0,0)으로 맞춘 뒤 offset을 적용합니다.")]
    public bool applyFromZero = true;

    [Header("Options")]
    [Tooltip("체크하면 기존 위치 트윈을 끝내고 committed state에서 시작합니다.")]
    public bool killTween = true;
}

public sealed class ApplyPresentationTargetOffsetCommand : CommandBase
{
    private readonly ApplyPresentationTargetOffsetCommandSpec _spec;

    private RectTransform _rect;
    private bool _resolveAttempted;

    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public ApplyPresentationTargetOffsetCommand(ApplyPresentationTargetOffsetCommandSpec spec)
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

        if (_spec.applyFromZero)
            _rect.anchoredPosition = Vector2.zero;

        _rect.anchoredPosition += _spec.offset;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        _rect = PresentationTargetResolver.ResolveRect(
            scope,
            _spec.target,
            _spec.strict,
            nameof(ApplyPresentationTargetOffsetCommand));
    }
}