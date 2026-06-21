using System;
using System.Threading;

public sealed class CommandRunScope
{
    private readonly PresentationSessionContext _context;
    private readonly VNLinePresentationState _linePresentationAdvanceState;
    private readonly PresentationStage _stage;
    private readonly bool _reportsNodeBusy;
    
    public CancellationToken Token { get; set; }
    
    public CharacterRigRegistry CharacterRigs => _stage.characterRigs;
    public BackgroundRigRegistry BackgroundRigs => _stage.backgroundRigs;
    public CastRegistry CastRegistry => _stage.castRegistry;

    public CharacterRigTargetAliasRegistry CharacterTargetAliases => _stage.characterTargetAliases;
    
    private LifetimeScope StepLifetime { get; } = new();
    private LifetimeScope RunLifetime { get; } = new();
    
    private bool _isCancelled;

    public CommandRunScope(
        PresentationSessionContext context,
        VNLinePresentationState linePresentationAdvanceState,
        PresentationStage stage,
        bool reportsNodeBusy = true)
    {
        _context = context;
        _linePresentationAdvanceState = linePresentationAdvanceState;
        _stage = stage;
        _reportsNodeBusy = reportsNodeBusy;
        Token = CancellationToken.None;
    }
    public bool IsCancelled => _isCancelled;
    public void MarkCancelled() => _isCancelled = true;

    public bool IsSpeedUpMode => _context != null && _context.IsSpeedUpMode;
    public bool IsAutoMode => _context != null && _context.IsAutoMode;
    public float TimeScale => _context != null ? _context.TimeScale : 1f;
    public bool IsNodeBusy => _context != null && _context.IsNodeBusy;
    
    public bool IsSeekPassThrough => _linePresentationAdvanceState != null && _linePresentationAdvanceState.IsSeekingActive;
    public bool ShouldCompressCommandExecution => IsSpeedUpMode || IsSeekPassThrough;
    
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
    public void TrackRun(Action cancel, Action finish = null) => RunLifetime.Track(cancel, finish);
    
    public void CleanupStep(CleanupPolicy policy) => StepLifetime.Cleanup(policy);
    public void CleanupRun(CleanupPolicy policy) => RunLifetime.Cleanup(policy);
}