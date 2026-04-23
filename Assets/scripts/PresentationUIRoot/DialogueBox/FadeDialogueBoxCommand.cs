using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Presentation Dialogue", "Fade Dialogue Box", Order = -695)]
public sealed class FadeDialogueBoxCommandSpec : CommandSpecBase
{
    [Header("Identity")]
    public string dialogueKey = "main";

    [Header("Fade")]
    public float targetAlpha = 1f;
    public float duration = 0.25f;
    public Ease ease = Ease.OutCubic;

    [Header("Interaction")]
    public bool controlInteractable = false;
    public bool controlBlocksRaycasts = false;

    [Header("Wait")]
    public bool wait = false;

    [Header("Options")]
    public bool killTween = true;
    public bool strict = true;
}

public sealed class FadeDialogueBoxCommand : CommandBase
{
    private readonly FadeDialogueBoxCommandSpec _spec;

    private PresentationDialogueBoxView _view;
    private RectTransform _rect;
    private CanvasGroup _group;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public FadeDialogueBoxCommand(FadeDialogueBoxCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_spec.killTween)
            _rect.DOKill(true); // Finish previous motion so this command starts from a committed state.

        _group.DOKill(true);

        _canCommitFinalState = true;

        if (_spec.duration <= 0f)
        {
            ApplyFinalState(_spec.targetAlpha);
            ClearRefs();
            yield break;
        }

        _tween = DOTween
            .To(
                () => _group.alpha,
                x =>
                {
                    if (!_canCommitFinalState || _group == null)
                        return;

                    _group.alpha = x;
                },
                Mathf.Clamp01(_spec.targetAlpha),
                _spec.duration
            )
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_group)
            .OnComplete(() =>
            {
                if (!_canCommitFinalState || _group == null)
                    return;

                ApplyFinalState(_spec.targetAlpha);
                ClearRefs();
            });

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ApplyFinalState(_spec.targetAlpha);
        ClearRefs();
    }

    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!_canCommitFinalState || _rect == null || _group == null)
            return;

        _tween?.Kill(false);
        _rect.DOKill(false);
        _group.DOKill(false);

        ApplyFinalState(_spec.targetAlpha);
        ClearRefs();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (!scope.Refs.TryGetDialogueBoxView(_spec.dialogueKey, out PresentationDialogueBoxView view))
        {
            if (_spec.strict)
                throw new InvalidOperationException($"[FadeDialogueBoxCommand] DialogueBox view not found. dialogueKey={_spec.dialogueKey}");
            return;
        }

        _view = view;
        _view.EnsureBound(_spec.strict);

        _rect = _view.Root;
        _group = _view.CanvasGroup;
    }

    private void ApplyFinalState(float alpha)
    {
        float clamped = Mathf.Clamp01(alpha);
        _group.alpha = clamped;

        if (_spec.controlInteractable)
            _group.interactable = clamped > 0.999f;

        if (_spec.controlBlocksRaycasts)
            _group.blocksRaycasts = clamped > 0.001f;
    }

    private void ClearRefs()
    {
        _canCommitFinalState = false;
        _view = null;
        _rect = null;
        _group = null;
        _tween = null;
    }
}