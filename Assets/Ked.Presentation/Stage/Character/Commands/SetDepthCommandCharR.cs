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

    [Tooltip("true이면 level · preset 대신 배율을 직접 놓습니다(size 슬롯 x1.04). Y는 건드리지 않습니다.")]
    public bool useScale = false;
    public float scale = 1f;

    [Tooltip("true이면 배율만 놓고 Y(DepthOffset)는 지금 값 그대로 둡니다(size 슬롯 14 keep_y).")]
    public bool keepY = false;

    [Header("Nodes")]
    public CharacterRigNode depthYNode = CharacterRigNode.CharacterPortrait_DepthOffset;
    public CharacterRigNode depthScaleNode = CharacterRigNode.CharacterPortrait_DepthScale;

    [Header("Tween")]
    public float duration = 0.4f;
    public Ease ease = Ease.OutCubic;
}

/// <summary>
/// size — 그 사람의 크기. 깊이 레벨의 Y(DepthOffset)와 배율(DepthScale)을 그대로 놓는다. 무대를 재지 않는다 —
/// level 커브가 낸 값(또는 적힌 배율)이 곧 목표다. keep_y · x배율이면 Y를 건드리지 않는다(docs/PLAN_rig-layers.md L6).
/// 깊이는 초상만 곱한다 — 슬롯의 자리 · 움직임(place · left · right · up · down)은 어느 크기에서나 그대로다.
/// 코어 폴드(NodeTargets.TrySetDepth)와 같은 값을 낸다.
/// </summary>
public sealed class SetDepthCommandCharR : ClaimTweenCommandBase
{
    private readonly SetDepthCommandSpecCharR _spec;
    private readonly CharacterDepthTuningSO _globalTuning;

    private RectTransform _depthYRect;
    private RectTransform _depthScaleRect;

    private bool _movesY;
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

        if (_spec.useScale)
        {
            // 배율을 직접 — 레벨이 없어 Y의 근거가 없다.
            _movesY = false;
            _destDepthScale = new Vector2(_spec.scale, _spec.scale);
        }
        else
        {
            CharacterDepthPresetValue depth = _spec.useLevel
                ? _globalTuning.ResolveLevel(_spec.level)
                : _globalTuning.ResolvePreset(_spec.preset);

            _movesY = !_spec.keepY;
            _destDepthY = depth.depthY;
            _destDepthScale = new Vector2(depth.depthScale, depth.depthScale);
        }

        // 착란원 계산이 읽는 정지 프레임 값. 트윈 시작 시점에 확정된다.
        _rig.SettledDepthScale = _destDepthScale.x;

        if (_movesY)
            _rig.PlacementTargets.Reserve(_depthYRect, NodeTarget.AnchoredPosition(_depthYRect.name, _destDepthY.ToCore()), Releases);

        _rig.PlacementTargets.Reserve(_depthScaleRect, NodeTarget.LocalScaleXY(_depthScaleRect.name, _destDepthScale.ToCore()), Releases);
    }

    /// <summary>
    /// 깊이는 Y와 배율이 함께 움직여야 한 몸이라, 두 트윈을 한 시퀀스로 묶는다. 시퀀스의 표적은 배율 노드다 —
    /// Y를 건드리지 않는 size도 앞 size의 시퀀스를 끝낼 수 있게(ClaimTarget이 두 노드를 다 DOKill한다).
    /// </summary>
    protected override Tween CreateTween(float duration)
    {
        Sequence sequence = DOTween.Sequence()
            .SetTarget(_depthScaleRect);

        if (_movesY)
        {
            sequence.Join(
                _depthYRect
                    .DOAnchorPos(_destDepthY, duration)
                    .SetEase(_spec.ease)
                    .SetUpdate(true)
                    .SetTarget(_depthYRect));
        }

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
        if (_movesY)
            _depthYRect.anchoredPosition = _destDepthY;

        _depthScaleRect.localScale = new Vector3(_destDepthScale.x, _destDepthScale.y, 1f);
    }

}