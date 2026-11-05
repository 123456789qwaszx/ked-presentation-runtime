using System;

/// <summary>
/// VN 재생 상태를 실행기의 정책(ICommandPacePolicy)으로 접는 어댑터.
/// 빨리 넘기기·시크는 "압축"으로 — 실행기는 그것만 본다.
/// 속도 올리기는 타자기만 빠르게 하고 연출은 건드리지 않으므로 여기로 오지 않는다.
/// </summary>
[Serializable]
public sealed class PresentationSessionContext : ICommandPacePolicy
{
    private readonly PlaybackRuntimeState _playback;
    private readonly ISeekStateQuery _seek;

    public PresentationSessionContext(PlaybackRuntimeState playback, ISeekStateQuery seek)
    {
        _playback = playback;
        _seek = seek;
    }

    public bool CompressExecution =>
        (_playback != null && _playback.IsRapidSkipMode) ||
        (_seek != null && _seek.IsSeekingActive);
}
