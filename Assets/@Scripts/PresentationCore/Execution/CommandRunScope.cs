using System;
using System.Collections.Generic;
using System.Threading;

public sealed class CommandRunScope
{
    private readonly PresentationSessionContext _context;
    private readonly LinePresentationAdvanceState _linePresentationAdvanceState;
    public CancellationToken Token { get; set; }
    
    public readonly Dictionary<string, object> Refs = new(); //roleKey 기반 런타임 참조 저장소
    public readonly CharacterRigRegistry CharacterRigs = new();
    public CastRegistry CastRegistry { get; } = new(); // 정체성 바인딩 저장소
    
    public PresentationViewRefs Presentation { get; set; }
    
    /// <summary>
    /// Lifetime for resources spawned by commands within the current step.
    /// Cleaned up when the step boundary is crossed.
    /// </summary>
    internal LifetimeScope StepLifetime { get; } = new();

    /// <summary>
    /// Lifetime for resources that must outlive a single step (e.g., BGM).
    /// Cleaned up when the run/session ends.
    /// </summary>
    internal LifetimeScope RunLifetime { get; } = new();

    public CommandRunScope(PresentationSessionContext context, LinePresentationAdvanceState linePresentationAdvanceState)
    {
        _context = context;
        _linePresentationAdvanceState = linePresentationAdvanceState;
        Token = CancellationToken.None;
    }

    public bool IsSkipping => _context != null && _context.IsSpeedUpMode;
    public bool IsAutoMode => _context != null && _context.IsAutoMode;
    public float TimeScale => _context != null ? _context.TimeScale : 1f;
    public bool IsNodeBusy => _context != null && _context.IsNodeBusy;
    public bool IsRollbackSeeking => _linePresentationAdvanceState != null && _linePresentationAdvanceState.IsRollbackSeeking;

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

    // Boundary cleanup
    public void CleanupStep(CleanupPolicy policy) => StepLifetime.Cleanup(policy);
    public void CleanupRun (CleanupPolicy policy) => RunLifetime.Cleanup(policy);

    // Domain-agnostic tracking (no DOTween/Coroutine types needed)
    public void TrackStep(Action cancel, Action finish = null) => StepLifetime.Track(cancel, finish);
    public void TrackRun (Action cancel, Action finish = null) => RunLifetime.Track(cancel, finish);
}