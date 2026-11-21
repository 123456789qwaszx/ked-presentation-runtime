using System;
using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

[Serializable]
public sealed class SetDepthCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Depth")]
    public CharacterDepthKey preset = CharacterDepthKey.Mid;

    [Tooltip("true이면 preset 대신 level을 사용합니다.")]
    public bool useLevel = false;
    public float level = 5f;

    [Header("Nodes")]
    public CharacterRigNode depthYNode = CharacterRigNode.CharSlot_DepthY;
    public CharacterRigNode depthScaleNode = CharacterRigNode.CharSlot_DepthScale;

    [Header("Tween")]
    public float duration = 0.4f;
    public Ease ease = Ease.OutCubic;
}

/// <summary>
/// size — 깊이 레벨의 Y와 배율을 그대로 놓는다. 무대를 재지 않는다 — level 커브가 낸 값이 곧 목표다.
/// 크기를 바꾸며 어느 지점을 제자리에 두려면 그 보정은 자리 명령으로 따로 적는다(docs/PLAN_command-prune.md P3).
/// </summary>
public sealed class SetDepthCommandCharR : ClaimTweenCommandBase
{
    private readonly SetDepthCommandSpecCharR _spec;
    private readonly CharacterDepthTuningSO _globalTuning;

    private RectTransform _depthYRect;
    private RectTransform _depthScaleRect;

    private Vector2 _destDepthY;
    private Vector2 _destDepthScale;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    private readonly PresentationStage _stage;
    private CharacterRigRefs _rig;

    public SetDepthCommandCharR(
        PresentationStage stage,
        SetDepthCommandSpecCharR spec,
        CharacterDepthTuningSO globalTuning)
    {
        _stage = stage;
        _spec = spec;
        _globalTuning = globalTuning;
    }

    protected override void ResolveRefs()
    {
        _rig = CharacterRigResolver.ResolveRig(_stage, _spec.roleKey);

        _depthYRect = _rig.GetRect(_spec.depthYNode);
        _depthScaleRect = _rig.GetRect(_spec.depthScaleNode);
    }

    protected override void ClaimTarget()
    {
        _depthYRect.DOKill(true);
        _depthScaleRect.DOKill(true);

        CharacterDepthPresetValue depth = _spec.useLevel
            ? _globalTuning.ResolveLevel(_spec.level)
            : _globalTuning.ResolvePreset(_spec.preset);

        _destDepthY = depth.depthY;
        _destDepthScale = new Vector2(depth.depthScale, depth.depthScale);

        // 착란원 계산이 읽는 정지 프레임 값. 트윈 시작 시점에 확정된다.
        _rig.SettledDepthScale = _destDepthScale.x;

        _rig.PlacementTargets.Reserve(_depthYRect, NodeTarget.AnchoredPosition(_depthYRect.name, _destDepthY.ToCore()), Releases);
        _rig.PlacementTargets.Reserve(_depthScaleRect, NodeTarget.LocalScaleXY(_depthScaleRect.name, _destDepthScale.ToCore()), Releases);
    }

    /// <summary>깊이는 Y와 배율이 함께 움직여야 한 몸이라, 두 트윈을 한 시퀀스로 묶는다.</summary>
    protected override Tween CreateTween(float duration)
    {
        Sequence sequence = DOTween.Sequence()
            .SetTarget(_depthYRect);

        sequence.Join(
            _depthYRect
                .DOAnchorPos(_destDepthY, duration)
                .SetEase(_spec.ease)
                .SetUpdate(true)
                .SetTarget(_depthYRect));

        sequence.Join(
            _depthScaleRect
                .DOScale(new Vector3(_destDepthScale.x, _destDepthScale.y, 1f), duration)
                .SetEase(_spec.ease)
                .SetUpdate(true)
                .SetTarget(_depthScaleRect));

        return sequence;
    }

    protected override void OnCommitFinalState()
    {
        _depthYRect.anchoredPosition = _destDepthY;
        _depthScaleRect.localScale = new Vector3(_destDepthScale.x, _destDepthScale.y, 1f);
    }

}