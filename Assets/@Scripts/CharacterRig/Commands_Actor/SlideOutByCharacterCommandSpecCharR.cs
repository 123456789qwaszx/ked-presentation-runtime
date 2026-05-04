using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Char Rig Motion", "Slide Out By Character", Order = -773)]
public sealed class SlideOutByCharacterCommandSpecCharR : CommandSpecBase
{
    [Header("Character Target")]
    public string characterKey;

    [Header("Target (Track)")]
    public CharacterRigTarget target = CharacterRigTarget.Character_Track;

    [Header("Slide")]
    public CharRDirection to = CharRDirection.Right;
    public float distance = 480f;

    [Header("Tween")]
    public float duration = 0.45f;
    public Ease ease = Ease.InCubic;

    [Header("Juice (launch kick at the start)")]
    [Tooltip("0이면 심심한 SlideOut. 8~20 정도가 예쁘게 튐.")]
    public float punch = 14f;

    [Header("Options")]
    [Tooltip("체크하면 기존 위치 관련 트윈을 끝내고 committed state에서 시작합니다.")]
    public bool killTween = true;

    [Header("Validation")]
    public bool strict = true;
}

public sealed class SlideOutByCharacterCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly SlideOutByCharacterCommandSpecCharR _spec;

    private RectTransform _rect;
    private Tween _tween;
    private Vector2 _startPos;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public SlideOutByCharacterCommandCharR(SlideOutByCharacterCommandSpecCharR spec) => _spec = spec;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            yield break;

        if (_spec.killTween)
            _rect.DOKill(true); // Finish previous motion so this command starts from a committed state.

        _canCommitFinalState = true;

        Vector2 start = _startPos;
        Vector2 dir = GetDir(_spec.to);
        Vector2 end = start + dir * _spec.distance;

        if (_spec.duration <= 0f)
        {
            _rect.anchoredPosition = end;
            _canCommitFinalState = false;
            _rect = null;
            _tween = null;
            yield break;
        }

        Vector2 slideDir = end - start;
        slideDir = slideDir.sqrMagnitude > 0f
            ? slideDir.normalized
            : dir;

        _tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    if (!_canCommitFinalState || _rect == null)
                        return;

                    float e = DOVirtual.EasedValue(0f, 1f, t, _spec.ease);
                    Vector2 basePos = Vector2.LerpUnclamped(start, end, e);

                    float bump = JuicyBump_Start(e);
                    Vector2 offset = slideDir * (_spec.punch * bump);

                    _rect.anchoredPosition = basePos + offset;
                },
                1f,
                _spec.duration
            )
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .SetTarget(_rect)
            .OnComplete(() =>
            {
                if (!_canCommitFinalState || _rect == null)
                    return;

                _rect.anchoredPosition = end;
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

        _rect.anchoredPosition = _startPos + GetDir(_spec.to) * _spec.distance;

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
        _rect.anchoredPosition = _startPos + GetDir(_spec.to) * _spec.distance;

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
                Debug.LogError("[SlideOutByCharacterCommandCharR] characterKey is null or empty.");
            return;
        }

        if (!scope.CastRegistry.TryGetRole(characterKey, out string roleKey) ||
            string.IsNullOrWhiteSpace(roleKey))
        {
            if (_spec.strict)
                Debug.LogWarning(
                    $"[SlideOutByCharacterCommandCharR] No cast role found for character='{characterKey}'.");
            return;
        }

        if (!scope.Refs.TryGetCharRigRefs(roleKey, out CharacterRigRefs rig) || rig == null)
        {
            if (_spec.strict)
                Debug.LogWarning(
                    $"[SlideOutByCharacterCommandCharR] Rig refs not found. character='{characterKey}', roleKey='{roleKey}'.");
            return;
        }

        _rect = rig.GetRect(_spec.target);
        if (_rect == null)
        {
            if (_spec.strict)
                Debug.LogWarning(
                    $"[SlideOutByCharacterCommandCharR] Target rect not found. character='{characterKey}', roleKey='{roleKey}', target='{_spec.target}'.");
            return;
        }

        _startPos = _rect.anchoredPosition;
    }

    private static Vector2 GetDir(CharRDirection from) => from switch
    {
        CharRDirection.Right => new Vector2(+1f, 0f),
        CharRDirection.Up => new Vector2(0f, +1f),
        CharRDirection.Down => new Vector2(0f, -1f),
        _ => new Vector2(-1f, 0f),
    };

    private static float JuicyBump_Start(float e)
    {
        e = Mathf.Clamp01(e);
        float oneMinus = 1f - e;
        return Mathf.Sin(Mathf.PI * e) * (oneMinus * oneMinus);
    }

    private static string SafeTrim(string s)
    {
        return string.IsNullOrEmpty(s) ? string.Empty : s.Trim();
    }
}