using System;
using System.Threading;

public sealed class CommandRunScope
{
    private readonly PresentationSessionContext _context;
    private readonly VNLinePresentationState _linePresentationAdvanceState;
    public CancellationToken Token { get; set; }
    
    private readonly bool _reportsNodeBusy;
    
    public PresentationStage Stage { get; }
    public CharacterRigRegistry characterRigs => Stage.characterRigs;
    public BackgroundRigRegistry backgroundRigs => Stage.backgroundRigs;
    public CastRegistry castRegistry => Stage.castRegistry;
    
    /// <summary>
    /// Lifetime for resources spawned by commands within the current step.
    /// Cleaned up when the step boundary is crossed.
    /// </summary>
    private LifetimeScope StepLifetime { get; } = new();

    /// <summary>
    /// Lifetime for resources that must outlive a single step (e.g., BGM).
    /// Cleaned up when the run/session ends.
    /// </summary>
    private LifetimeScope RunLifetime { get; } = new();

    public CommandRunScope(
        PresentationSessionContext context,
        VNLinePresentationState linePresentationAdvanceState,
        PresentationStage stage,
        bool reportsNodeBusy = true)
    {
        _context = context;
        _linePresentationAdvanceState = linePresentationAdvanceState;
        Stage = stage;
        _reportsNodeBusy = reportsNodeBusy;
        Token = CancellationToken.None;
    }

    public bool IsSpeedUpMode => _context != null && _context.IsSpeedUpMode;
    public bool IsAutoMode => _context != null && _context.IsAutoMode;
    public float TimeScale => _context != null ? _context.TimeScale : 1f;
    public bool IsNodeBusy => _context != null && _context.IsNodeBusy;
    public bool IsSeekPassThrough => _linePresentationAdvanceState != null && _linePresentationAdvanceState.IsSeekingActive;

    public bool ShouldRespectCommandWait => 
        _context == null || !IsSeekPassThrough || !IsSpeedUpMode;
    public bool ShouldCompressTime => IsSeekPassThrough;
    
    // 메인 레인만 공유 컨텍스트의 NodeBusy를 토글한다.
    // 서브 레인(reportsNodeBusy=false)은 no-op → 공유 NodeBusy/AdvanceGate를 메인 기준으로 보존.
    public void SetNodeBusy(bool busy)
    {
        if (!_reportsNodeBusy)
            return;

        _context?.SetNodeBusy(busy);
    }
    
    public void ClearRuntimeState(CleanupPolicy policy = CleanupPolicy.Cancel)
    {
        CleanupStep(policy);
        CleanupRun(policy);

        Token = CancellationToken.None;
        SetNodeBusy(false);
    }

    public void TrackStep(Action cancel, Action finish = null) => StepLifetime.Track(cancel, finish);
    public void TrackRun (Action cancel, Action finish = null) => RunLifetime.Track(cancel, finish);
    
    public void CleanupStep(CleanupPolicy policy) => StepLifetime.Cleanup(policy);
    public void CleanupRun (CleanupPolicy policy) => RunLifetime.Cleanup(policy);

    
}