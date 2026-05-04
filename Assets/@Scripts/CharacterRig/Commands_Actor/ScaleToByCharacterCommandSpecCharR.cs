using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig Motion",
    "Scale (From → To) By Character",
    Order = -169
)]
public sealed class ScaleToByCharacterCommandSpecCharR : CommandSpecBase
{
    [Header("Character Target")]
    public string characterKey;

    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_Scale;

    [Header("Scale (XY)")]
    public Vector2 toScale = Vector2.one;

    [Header("From")]
    public bool overrideFromScale = false;
    public Vector2 fromScale = Vector2.one;

    [Header("Tween")]
    public float duration = 0.4f;
    public Ease ease = Ease.OutCubic;

    [Header("Options")]
    public bool killTween = true;

    [Header("Validation")]
    public bool strict = true;
}

public sealed class ScaleToByCharacterCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly ScaleToByCharacterCommandSpecCharR _spec;

    private RectTransform _rect;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public ScaleToByCharacterCommandCharR(ScaleToByCharacterCommandSpecCharR spec) => _spec = spec;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            yield break;

        if (_spec.killTween)
            _rect.DOKill(true); // Finish previous motion so this command starts from a committed state.

        _canCommitFinalState = true;

        if (_spec.overrideFromScale)
            ApplyScaleXY(_rect, _spec.fromScale);

        if (_spec.duration <= 0f)
        {
            ApplyScaleXY(_rect, _spec.toScale);
            _canCommitFinalState = false;
            _rect = null;
            _tween = null;
            yield break;
        }

        Vector3 endScale = _rect.localScale;
        endScale.x = _spec.toScale.x;
        endScale.y = _spec.toScale.y;

        _tween = _rect
            .DOScale(endScale, _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_rect)
            .OnComplete(() =>
            {
                if (!_canCommitFinalState || _rect == null)
                    return;

                ApplyScaleXY(_rect, _spec.toScale);
                _canCommitFinalState = false;
                _rect = null;
                _tween = null;
            });

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }


    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            return;

        ApplyScaleXY(_rect, _spec.toScale);

        _canCommitFinalState = false;
        _rect = null;
        _tween = null;
    }
    
    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);
    
    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!_canCommitFinalState || _rect == null)
            return;

        _tween?.Kill(false);
        _rect.DOKill(false);
        ApplyScaleXY(_rect, _spec.toScale);

        _canCommitFinalState = false;
        _rect = null;
        _tween = null;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        string characterKey = SafeTrim(_spec.characterKey);
        if (string.IsNullOrEmpty(characterKey))
        {
            if (_spec.strict)
                Debug.LogError("[ScaleToByCharacterCommandCharR] characterKey is null or empty.");
            return;
        }

        if (!scope.CastRegistry.TryGetRole(characterKey, out string roleKey) ||
            string.IsNullOrWhiteSpace(roleKey))
        {
            if (_spec.strict)
                Debug.LogWarning(
                    $"[ScaleToByCharacterCommandCharR] No cast role found for character='{characterKey}'.");
            return;
        }

        if (!scope.Refs.TryGetCharRigRefs(roleKey, out CharacterRigRefs rig) || rig == null)
        {
            if (_spec.strict)
                Debug.LogWarning(
                    $"[ScaleToByCharacterCommandCharR] Rig refs not found. character='{characterKey}', roleKey='{roleKey}'.");
            return;
        }

        _rect = rig.GetRect(_spec.target);

        if (_rect == null && _spec.strict)
        {
            Debug.LogWarning(
                $"[ScaleToByCharacterCommandCharR] Target rect not found. character='{characterKey}', roleKey='{roleKey}', target='{_spec.target}'.");
        }
    }

    private static void ApplyScaleXY(RectTransform rect, Vector2 targetXY)
    {
        Vector3 s = rect.localScale;
        s.x = targetXY.x;
        s.y = targetXY.y;
        rect.localScale = s;
    }

    private static string SafeTrim(string s)
    {
        return string.IsNullOrEmpty(s) ? string.Empty : s.Trim();
    }
}