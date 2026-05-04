using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig Motion",
    "Move By (XY) By Character",
    Order = -199)]
public sealed class MoveByByCharacterCommandSpecCharR : CommandSpecBase
{
    [Header("Character Target")]
    public string characterKey;

    [Header("Target (Track or Rig)")]
    public CharacterRigTarget target = CharacterRigTarget.Character_Track;

    [Header("Delta (relative offset)")]
    [Tooltip("현재 anchoredPosition 기준으로 더해질 오프셋(픽셀 단위).")]
    public Vector2 delta = Vector2.zero;

    [Header("Tween")]
    [Tooltip("트윈 시간. <= 0이면 즉시 dest로 스냅")]
    public float duration = 0.4f;

    public Ease ease = Ease.OutCubic;

    [Header("Options")]
    [Tooltip("체크하면 기존 위치 관련 트윈을 끝내고 committed state에서 시작합니다.")]
    public bool killTween = true;

    [Header("Validation")]
    public bool strict = true;
}

public sealed class MoveByByCharacterCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly MoveByByCharacterCommandSpecCharR _spec;

    private RectTransform _rect;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    private bool _hasComputedDest;
    private Vector2 _startPos;
    private Vector2 _destPos;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public MoveByByCharacterCommandCharR(MoveByByCharacterCommandSpecCharR spec) => _spec = spec;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            yield break;

        _hasComputedDest = false;

        if (_spec.killTween)
            _rect.DOKill(true); // Finish previous motion so this command starts from a committed state.

        _canCommitFinalState = true;

        ComputeDestIfNeeded();

        if (_spec.duration <= 0f)
        {
            _rect.anchoredPosition = _destPos;
            _canCommitFinalState = false;
            _rect = null;
            _tween = null;
            yield break;
        }

        _tween = _rect
            .DOAnchorPos(_destPos, _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_rect)
            .OnComplete(() =>
            {
                if (!_canCommitFinalState || _rect == null)
                    return;

                _rect.anchoredPosition = _destPos;
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

        _hasComputedDest = false;
        ComputeDestIfNeeded();

        _rect.anchoredPosition = _destPos;
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

        ComputeDestIfNeeded();
        _rect.anchoredPosition = _destPos;

        _canCommitFinalState = false;
        _rect = null;
        _tween = null;
    }

    private void ComputeDestIfNeeded()
    {
        if (_hasComputedDest)
            return;

        _hasComputedDest = true;
        _startPos = _rect.anchoredPosition;
        _destPos = _startPos + _spec.delta;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        string characterKey = SafeTrim(_spec.characterKey);
        if (string.IsNullOrEmpty(characterKey))
        {
            if (_spec.strict)
                Debug.LogError("[MoveByByCharacterCommandCharR] characterKey is null or empty.");
            return;
        }

        if (!scope.CastRegistry.TryGetRole(characterKey, out string roleKey) ||
            string.IsNullOrWhiteSpace(roleKey))
        {
            if (_spec.strict)
                Debug.LogWarning(
                    $"[MoveByByCharacterCommandCharR] No cast role found for character='{characterKey}'.");
            return;
        }

        if (!scope.Refs.TryGetCharRigRefs(roleKey, out CharacterRigRefs rig) || rig == null)
        {
            if (_spec.strict)
                Debug.LogWarning(
                    $"[MoveByByCharacterCommandCharR] Rig refs not found. character='{characterKey}', roleKey='{roleKey}'.");
            return;
        }

        _rect = rig.GetRect(_spec.target);

        if (_rect == null && _spec.strict)
        {
            Debug.LogWarning(
                $"[MoveByByCharacterCommandCharR] Target rect not found. character='{characterKey}', roleKey='{roleKey}', target='{_spec.target}'.");
        }
    }

    private static string SafeTrim(string s)
    {
        return string.IsNullOrEmpty(s) ? string.Empty : s.Trim();
    }
}