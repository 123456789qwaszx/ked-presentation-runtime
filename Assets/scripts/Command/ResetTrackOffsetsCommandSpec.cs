using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig",
    "#ResetTrackOffsets (default = ResetToZero)",
    Order = -890,
    Sets = new[]
    {
        CommandMenuSets.ResetChar,
    },
    SetOrder = -940)]
public class ResetTrackOffsetsCommandSpec : CommandSpecBase
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

    [Tooltip("Char_Track_Y 를 (0,0)으로 초기화.")]
    public bool resetCharTrackY = false;
}

public sealed class ResetTrackOffsetsCommand : CommandBase
{
    private readonly ResetTrackOffsetsCommandSpec _spec;

    private CharacterRigRefs _rigRefs;
    private RectTransform _rect;
    private bool _resolveAttempted;

    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public ResetTrackOffsetsCommand(ResetTrackOffsetsCommandSpec spec) => _spec = spec;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        Apply();
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope) => OnCommandCompleted(scope);

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        Apply();
    }

    private void Apply()
    {
        ResetRequestedTrackLayers();

        _rect.DOKill(true); // Finish previous motion so this command starts from a committed state.

        if (_spec.applyFromZero)
            _rect.anchoredPosition = Vector2.zero;

        _rect.anchoredPosition += _spec.offset;
    }

    private void ResetRequestedTrackLayers()
    {
        bool resetTrack = _spec.resetAllTrackLayers || _spec.resetCharTrack;
        bool resetMove = _spec.resetAllTrackLayers || _spec.resetCharTrackMove;
        bool resetX = _spec.resetAllTrackLayers || _spec.resetCharTrackX;
        bool resetY = _spec.resetAllTrackLayers || _spec.resetCharTrackY;

        if (resetTrack)
            ResetRect(_rigRefs.Character_Track);

        if (resetMove)
            ResetRect(_rigRefs.Character_Track_Move);

        if (resetX)
            ResetRect(_rigRefs.Character_Track_X);

        if (resetY)
            ResetRect(_rigRefs.Character_Track_Y);
    }

    private static void ResetRect(RectTransform rect)
    {
        rect.DOKill(true);  // Finish previous motion so this command starts from a committed state.
        rect.anchoredPosition = Vector2.zero;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rigRefs))
            return;

        _rigRefs = rigRefs;
        _rect = rigRefs.GetRect(_spec.target);
    }
}