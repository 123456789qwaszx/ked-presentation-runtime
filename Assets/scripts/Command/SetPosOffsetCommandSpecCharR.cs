using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
[CommandMenuHint(
    "Char Rig", "#Offset Position (default = ResetToZero)", Order = -890,
    Sets = new[]
    {
        CommandMenuSets.ResetChar,
    },
    SetOrder = -940)]
public class SetPosOffsetCommandSpecCharR : CharRigCommandSpecBase
{
    [Header("Target")]
    [Tooltip("offset를 실제로 적용할 대상.")]
    public CharacterRigTarget target = CharacterRigTarget.Character_Track;

    [Header("Offset")]
    [Tooltip("현재 anchoredPosition 기준으로 더해질 오프셋(픽셀 단위).")]
    public Vector2 offset = Vector2.zero;

    [Header("Reset Target Before Apply")]
    [Tooltip("체크하면 target의 위치를 먼저 (0,0)으로 맞춘 뒤 offset을 적용합니다. 상위 Anchor의 위치는 유지됩니다.")]
    public bool applyFromZero = true;

    [Header("Track Layer Reset")]
    [Tooltip("체크하면 Character_Track / Move / X / Y 를 전부 (0,0)으로 초기화합니다.")]
    public bool resetAllTrackLayers = true;

    [Tooltip("Char_Track 을 (0,0)으로 초기화.")]
    public bool resetCharTrack = false;

    [Tooltip("Char_Track_Move 를 (0,0)으로 초기화.")]
    public bool resetCharTrackMove = false;

    [Tooltip("Char_Track_X 를 (0,0)으로 초기화.")]
    public bool resetCharTrackX = false;

    [FormerlySerializedAs("resetCharacterTrackY")] [Tooltip("Char_Track_Y 를 (0,0)으로 초기화.")]
    public bool resetCharTrackY = false;
}

public sealed class SetPosOffsetCommandCharR : CommandBase
{
    private readonly SetPosOffsetCommandSpecCharR _spec;

    private CharacterRigRefs _rig;
    private RectTransform _targetRect;
    private bool _resolveAttempted;

    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public SetPosOffsetCommandCharR(SetPosOffsetCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rig == null)
            yield break;

        Apply();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rig == null)
            return;

        Apply();
    }

    private void Apply()
    {
        // 1) 필요하면 트랙 레이어들부터 먼저 초기화
        ResetRequestedTrackLayers();

        // 2) target에 offset 적용
        if (_targetRect == null)
            return;

        _targetRect.DOKill(false);

        if (_spec.applyFromZero)
            _targetRect.anchoredPosition = Vector2.zero;

        _targetRect.anchoredPosition += _spec.offset;
    }

    private void ResetRequestedTrackLayers()
    {
        bool resetTrack = _spec.resetAllTrackLayers || _spec.resetCharTrack;
        bool resetMove  = _spec.resetAllTrackLayers || _spec.resetCharTrackMove;
        bool resetX     = _spec.resetAllTrackLayers || _spec.resetCharTrackX;
        bool resetY     = _spec.resetAllTrackLayers || _spec.resetCharTrackY;

        if (resetTrack)
            ResetRect(_rig.Character_Track);

        if (resetMove)
            ResetRect(_rig.Character_Track_Move);

        if (resetX)
            ResetRect(_rig.Character_Track_X);

        if (resetY)
            ResetRect(_rig.Character_Track_Y);
    }

    private void ResetRect(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.DOKill(false);
        rect.anchoredPosition = Vector2.zero;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rig) || rig == null)
            return;

        _rig = rig;
        _targetRect = rig.GetRect(_spec.target);
    }
}