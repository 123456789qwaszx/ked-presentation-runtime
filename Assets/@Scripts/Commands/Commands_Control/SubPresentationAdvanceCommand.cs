using System;
using System.Collections;

[Serializable]
[CommandMenuHint(
    "Sub Presentation", "Advance Sub Presentation", Order = -909,
    Sets = new[]
    {
        CommandMenuSets.Presentation,
    },
    SetOrder = -909)]
public sealed class SubPresentationAdvanceCommandSpec : CommandSpecBase
{
    public SyncAdvanceKind Kind = SyncAdvanceKind.Scripted;

    public static SubPresentationAdvanceCommandSpec Scripted()
    {
        return new SubPresentationAdvanceCommandSpec
        {
            Kind = SyncAdvanceKind.Scripted,
        };
    }

    public static SubPresentationAdvanceCommandSpec SeekResync()
    {
        return new SubPresentationAdvanceCommandSpec
        {
            Kind = SyncAdvanceKind.SeekResync,
        };
    }

    public static SubPresentationAdvanceCommandSpec ManualBypassPause()
    {
        return new SubPresentationAdvanceCommandSpec
        {
            Kind = SyncAdvanceKind.ManualBypassPause,
        };
    }
}

public sealed class SubPresentationAdvanceCommand : CommandBase
{
    private readonly VNSideRunnerSyncHub _syncHub;
    private readonly SubPresentationAdvanceCommandSpec _spec;

    public SubPresentationAdvanceCommand(VNSideRunnerSyncHub syncHub, SubPresentationAdvanceCommandSpec spec)
    {
        _syncHub = syncHub;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        Apply();
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope) => Apply();

    private void Apply()
    {
        // This signal must be emitted only after the main line has been committed.
        //
        // Yarn command callbacks may run before VNLineEntryCommitter has updated the current YarnLineMeta.
        // Therefore, those callbacks only collect this command spec;
        // they must not advance the sub lane directly.
        //
        // The valid flow is:
        //   Yarn command callback
        //     -> collect SubPresentationAdvanceCommandSpec
        //   Main line entered
        //     -> CommitLineEntered updates current meta / backlog / rollback state
        //     -> PlayCollected runs this command
        //     -> DispatchPresentationAdvance
        _syncHub.DispatchPresentationAdvance(_spec.Kind);
    }
}