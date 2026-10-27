using System;
using System.Threading;

public sealed class CommandRunScope
{
    private readonly ICommandPacePolicy _pace;
    private readonly PresentationStage _stage;

    public CancellationToken Token { get; set; }

    // 무대(리그·배경·cast 레지스트리)는 실행 수명과 다른 것이다 — 리졸버는 Stage만 받는다.
    public PresentationStage Stage => _stage;
    
    private LifetimeScope StepLifetime { get; } = new();
    private LifetimeScope RunLifetime { get; } = new();
    
    private bool _isCancelled;

    public CommandRunScope(ICommandPacePolicy pace, PresentationStage stage)
    {
        _pace = pace;
        _stage = stage;
        Token = CancellationToken.None;
    }

    public bool IsCancelled => _isCancelled;
    public void MarkCancelled() => _isCancelled = true;

    public float TimeScale => _pace.TimeScale;
    
    public bool CommandIsPlaying { get; set; }

    public bool ShouldCompressCommandExecution => _pace.CompressExecution;

    public float ScalePresentationDuration(float duration)
    {
        if (duration <= 0f)
            return duration;

        return duration / Math.Max(1f, TimeScale);
    }

    public void SetCommandIsPlaying(bool busy) => CommandIsPlaying = busy;
    
    public void ClearRuntimeState(CleanupPolicy policy = CleanupPolicy.Cancel)
    {
        CleanupStep(policy);
        CleanupRun(policy);

        Token = CancellationToken.None;
        SetCommandIsPlaying(false);
    }

    public void TrackStep(Action cancel, Action finish = null) => StepLifetime.Track(cancel, finish);
    public void TrackRun(Action cancel, Action finish = null) => RunLifetime.Track(cancel, finish);
    
    public void CleanupStep(CleanupPolicy policy) => StepLifetime.Cleanup(policy);
    public void CleanupRun(CleanupPolicy policy) => RunLifetime.Cleanup(policy);
}