using System;
using System.Threading;

public sealed class CommandRunScope
{
    private readonly PresentationSessionContext _context;
    private readonly VNLinePresentationState _linePresentationAdvanceState;
    public CancellationToken Token { get; set; }
    
    public readonly CharacterRigRegistry characterRigs = new();
    public readonly BackgroundRigRegistry backgroundRigs = new();
    public readonly CastRegistry castRegistry = new();
    
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

    public CommandRunScope(PresentationSessionContext context, VNLinePresentationState linePresentationAdvanceState)
    {
        _context = context;
        _linePresentationAdvanceState = linePresentationAdvanceState;
        Token = CancellationToken.None;
    }

    public bool IsSkipping => _context != null && _context.IsSpeedUpMode;
    public bool IsAutoMode => _context != null && _context.IsAutoMode;
    public float TimeScale => _context != null ? _context.TimeScale : 1f;
    public bool IsNodeBusy => _context != null && _context.IsNodeBusy;
    public bool IsRollbackSeeking => _linePresentationAdvanceState != null && _linePresentationAdvanceState.IsSeekingActive;

    public bool ShouldRespectCommandWait => 
        _context == null || !IsRollbackSeeking || !IsSkipping;
    public bool ShouldCompressTime => IsRollbackSeeking;
    
    /// <summary>
    /// Must be called only by the Executor.
    /// </summary>
    public void SetNodeBusy(bool busy)
    {
        _context?.SetNodeBusy(busy);
    }
    
    public void ClearRuntimeState(CleanupPolicy policy = CleanupPolicy.Cancel)
    {
        CleanupStep(policy);
        CleanupRun(policy);

        characterRigs.Clear();
        backgroundRigs.Clear();
        castRegistry.Clear();

        Token = CancellationToken.None;
        SetNodeBusy(false);
    }

    public void TrackStep(Action cancel, Action finish = null) => StepLifetime.Track(cancel, finish);
    public void TrackRun (Action cancel, Action finish = null) => RunLifetime.Track(cancel, finish);
    
    public void CleanupStep(CleanupPolicy policy) => StepLifetime.Cleanup(policy);
    public void CleanupRun (CleanupPolicy policy) => RunLifetime.Cleanup(policy);

    
}