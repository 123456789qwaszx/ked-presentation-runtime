using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
[CommandMenuHint(
    "Char Rig",
    "Set Color (Z)",
    Order = 870
)]
public class SetColorCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortraitSprite_Image;

    [Header("Color")]
    public Color color = Color.white;

    [Tooltip("체크하면 현재 알파(A)는 그대로 두고 색상(RGB)만 변경합니다.")]
    public bool keepAlpha = true;
    
    [Header("Tween")]
    [Tooltip("트윈 시간. <= 0이면 즉시 color로 스냅")]
    public float duration = 0f;

    public Ease ease = Ease.OutCubic;

    [Header("Options")]
    [Tooltip("체크하면 기존 color tween을 끝내고 committed state에서 시작합니다.")]
    public bool killTween = true;
}
public sealed class SetColorCommandCharR : CommandBase
{
    private readonly SetColorCommandSpecCharR _spec;

    private Image _image;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    private bool _hasComputedColor;
    private Color _startColor;
    private Color _destColor;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public SetColorCommandCharR(SetColorCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_image == null)
            yield break;

        _hasComputedColor = false;

        if (_spec.killTween)
            _image.DOKill(true); // Finish previous color tween so this command starts from a committed state.

        _canCommitFinalState = true;

        ComputeColorIfNeeded();

        if (_spec.duration <= 0f)
        {
            _image.color = _destColor;
            _canCommitFinalState = false;
            _image = null;
            _tween = null;
            yield break;
        }

        _tween = _image
            .DOColor(_destColor, _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_image)
            .OnComplete(() =>
            {
                if (!_canCommitFinalState || _image == null)
                    return;

                _image.color = _destColor;
                _canCommitFinalState = false;
                _image = null;
                _tween = null;
            });

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_image == null)
            return;

        _hasComputedColor = false;
        ComputeColorIfNeeded();

        _image.color = _destColor;
        _canCommitFinalState = false;
        _image = null;
        _tween = null;
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!_canCommitFinalState || _image == null)
            return;

        _tween?.Kill(false);
        _image.DOKill(false);

        ComputeColorIfNeeded();
        _image.color = _destColor;

        _canCommitFinalState = false;
        _image = null;
        _tween = null;
    }

    private void ComputeColorIfNeeded()
    {
        if (_hasComputedColor)
            return;

        _hasComputedColor = true;
        _startColor = _image.color;
        _destColor = _spec.color;

        if (_spec.keepAlpha)
            _destColor.a = _startColor.a;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rig =
            CharacterRigTargetResolver.ResolveCharRigFromTargetKey(
                scope,
                _spec.slotKey);

        if (rig == null)
            return;

        RectTransform rect = rig.GetRect(_spec.target);

        if (rect == null)
            return;

        if (!rect.TryGetComponent(out _image))
        {
            Debug.LogWarning(
                $"[SetColorCommandCharR] Target Image not found. targetKey='{_spec.slotKey}', target='{_spec.target}'");
        }
    }
}