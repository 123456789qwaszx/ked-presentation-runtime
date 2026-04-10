using System;
using UnityEngine;
using DG.Tweening;
using System.Collections;
using RectTransform = UnityEngine.RectTransform;

[Serializable]
[CommandMenuHint(
    "Char Rig Motion",
    "Bounce",
    Order = 100)]
public class BounceCommandSpecCharR : CommandSpecBase
{
    [Header("Target")] public CharacterRigTarget target = CharacterRigTarget.Character_Track;

    public Ease ease = Ease.OutCubic;
    
    public float duration = 1.2f;

    [Header("Wave (cute bounce along the path)")] [Tooltip("경로를 따라 통통 튀는 진폭(픽셀). 0이면 웨이브 없음.")]
    public float waveAmplitude = 12f;

    [Tooltip("웨이브 반복 횟수(피크 기준). 1~5 추천.")] 
    public int waveLoops = 4;

    [Tooltip("웨이브 방향 축. 보통 Y 또는 XY가 자연스럽습니다.")]
    public ShakeAxisCharR waveAxis = ShakeAxisCharR.Y;

    [Header("Wait")] public bool wait = false;
}

public sealed class BounceCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly BounceCommandSpecCharR _spec;

    private RectTransform _rect;
    private Vector2 _originPos;

    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public BounceCommandCharR(BounceCommandSpecCharR spec)
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

        bool hasWave = _spec.waveAmplitude != 0f && _spec.waveLoops != 0;

        if (_spec.duration <= 0f || !hasWave)
        {
            yield break;
        }
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

        _rect.anchoredPosition = _originPos;

        _rect = null;
        _originPos = default;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rig) || rig == null)
            return;

        _rect = rig.GetRect(_spec.target);

        _originPos = _rect.anchoredPosition;
    }
}