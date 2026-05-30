using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Dialogue Box",
    "Hide Dialogue Box",
    Order = -780)]
public sealed class HideDialogueBoxCommandSpec : CommandSpecBase
{
    [Header("Target")]
    public DialogueBoxKind targetKind = DialogueBoxKind.Speaker;
    public bool hideAll = true;
    public float duration = 0.18f;
    public bool snapOnSkip = true;
}

public sealed class HideDialogueBoxCommand : CommandBase
{
    private readonly HideDialogueBoxCommandSpec _spec;
    private readonly DialogueBoxHost _resolver;

    private IDialogueTextTarget _target;
    private CanvasGroup _cg;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public HideDialogueBoxCommand(
        HideDialogueBoxCommandSpec spec,
        DialogueBoxHost resolver)
    {
        _spec = spec;
        _resolver = resolver;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        if (_resolver == null)
            yield break;

        if (_spec.hideAll)
        {
            _resolver.HideAll();
            ClearRuntimeRefs();
            yield break;
        }

        if (_target == null)
        {
            ClearRuntimeRefs();
            yield break;
        }

        _canCommitFinalState = true;

        if (_cg == null || _spec.duration <= 0f)
        {
            HideImmediate(_target);
            ClearRuntimeRefs();
            yield break;
        }

        _cg.DOKill(true);

        _cg.interactable = false;
        _cg.blocksRaycasts = false;

        _tween = _cg
            .DOFade(0f, _spec.duration)
            .SetUpdate(true)
            .SetTarget(_cg)
            .OnComplete(() =>
            {
                if (!_canCommitFinalState || _target == null)
                    return;

                HideImmediate(_target);
                ClearRuntimeRefs();
            });

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_spec.snapOnSkip)
            return;

        if (!_resolveAttempted)
            ResolveRefs();

        Apply();
        ClearRuntimeRefs();
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        Apply();
        ClearRuntimeRefs();
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        if (!_canCommitFinalState)
            return;

        _tween?.Kill(false);

        if (_cg != null)
            _cg.DOKill(false);

        Apply();
        ClearRuntimeRefs();
    }

    private void ResolveRefs()
    {
        _resolveAttempted = true;

        if (_resolver == null)
            return;

        if (_spec.hideAll)
            return;

        _target = _resolver.ResolveTarget(_spec.targetKind);
        _cg = _target != null ? _target.CanvasGroup : null;
    }

    private void Apply()
    {
        if (_resolver == null)
            return;

        if (_spec.hideAll)
        {
            _resolver.HideAll();
            return;
        }

        HideImmediate(_target);
    }

    private void ClearRuntimeRefs()
    {
        _canCommitFinalState = false;
        _target = null;
        _cg = null;
        _tween = null;
    }

    private static void HideImmediate(IDialogueTextTarget target)
    {
        if (target == null)
            return;

        IPresentationDialogueBoxView view = target as IPresentationDialogueBoxView;
        if (view != null)
        {
            view.SetVisible(false);
            return;
        }

        CanvasGroup cg = target.CanvasGroup;
        if (cg == null)
            return;

        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }
}