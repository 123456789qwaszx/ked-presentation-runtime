using System;
using UnityEngine;

[Serializable]
public sealed class ShotTrackCommandSpec : ShotIntentCommandSpecBase
{
    [Header("Track")]
    [Tooltip("카메라가 서는 자리(절대값). mid 면의 이 무대 점이 화면 가운데에 온다 — 카메라가 가는 쪽이 +라 화면은 반대로 흐른다.")]
    public Vector2 track = Vector2.zero;
}

// shot_track — 카메라의 몸을 옆 · 위아래로 옮긴다. 층마다 다르게 밀려 시차가 난다.
public sealed class ShotTrackCommand : ShotIntentCommandBase<ShotTrackCommandSpec>
{
    public ShotTrackCommand(PresentationShotResponseSystem rig, ShotTrackCommandSpec spec)
        : base(rig, spec)
    { }

    protected override ShotAxes Axes => ShotAxes.Track;

    protected override PresentationIntentState BuildTargetState(in PresentationIntentState from)
    {
        return Ked.Presentation.Core.ShotIntents
            .Track(from.ToCore(), spec.track.ToCore())
            .ToUnity();
    }
}
