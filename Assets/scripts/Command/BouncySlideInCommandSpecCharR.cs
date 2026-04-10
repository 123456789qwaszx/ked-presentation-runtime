using System;
using UnityEngine;
using DG.Tweening;
using System.Collections;
using RectTransform = UnityEngine.RectTransform;

[Serializable]
[CommandMenuHint(
    "Char Rig Motion",
    "Bouncy Slide In", 
    Order = -750)]
public class BouncySlideInCommandSpecCharR : CommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.Character_Track;

    [Header("Slide")]
    public SlideFromCharR from = SlideFromCharR.Left;

    [Tooltip("슬라이드 시작 오프셋 거리(픽셀). 0 이하이면 슬라이드 없이 웨이브만 사용합니다.")]
    public float slideDistance = 480f;

    [Tooltip("슬라이드에 걸리는 시간(초). 0 이하이면 슬라이드 없이 웨이브만 사용합니다.")]
    public float slideDuration = 1.2f;

    public Ease slideEase = Ease.OutCubic;

    [Header("Wave (cute bounce along the path)")]
    [Tooltip("경로를 따라 통통 튀는 진폭(픽셀). 0이면 웨이브 없음.")]
    public float waveAmplitude = 14f;

    [Tooltip("웨이브 반복 횟수(피크 기준). 1~5 추천.")]
    public int waveLoops = 4;

    [Tooltip("웨이브 방향 축. 보통 Y 또는 XY가 자연스럽습니다.")]
    public ShakeAxisCharR waveAxis = ShakeAxisCharR.Y;

    [Header("Wait")]
    public bool wait = false;
}

public sealed class BouncySlideInCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly BouncySlideInCommandSpecCharR _spec;

    private RectTransform _rect;
    private Vector2 _destPos;
    
    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public BouncySlideInCommandCharR(BouncySlideInCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            yield break;

        _rect.DOKill(false);
        
        Vector2 dest = _destPos;
        Vector2 fromPos = dest + GetOffset(_spec.from, _spec.slideDistance);
        bool hasWave = _spec.waveAmplitude != 0f && _spec.waveLoops != 0;
        
        if (_spec.slideDuration <= 0f)
        {
            _rect.anchoredPosition = dest;
            yield break;
        }
        
        _rect.anchoredPosition = fromPos;

        Tween tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    float eased = DOVirtual.EasedValue(0f, 1f, t, _spec.slideEase);

                    Vector2 basePos = Vector2.Lerp(fromPos, dest, eased);

                    Vector2 offset = Vector2.zero;
                    if (hasWave)
                    {
                        float sin = Mathf.Sin(eased * Mathf.PI * _spec.waveLoops);
                        float decay = 1f - eased;
                        float amp = _spec.waveAmplitude * sin * decay;

                        Vector2 slideDir = (dest - fromPos).sqrMagnitude > 0f
                            ? (dest - fromPos).normalized
                            : GetSlideDir(_spec.from);

                        Vector2 perpDir = new Vector2(-slideDir.y, slideDir.x);

                        Vector2 waveDir = _spec.waveAxis switch
                        {
                            ShakeAxisCharR.X => slideDir,
                            ShakeAxisCharR.XY => (slideDir + perpDir).normalized,
                            ShakeAxisCharR.Y => perpDir,
                            _ => perpDir,
                        };

                        offset = waveDir * amp;
                    }

                    _rect.anchoredPosition = basePos + offset;
                },
                1f,
                _spec.slideDuration
            )
            .SetUpdate(true);
        
        //tween.BindToStep(scope);
        
        if (_spec.wait)
            yield return tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        OnCommandCompleted(scope);
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            return;

        _rect.DOKill();
        
        _rect.anchoredPosition = _destPos;
        _rect = null;
        _destPos = default;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rig) || rig == null)
            return;

        _rect = rig.GetRect(_spec.target);
        
        _destPos = _rect.anchoredPosition;
    }
    
    
    private static Vector2 GetOffset(SlideFromCharR from, float distance)
    {
        return from switch
        {
            SlideFromCharR.Right => new Vector2(+distance, 0f),
            SlideFromCharR.Up => new Vector2(0f, +distance),
            SlideFromCharR.Down => new Vector2(0f, -distance),
            _ => new Vector2(-distance, 0f),
        };
    }

    private static Vector2 GetSlideDir(SlideFromCharR from)
    {
        return from switch
        {
            SlideFromCharR.Right => new Vector2(+1f, 0f),
            SlideFromCharR.Up => new Vector2(0f, +1f),
            SlideFromCharR.Down => new Vector2(0f, -1f),
            _ => new Vector2(-1f, 0f),
        };
    }
}