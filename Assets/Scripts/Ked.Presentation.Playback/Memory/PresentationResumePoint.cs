using System.Collections.Generic;

/// <summary>
/// 재개 지점 — 단위 루트에서 이 줄까지 달리기 위해 연출이 필요로 하는 것.
/// 줄 좌표와, 거기까지 오며 고른 Yarn 선택. 재생 단위가 열릴 때 이것으로 시크를 연다(PlaybackUnit).
/// </summary>
internal sealed class PresentationResumePoint
{
    public LineTarget Target { get; }
    public IReadOnlyList<VNChoiceRecord> YarnChoices { get; }

    public PresentationResumePoint(LineTarget target, IReadOnlyList<VNChoiceRecord> yarnChoices)
    {
        Target = target;
        YarnChoices = yarnChoices;
    }
}
